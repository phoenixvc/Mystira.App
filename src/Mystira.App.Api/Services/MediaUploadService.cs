using System.Security.Cryptography;
using Mystira.App.Api.Models;
using Mystira.App.Api.Repositories;
using Mystira.App.Infrastructure.Azure.Services;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for handling media file uploads
/// </summary>
public class MediaUploadService : IMediaUploadService
{
    private readonly IMediaAssetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzureBlobService _blobStorageService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<MediaUploadService> _logger;

    private readonly Dictionary<string, string> _mimeTypeMap = new()
    {
        // Audio
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".aac", "audio/aac" },
        { ".m4a", "audio/mp4" },
        
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

    public MediaUploadService(
        IMediaAssetRepository repository,
        IUnitOfWork unitOfWork,
        IAzureBlobService blobStorageService,
        IMediaMetadataService mediaMetadataService,
        ILogger<MediaUploadService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _blobStorageService = blobStorageService;
        _mediaMetadataService = mediaMetadataService;
        _logger = logger;
    }

    public async Task<MediaAsset> UploadMediaAsync(IFormFile file, string mediaId, string mediaType, string? description = null, List<string>? tags = null)
    {
        ValidateMediaFile(file, mediaType);

        // Validate that media metadata entry exists and resolve the media ID
        var resolvedMediaId = await ValidateAndResolveMediaId(mediaId, file.FileName);

        // Check if media with this ID already exists
        var existingMedia = await _repository.GetByMediaIdAsync(resolvedMediaId);
        if (existingMedia != null)
        {
            throw new InvalidOperationException($"Media with ID '{resolvedMediaId}' already exists");
        }

        // Calculate file hash
        var hash = await CalculateFileHashAsync(file);

        // Upload to blob storage and get URL
        var url = await _blobStorageService.UploadMediaAsync(file.OpenReadStream(), file.FileName, file.ContentType ?? GetMimeType(file.FileName));

        // Create media asset record
        var mediaAsset = new MediaAsset
        {
            Id = Guid.NewGuid().ToString(),
            MediaId = resolvedMediaId,
            Url = url,
            MediaType = mediaType,
            MimeType = file.ContentType ?? GetMimeType(file.FileName),
            FileSizeBytes = file.Length,
            Description = description,
            Tags = tags ?? new List<string>(),
            Hash = hash,
            Version = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(mediaAsset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Media uploaded successfully: {MediaId} at {Url}", resolvedMediaId, url);

        return mediaAsset;
    }

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
                var existingMedia = await _repository.GetByMediaIdAsync(mediaId);
                if (existingMedia != null && !overwriteExisting)
                {
                    result.Errors.Add($"Media with ID '{mediaId}' already exists (skipped)");
                    result.FailedCount++;
                    continue;
                }

                if (existingMedia != null && overwriteExisting)
                {
                    // Delete existing media from blob storage and database
                    try
                    {
                        var uri = new Uri(existingMedia.Url);
                        var blobName = Path.GetFileName(uri.LocalPath);
                        await _blobStorageService.DeleteMediaAsync(blobName);
                        await _repository.DeleteAsync(existingMedia.Id);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete existing media {MediaId} during overwrite", mediaId);
                        result.Errors.Add($"Failed to delete existing media '{mediaId}': {ex.Message}");
                        result.FailedCount++;
                        continue;
                    }
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
            "audio" => new[] { ".mp3", ".wav", ".ogg", ".aac", ".m4a" },
            "video" => new[] { ".mp4", ".avi", ".mov", ".wmv", ".mkv" },
            "image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" },
            _ => new string[0]
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension '{extension}' is not allowed for {mediaType} files");
        }
    }

    private string DetectMediaTypeFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();

        if (new[] { ".mp3", ".wav", ".ogg", ".aac", ".m4a" }.Contains(extension))
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

    private async Task<string> CalculateFileHashAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var sha256 = SHA256.Create();
        var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
        return Convert.ToBase64String(hashBytes);
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
            metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.FileName == fileName);
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
}

