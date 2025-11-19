using System.IO.Compression;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing bundle uploads
/// </summary>
public class BundleService : IBundleService
{
    private readonly IScenarioApiService _scenarioService;
    private readonly IMediaApiService _mediaService;
    private readonly ILogger<BundleService> _logger;

    public BundleService(
        IScenarioApiService scenarioService,
        IMediaApiService mediaService,
        ILogger<BundleService> logger)
    {
        _scenarioService = scenarioService;
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a bundle file
    /// </summary>
    public async Task<BundleValidationResult> ValidateBundleAsync(IFormFile bundleFile)
    {
        var result = new BundleValidationResult();

        try
        {
            // Check file extension
            if (!bundleFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Bundle file must be a ZIP file");
                return result;
            }

            // Check file size (max 500MB)
            var maxSize = 500 * 1024 * 1024; // 500MB
            if (bundleFile.Length > maxSize)
            {
                result.Errors.Add($"Bundle file size exceeds maximum limit of {maxSize / (1024 * 1024)}MB");
                return result;
            }

            using var stream = bundleFile.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var scenarioFiles = new List<ZipArchiveEntry>();
            var mediaFiles = new List<ZipArchiveEntry>();

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("scenarios/", StringComparison.OrdinalIgnoreCase) && 
                    (entry.FullName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                     entry.FullName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
                {
                    scenarioFiles.Add(entry);
                }
                else if (entry.FullName.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
                {
                    mediaFiles.Add(entry);
                }
            }

            result.ScenarioCount = scenarioFiles.Count;
            result.MediaCount = mediaFiles.Count;

            // Validate scenarios
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            foreach (var scenarioFile in scenarioFiles)
            {
                try
                {
                    using var scenarioStream = scenarioFile.Open();
                    using var reader = new StreamReader(scenarioStream);
                    var yamlContent = await reader.ReadToEndAsync();
                    
                    var scenario = deserializer.Deserialize<Scenario>(yamlContent);
                    if (scenario == null)
                    {
                        result.Errors.Add($"Failed to parse scenario file: {scenarioFile.FullName}");
                        continue;
                    }

                    // Validate scenario structure
                    if (string.IsNullOrEmpty(scenario.Id))
                    {
                        result.Errors.Add($"Scenario missing ID: {scenarioFile.FullName}");
                    }
                    if (string.IsNullOrEmpty(scenario.Title))
                    {
                        result.Errors.Add($"Scenario missing title: {scenarioFile.FullName}");
                    }
                    if (scenario.Scenes == null || scenario.Scenes.Count == 0)
                    {
                        result.Errors.Add($"Scenario missing scenes: {scenarioFile.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error validating scenario {scenarioFile.FullName}: {ex.Message}");
                }
            }

            // Validate media files
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".mp3", ".wav", ".ogg", ".mp4", ".webm" };
            foreach (var mediaFile in mediaFiles)
            {
                var extension = Path.GetExtension(mediaFile.FullName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    result.Warnings.Add($"Unsupported media file type: {mediaFile.FullName}");
                }

                // Check media file size limits
                var mediaType = GetMediaType(extension);
                var sizeLimit = GetMediaSizeLimit(mediaType);
                if (mediaFile.Length > sizeLimit)
                {
                    result.Errors.Add($"Media file exceeds size limit ({sizeLimit / (1024 * 1024)}MB): {mediaFile.FullName}");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            result.Metadata.Add("bundleName", bundleFile.FileName);
            result.Metadata.Add("bundleSize", bundleFile.Length);
            result.Metadata.Add("scenarioFiles", scenarioFiles.Select(f => f.FullName).ToList());
            result.Metadata.Add("mediaFiles", mediaFiles.Select(f => f.FullName).ToList());

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bundle: {FileName}", bundleFile.FileName);
            result.Errors.Add($"Error validating bundle: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Uploads and processes a bundle file
    /// </summary>
    public async Task<BundleUploadResult> UploadBundleAsync(IFormFile bundleFile, BundleUploadRequest request)
    {
        var result = new BundleUploadResult();

        try
        {
            // First validate the bundle
            var validationResult = await ValidateBundleAsync(bundleFile);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.Message = "Bundle validation failed";
                result.Errors = validationResult.Errors;
                result.Warnings = validationResult.Warnings;
                return result;
            }

            using var stream = bundleFile.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var scenarioFiles = archive.Entries
                .Where(e => e.FullName.StartsWith("scenarios/", StringComparison.OrdinalIgnoreCase) && 
                           (e.FullName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                            e.FullName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var mediaFiles = archive.Entries
                .Where(e => e.FullName.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Process media files first
            foreach (var mediaFile in mediaFiles)
            {
                try
                {
                    await ProcessMediaFile(mediaFile, request.OverwriteExisting);
                    result.MediaImported++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing media file {mediaFile.FullName}: {ex.Message}");
                }
            }

            // Process scenario files
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            foreach (var scenarioFile in scenarioFiles)
            {
                try
                {
                    using var scenarioStream = scenarioFile.Open();
                    using var reader = new StreamReader(scenarioStream);
                    var yamlContent = await reader.ReadToEndAsync();
                    
                    var scenario = deserializer.Deserialize<Scenario>(yamlContent);
                    if (scenario != null)
                    {
                        await ProcessScenarioFile(scenario, request.OverwriteExisting);
                        result.ScenariosImported++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing scenario {scenarioFile.FullName}: {ex.Message}");
                }
            }

            result.Success = true;
            result.Message = $"Bundle imported successfully. {result.ScenariosImported} scenarios and {result.MediaImported} media files imported.";
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading bundle: {FileName}", bundleFile.FileName);
            result.Success = false;
            result.Message = $"Error uploading bundle: {ex.Message}";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    private async Task ProcessMediaFile(ZipArchiveEntry mediaFile, bool overwriteExisting)
    {
        var fileName = Path.GetFileName(mediaFile.FullName);
        var mediaType = GetMediaType(Path.GetExtension(fileName));
        var mediaId = GenerateMediaId(fileName);

        // Check if media already exists
        var existingMedia = await _mediaService.GetMediaByIdAsync(mediaId);
        if (existingMedia != null && !overwriteExisting)
        {
            throw new InvalidOperationException($"Media file already exists: {fileName}");
        }

        // Create a temporary file from the ZIP entry
        using var entryStream = mediaFile.Open();
        var tempPath = Path.GetTempFileName();
        var tempFileWithExtension = Path.ChangeExtension(tempPath, Path.GetExtension(fileName));
        
        try
        {
            using var tempFileStream = File.Create(tempFileWithExtension);
            await entryStream.CopyToAsync(tempFileStream);
            
            // Create IFormFile from temporary file
            using var fileStream = new FileStream(tempFileWithExtension, FileMode.Open, FileAccess.Read);
            var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = GetMimeType(Path.GetExtension(fileName))
            };

            await _mediaService.UploadMediaAsync(formFile, mediaId, mediaType);
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            if (File.Exists(tempFileWithExtension))
                File.Delete(tempFileWithExtension);
        }
    }

    private async Task ProcessScenarioFile(Scenario scenario, bool overwriteExisting)
    {
        var existingScenario = await _scenarioService.GetScenarioByIdAsync(scenario.Id);
        if (existingScenario != null && !overwriteExisting)
        {
            throw new InvalidOperationException($"Scenario already exists: {scenario.Id}");
        }

        var createRequest = new CreateScenarioRequest
        {
            Title = scenario.Title,
            Description = scenario.Description,
            Tags = scenario.Tags,
            Difficulty = scenario.Difficulty,
            SessionLength = scenario.SessionLength,
            Archetypes = scenario.Archetypes?.Select(a => a.Value).ToList() ?? new List<string>(),
            AgeGroup = scenario.AgeGroup,
            MinimumAge = scenario.MinimumAge,
            CoreAxes = scenario.CoreAxes?.Select(a => a.Value).ToList() ?? new List<string>(),
            Characters = scenario.Characters,
            Scenes = scenario.Scenes
        };

        createRequest.CompassAxes = createRequest.CoreAxes;

        if (existingScenario != null)
        {
            await _scenarioService.UpdateScenarioAsync(scenario.Id, createRequest);
        }
        else
        {
            await _scenarioService.CreateScenarioAsync(createRequest);
        }
    }

    private string GetMediaType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" => "image",
            ".mp3" or ".wav" or ".ogg" => "audio",
            ".mp4" or ".webm" => "video",
            _ => "unknown"
        };
    }

    private string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    private long GetMediaSizeLimit(string mediaType)
    {
        return mediaType switch
        {
            "audio" => 50 * 1024 * 1024, // 50MB
            "video" => 100 * 1024 * 1024, // 100MB
            "image" => 10 * 1024 * 1024, // 10MB
            _ => 10 * 1024 * 1024 // 10MB default
        };
    }

    private string GenerateMediaId(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fileName)))[..8];
        return $"{nameWithoutExtension.ToLowerInvariant().Replace(" ", "-")}-{hash}";
    }
}