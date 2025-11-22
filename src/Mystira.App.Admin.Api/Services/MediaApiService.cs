using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Application.UseCases.Media;
using Mystira.App.Contracts.Requests.Media;
using Mystira.App.Contracts.Responses.Media;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Azure.Services;

namespace Mystira.App.Admin.Api.Services;

/// <summary>
/// Service for managing media assets - delegates to use cases
/// NOTE: This service violates architectural rules (services should not exist in API layer).
/// Controllers should call use cases directly. This is kept temporarily for backward compatibility.
/// </summary>
public class MediaApiService : IMediaApiService
{
    private readonly GetMediaUseCase _getMediaUseCase;
    private readonly GetMediaByFilenameUseCase _getMediaByFilenameUseCase;
    private readonly ListMediaUseCase _listMediaUseCase;
    private readonly UploadMediaUseCase _uploadMediaUseCase;
    private readonly UpdateMediaMetadataUseCase _updateMediaMetadataUseCase;
    private readonly DeleteMediaUseCase _deleteMediaUseCase;
    private readonly DownloadMediaUseCase _downloadMediaUseCase;
    private readonly MystiraAppDbContext _context;
    private readonly IAzureBlobService _blobStorageService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<MediaApiService> _logger;
    private readonly IAudioTranscodingService _audioTranscodingService;

    private readonly Dictionary<string, string> _mimeTypeMap = new()
    {
        // Audio
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".aac", "audio/aac" },
        { ".m4a", "audio/mp4" },
        { ".waptt", "audio/ogg" },
        
        // Video
        { ".mp4", "video/mp4" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".wmv", "video/x-ms-wmv" },
        { ".mkv", "video/x-matroska" },
        
        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".webp", "image/webp" }
    };

    public MediaApiService(
        GetMediaUseCase getMediaUseCase,
        GetMediaByFilenameUseCase getMediaByFilenameUseCase,
        ListMediaUseCase listMediaUseCase,
        UploadMediaUseCase uploadMediaUseCase,
        UpdateMediaMetadataUseCase updateMediaMetadataUseCase,
        DeleteMediaUseCase deleteMediaUseCase,
        DownloadMediaUseCase downloadMediaUseCase,
        MystiraAppDbContext context,
        IAzureBlobService blobStorageService,
        IMediaMetadataService mediaMetadataService,
        ILogger<MediaApiService> logger,
        IAudioTranscodingService audioTranscodingService)
    {
        _getMediaUseCase = getMediaUseCase;
        _getMediaByFilenameUseCase = getMediaByFilenameUseCase;
        _listMediaUseCase = listMediaUseCase;
        _uploadMediaUseCase = uploadMediaUseCase;
        _updateMediaMetadataUseCase = updateMediaMetadataUseCase;
        _deleteMediaUseCase = deleteMediaUseCase;
        _downloadMediaUseCase = downloadMediaUseCase;
        _context = context;
        _blobStorageService = blobStorageService;
        _mediaMetadataService = mediaMetadataService;
        _logger = logger;
        _audioTranscodingService = audioTranscodingService;
    }

    /// <inheritdoc />
    public async Task<Mystira.App.Admin.Api.Models.MediaQueryResponse> GetMediaAsync(Mystira.App.Admin.Api.Models.MediaQueryRequest request)
    {
        // Convert Admin.Api.Models.MediaQueryRequest to Contracts.MediaQueryRequest
        var contractsRequest = new Mystira.App.Contracts.Requests.Media.MediaQueryRequest
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Search = request.Search,
            MediaType = request.MediaType,
            Tags = request.Tags,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };

        var contractsResponse = await _listMediaUseCase.ExecuteAsync(contractsRequest);

        // Convert Contracts.MediaQueryResponse back to Admin.Api.Models.MediaQueryResponse
        return new Mystira.App.Admin.Api.Models.MediaQueryResponse
        {
            Media = contractsResponse.Media,
            TotalCount = contractsResponse.TotalCount,
            Page = contractsResponse.Page,
            PageSize = contractsResponse.PageSize,
            TotalPages = contractsResponse.TotalPages
        };
    }

    /// <inheritdoc />
    public Task<Domain.Models.MediaAsset?> GetMediaByIdAsync(string mediaId) =>
        _getMediaUseCase.ExecuteAsync(mediaId);

    /// <inheritdoc />
    public async Task<Domain.Models.MediaAsset> UploadMediaAsync(IFormFile file, string mediaId, string mediaType, string? description = null, List<string>? tags = null)
    {
        ValidateMediaFile(file, mediaType);

        // Validate that media metadata entry exists and resolve the media ID
        var resolvedMediaId = await ValidateAndResolveMediaId(mediaId, file.FileName);

        // Check if media with this ID already exists
        var existingMedia = await GetMediaByIdAsync(resolvedMediaId);
        if (existingMedia != null)
        {
            throw new InvalidOperationException($"Media with ID '{resolvedMediaId}' already exists");
        }

        await using var processedStream = await PrepareMediaStreamAsync(file);

        var fileSizeBytes = processedStream.Stream.Length;
        var hash = await CalculateStreamHashAsync(processedStream.Stream);
        processedStream.Stream.Position = 0;

        // Upload to blob storage and get URL
        var url = await _blobStorageService.UploadMediaAsync(processedStream.Stream, processedStream.FileName, processedStream.ContentType);

        // Create media asset record
        var mediaAsset = new Domain.Models.MediaAsset
        {
            Id = Guid.NewGuid().ToString(),
            MediaId = resolvedMediaId,
            Url = url,
            MediaType = mediaType,
            MimeType = processedStream.ContentType,
            FileSizeBytes = fileSizeBytes,
            Description = description,
            Tags = tags ?? new List<string>(),
            Hash = hash,
            Version = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MediaAssets.Add(mediaAsset);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Media uploaded successfully: {MediaId} at {Url}", resolvedMediaId, url);

        return mediaAsset;
    }

    /// <inheritdoc />
    public async Task<BulkUploadResult> BulkUploadMediaAsync(IFormFile[] files, bool autoDetectType = true, bool overwriteExisting = false)
    {
        var result = new BulkUploadResult { Success = true };

        // Pre-validate that media metadata file exists
        var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
        if (metadataFile == null || metadataFile.Entries.Count == 0)
        {
            result.Success = false;
            result.Errors.Add("No media metadata file found. Media uploads require a valid media metadata file to be uploaded first.");
            result.FailedCount = files.Length;
            return result;
        }

        foreach (var file in files)
        {
            try
            {
                // Try to find metadata entry by filename first
                var metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.FileName == file.FileName);
                if (metadataEntry == null)
                {
                    result.Errors.Add($"No metadata entry found for file: {file.FileName}");
                    result.FailedCount++;
                    continue;
                }

                var mediaId = metadataEntry.Id;
                var mediaType = autoDetectType ? DetectMediaTypeFromExtension(file.FileName) : metadataEntry.Type;

                if (mediaType == "unknown")
                {
                    result.Errors.Add($"Could not detect media type for file: {file.FileName}");
                    result.FailedCount++;
                    continue;
                }

                // Check if exists and handle overwrite
                var existingMedia = await GetMediaByIdAsync(mediaId);
                if (existingMedia != null && !overwriteExisting)
                {
                    result.Errors.Add($"Media with ID '{mediaId}' already exists (skipped)");
                    result.FailedCount++;
                    continue;
                }

                if (existingMedia != null && overwriteExisting)
                {
                    await DeleteMediaAsync(mediaId);
                }

                await UploadMediaAsync(file, mediaId, mediaType);
                result.SuccessfulUploads.Add(mediaId);
                result.UploadedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to upload {file.FileName}: {ex.Message}");
                result.FailedCount++;
                _logger.LogError(ex, "Failed to upload file during bulk upload: {FileName}", file.FileName);
            }
        }

        result.Success = result.FailedCount == 0;
        result.Message = $"Uploaded {result.UploadedCount} files successfully, {result.FailedCount} failed";

        return result;
    }

    /// <inheritdoc />
    public async Task<Domain.Models.MediaAsset> UpdateMediaAsync(string mediaId, Mystira.App.Admin.Api.Models.MediaUpdateRequest updateData)
    {
        // Convert Admin.Api.Models.MediaUpdateRequest to Contracts.MediaUpdateRequest
        var contractsRequest = new Mystira.App.Contracts.Requests.Media.MediaUpdateRequest
        {
            Description = updateData.Description,
            Tags = updateData.Tags,
            MediaType = updateData.MediaType
        };

        return await _updateMediaMetadataUseCase.ExecuteAsync(mediaId, contractsRequest);
    }

    /// <inheritdoc />
    public Task<bool> DeleteMediaAsync(string mediaId) =>
        _deleteMediaUseCase.ExecuteAsync(mediaId);

    /// <inheritdoc />
    public async Task<MediaValidationResult> ValidateMediaReferencesAsync(List<string> mediaReferences)
    {
        var result = new MediaValidationResult();

        if (mediaReferences == null || mediaReferences.Count == 0)
        {
            result.IsValid = true;
            result.Message = "No media references to validate";
            return result;
        }

        var existingMediaIds = await _context.MediaAssets
            .Where(m => mediaReferences.Contains(m.MediaId))
            .Select(m => m.MediaId)
            .ToListAsync();

        result.ValidMediaIds = existingMediaIds;
        result.MissingMediaIds = mediaReferences.Except(existingMediaIds).ToList();
        result.IsValid = result.MissingMediaIds.Count == 0;

        if (!result.IsValid)
        {
            result.Message = $"Missing media references: {string.Join(", ", result.MissingMediaIds)}";
        }
        else
        {
            result.Message = "All media references are valid";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<MediaUsageStats> GetMediaUsageStatsAsync()
    {
        var stats = new MediaUsageStats();

        var allMedia = await _context.MediaAssets.ToListAsync();

        stats.TotalMediaFiles = allMedia.Count;
        stats.AudioFiles = allMedia.Count(m => m.MediaType == "audio");
        stats.VideoFiles = allMedia.Count(m => m.MediaType == "video");
        stats.ImageFiles = allMedia.Count(m => m.MediaType == "image");
        stats.TotalStorageBytes = allMedia.Sum(m => m.FileSizeBytes);
        stats.TotalStorageFormatted = FormatBytes(stats.TotalStorageBytes);

        // Calculate tag usage
        var allTags = allMedia.SelectMany(m => m.Tags).ToList();
        stats.TagUsage = allTags
            .GroupBy(tag => tag)
            .ToDictionary(group => group.Key, group => group.Count());

        return stats;
    }

    private void ValidateMediaFile(IFormFile file, string mediaType)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required");
        }

        var maxSizeBytes = mediaType switch
        {
            "audio" => 50 * 1024 * 1024, // 50MB
            "video" => 100 * 1024 * 1024, // 100MB
            "image" => 10 * 1024 * 1024, // 10MB
            _ => 10 * 1024 * 1024 // Default 10MB
        };

        if (file.Length > maxSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size for {mediaType} files");
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        var allowedExtensions = mediaType switch
        {
            "audio" => new[] { ".mp3", ".wav", ".ogg", ".aac", ".m4a", ".waptt" },
            "video" => new[] { ".mp4", ".avi", ".mov", ".wmv", ".mkv" },
            "image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" },
            _ => new string[0]
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension '{extension}' is not allowed for {mediaType} files");
        }
    }

    private string GenerateMediaIdFromFileName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return nameWithoutExtension.ToLower().Replace(" ", "-").Replace("_", "-");
    }

    private string DetectMediaTypeFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();

        if (new[] { ".mp3", ".wav", ".ogg", ".aac", ".m4a", ".waptt" }.Contains(extension))
        {
            return "audio";
        }

        if (new[] { ".mp4", ".avi", ".mov", ".wmv", ".mkv" }.Contains(extension))
        {
            return "video";
        }

        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension))
        {
            return "image";
        }

        return "unknown";
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return _mimeTypeMap.TryGetValue(extension, out var mimeType) ? mimeType : "application/octet-stream";
    }

    private string FormatBytes(long bytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;

        if (bytes >= gb)
        {
            return $"{bytes / (double)gb:F2} GB";
        }

        if (bytes >= mb)
        {
            return $"{bytes / (double)mb:F2} MB";
        }

        if (bytes >= kb)
        {
            return $"{bytes / (double)kb:F2} KB";
        }

        return $"{bytes} bytes";
    }

    private async Task<ProcessedMediaStream> PrepareMediaStreamAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension == ".waptt")
        {
            await using var sourceStream = file.OpenReadStream();
            var conversion = await _audioTranscodingService.ConvertWhatsAppVoiceNoteAsync(sourceStream, file.FileName, cancellationToken);

            if (conversion == null)
            {
                throw new InvalidOperationException($"Failed to convert WhatsApp audio file '{file.FileName}'. Ensure ffmpeg is available on the host.");
            }

            return new ProcessedMediaStream(conversion.Stream, conversion.FileName, conversion.ContentType, conversion);
        }

        var memoryStream = new MemoryStream();
        await using (var sourceStream = file.OpenReadStream())
        {
            await sourceStream.CopyToAsync(memoryStream, cancellationToken);
        }

        memoryStream.Position = 0;
        var contentType = !string.IsNullOrWhiteSpace(file.ContentType) ? file.ContentType : GetMimeType(file.FileName);
        return new ProcessedMediaStream(memoryStream, file.FileName, contentType);
    }

    private static Task<string> CalculateStreamHashAsync(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("Stream must support seeking for hash calculation.");
        }

        stream.Position = 0;
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        stream.Position = 0;
        return Task.FromResult(Convert.ToBase64String(hashBytes));
    }

    private sealed class ProcessedMediaStream : IAsyncDisposable
    {
        private readonly IDisposable? _additionalDisposable;

        public ProcessedMediaStream(Stream stream, string fileName, string contentType, IDisposable? additionalDisposable = null)
        {
            Stream = stream;
            FileName = fileName;
            ContentType = contentType;
            _additionalDisposable = additionalDisposable;
        }

        public Stream Stream { get; }
        public string FileName { get; }
        public string ContentType { get; }

        public ValueTask DisposeAsync()
        {
            Stream.Dispose();
            _additionalDisposable?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Validates that a media metadata entry exists for the given media ID and filename
    /// Returns the resolved media ID from metadata
    /// </summary>
    private async Task<string> ValidateAndResolveMediaId(string mediaId, string fileName)
    {
        var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
        if (metadataFile == null || metadataFile.Entries.Count == 0)
        {
            throw new InvalidOperationException("No media metadata file found. Media uploads require a valid media metadata file to be uploaded first.");
        }

        // Look for metadata entry by ID first, then by filename if ID is not found
        var metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == mediaId);
        if (metadataEntry == null)
        {
            metadataEntry = metadataFile.Entries.FirstOrDefault(e =>
                string.Equals(e.FileName, fileName, StringComparison.OrdinalIgnoreCase));

            if (metadataEntry == null)
            {
                var fileNameStem = Path.GetFileNameWithoutExtension(fileName);
                metadataEntry = metadataFile.Entries.FirstOrDefault(e =>
                    string.Equals(Path.GetFileNameWithoutExtension(e.FileName), fileNameStem, StringComparison.OrdinalIgnoreCase));
            }

            if (metadataEntry == null)
            {
                throw new InvalidOperationException($"No media metadata entry found for media ID '{mediaId}' or filename '{fileName}'. Please ensure the media metadata file contains an entry for this media before uploading.");
            }

            // Return the resolved media ID from metadata
            mediaId = metadataEntry.Id;
        }

        _logger.LogInformation("Media upload validated against metadata entry: {MediaId} -> {FileName}", mediaId, fileName);
        return mediaId;
    }

    public Task<(Stream stream, string contentType, string fileName)?> GetMediaFileAsync(string mediaId) =>
        _downloadMediaUseCase.ExecuteAsync(mediaId);

    /// <inheritdoc />
    public Task<Domain.Models.MediaAsset?> GetMediaByFileNameAsync(string fileName) =>
        _getMediaByFilenameUseCase.ExecuteAsync(fileName);

    /// <inheritdoc />
    public async Task<string?> GetMediaUrlAsync(string fileName)
    {
        var mediaAsset = await _getMediaByFilenameUseCase.ExecuteAsync(fileName);
        return mediaAsset?.Url;
    }

    /// <inheritdoc />
    public async Task<ZipUploadResult> UploadMediaFromZipAsync(IFormFile zipFile, bool overwriteMetadata = false, bool overwriteMedia = false)
    {
        var result = new ZipUploadResult { Success = false };

        try
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                result.AllErrors.Add("No zip file provided");
                result.Message = "No zip file provided";
                return result;
            }

            // Extract zip to memory
            var zipEntries = new Dictionary<string, byte[]>();
            string? metadataJsonContent = null;

            using (var stream = zipFile.OpenReadStream())
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName == "media-metadata.json")
                    {
                        using (var entryStream = entry.Open())
                        using (var reader = new StreamReader(entryStream))
                        {
                            metadataJsonContent = await reader.ReadToEndAsync();
                        }
                    }
                    else if (!entry.FullName.EndsWith("/"))
                    {
                        using (var entryStream = entry.Open())
                        using (var memoryStream = new MemoryStream())
                        {
                            await entryStream.CopyToAsync(memoryStream);
                            zipEntries[entry.Name] = memoryStream.ToArray();
                        }
                    }
                }
            }

            // Step 1: Import metadata first
            if (string.IsNullOrEmpty(metadataJsonContent))
            {
                result.AllErrors.Add("No media-metadata.json file found in the zip");
                result.Message = "Failed: media-metadata.json not found in zip file";
                return result;
            }

            try
            {
                var importedMetadata = await _mediaMetadataService.ImportMediaMetadataEntriesAsync(metadataJsonContent, overwriteMetadata);

                result.MetadataResult = new MetadataImportResult
                {
                    Success = true,
                    Message = $"Successfully imported {importedMetadata.Entries.Count} metadata entries",
                    ImportedCount = importedMetadata.Entries.Count
                };

                _logger.LogInformation("Metadata imported successfully from zip: {Count} entries", importedMetadata.Entries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import metadata from zip file");
                result.MetadataResult = new MetadataImportResult
                {
                    Success = false,
                    Message = $"Failed to import metadata: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
                result.AllErrors.Add($"Metadata import failed: {ex.Message}");
                result.Message = "Failed: metadata import error";
                return result;
            }

            // Step 2: Upload media files if metadata was successful
            var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
            if (metadataFile == null || metadataFile.Entries.Count == 0)
            {
                result.AllErrors.Add("Failed to retrieve imported metadata");
                result.Message = "Metadata imported but could not be retrieved for file processing";
                return result;
            }

            foreach (var (fileName, fileBytes) in zipEntries)
            {
                try
                {
                    var metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.FileName == fileName);
                    if (metadataEntry == null)
                    {
                        result.MediaErrors.Add($"No metadata entry found for file: {fileName}");
                        result.FailedMediaCount++;
                        continue;
                    }

                    var mediaId = metadataEntry.Id;
                    var mediaType = metadataEntry.Type;

                    // Check if media already exists
                    var existingMedia = await GetMediaByIdAsync(mediaId);
                    if (existingMedia != null && !overwriteMedia)
                    {
                        result.MediaErrors.Add($"Media with ID '{mediaId}' already exists (skipped)");
                        result.FailedMediaCount++;
                        continue;
                    }

                    if (existingMedia != null && overwriteMedia)
                    {
                        await DeleteMediaAsync(mediaId);
                    }

                    // Create temporary IFormFile from bytes
                    using (var memoryStream = new MemoryStream(fileBytes))
                    {
                        var mimeType = GetMimeType(fileName);

                        // Calculate hash
                        var hash = CalculateHashFromBytes(fileBytes);

                        // Upload to blob storage
                        var url = await _blobStorageService.UploadMediaAsync(memoryStream, fileName, mimeType);

                        // Create media asset record
                        var mediaAsset = new Domain.Models.MediaAsset
                        {
                            Id = Guid.NewGuid().ToString(),
                            MediaId = mediaId,
                            Url = url,
                            MediaType = mediaType,
                            MimeType = mimeType,
                            FileSizeBytes = fileBytes.Length,
                            Description = metadataEntry.Description,
                            Tags = metadataEntry.ClassificationTags.Select(t => $"{t.Key}:{t.Value}").ToList(),
                            Hash = hash,
                            Version = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.MediaAssets.Add(mediaAsset);
                        await _context.SaveChangesAsync();

                        result.SuccessfulMediaUploads.Add(mediaId);
                        result.UploadedMediaCount++;

                        _logger.LogInformation("Media uploaded from zip successfully: {MediaId} from {FileName}", mediaId, fileName);
                    }
                }
                catch (Exception ex)
                {
                    result.MediaErrors.Add($"Failed to upload {fileName}: {ex.Message}");
                    result.FailedMediaCount++;
                    _logger.LogError(ex, "Failed to upload media file from zip: {FileName}", fileName);
                }
            }

            // Prepare final result
            result.Success = result.FailedMediaCount == 0;
            result.AllErrors.AddRange(result.MediaErrors);

            if (result.Success)
            {
                result.Message = $"Successfully imported metadata and uploaded {result.UploadedMediaCount} media files";
            }
            else
            {
                result.Message = $"Metadata imported successfully. Uploaded {result.UploadedMediaCount} media files, {result.FailedMediaCount} failed";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing zip file upload");
            result.AllErrors.Add($"Zip processing error: {ex.Message}");
            result.Message = $"Error processing zip file: {ex.Message}";
            return result;
        }
    }

    private string CalculateHashFromBytes(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(data);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
