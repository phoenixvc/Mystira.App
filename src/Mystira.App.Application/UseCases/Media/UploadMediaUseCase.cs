using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.App.Contracts.Requests.Media;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Azure.Services;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using IMediaAssetRepository = Mystira.App.Infrastructure.Data.Repositories.IMediaAssetRepository;

namespace Mystira.App.Application.UseCases.Media;

/// <summary>
/// Use case for uploading a media asset
/// </summary>
public class UploadMediaUseCase
{
    private readonly IMediaAssetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzureBlobService _blobStorageService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<UploadMediaUseCase> _logger;

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

    public UploadMediaUseCase(
        IMediaAssetRepository repository,
        IUnitOfWork unitOfWork,
        IAzureBlobService blobStorageService,
        IMediaMetadataService mediaMetadataService,
        ILogger<UploadMediaUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _blobStorageService = blobStorageService;
        _mediaMetadataService = mediaMetadataService;
        _logger = logger;
    }

    public async Task<MediaAsset> ExecuteAsync(UploadMediaRequest request)
    {
        ValidateMediaFile(request);

        // Validate that media metadata entry exists and resolve the media ID
        var resolvedMediaId = await ValidateAndResolveMediaId(request.MediaId, request.FileName);

        // Check if media with this ID already exists
        var existingMedia = await _repository.GetByMediaIdAsync(resolvedMediaId);
        if (existingMedia != null)
        {
            throw new InvalidOperationException($"Media with ID '{resolvedMediaId}' already exists");
        }

        // Calculate file hash
        var hash = await CalculateFileHashAsync(request.FileStream);

        // Reset stream position for upload
        request.FileStream.Position = 0;

        // Upload to blob storage and get URL
        var url = await _blobStorageService.UploadMediaAsync(request.FileStream, request.FileName, request.ContentType ?? GetMimeType(request.FileName));

        // Create media asset record
        var mediaAsset = new MediaAsset
        {
            Id = Guid.NewGuid().ToString(),
            MediaId = resolvedMediaId,
            Url = url,
            MediaType = request.MediaType,
            MimeType = request.ContentType ?? GetMimeType(request.FileName),
            FileSizeBytes = request.FileSizeBytes,
            Description = request.Description,
            Tags = request.Tags ?? new List<string>(),
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

    private void ValidateMediaFile(UploadMediaRequest request)
    {
        if (request == null || request.FileStream == null)
        {
            throw new ArgumentException("File is required");
        }

        if (request.FileSizeBytes == 0)
        {
            throw new ArgumentException("File size must be greater than zero");
        }

        var maxSizeBytes = request.MediaType switch
        {
            "audio" => 50 * 1024 * 1024, // 50MB
            "video" => 100 * 1024 * 1024, // 100MB
            "image" => 10 * 1024 * 1024, // 10MB
            _ => 10 * 1024 * 1024 // Default 10MB
        };

        if (request.FileSizeBytes > maxSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size for {request.MediaType} files");
        }

        var extension = Path.GetExtension(request.FileName).ToLower();
        var allowedExtensions = request.MediaType switch
        {
            "audio" => new[] { ".mp3", ".wav", ".ogg", ".aac", ".m4a" },
            "video" => new[] { ".mp4", ".avi", ".mov", ".wmv", ".mkv" },
            "image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" },
            _ => Array.Empty<string>()
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension '{extension}' is not allowed for {request.MediaType} files");
        }
    }

    private async Task<string> ValidateAndResolveMediaId(string mediaId, string fileName)
    {
        // Get metadata file to validate and resolve media ID
        var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
        if (metadataFile == null || metadataFile.Entries.Count == 0)
        {
            throw new InvalidOperationException("No media metadata file found. Media uploads require a valid media metadata file to be uploaded first.");
        }

        // Try to find metadata entry by filename first
        var metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.FileName == fileName);
        if (metadataEntry != null)
        {
            return metadataEntry.Id;
        }

        // If not found by filename, try to find by media ID
        metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == mediaId);
        if (metadataEntry == null)
        {
            throw new InvalidOperationException($"No metadata entry found for media ID '{mediaId}' or filename '{fileName}'");
        }

        return metadataEntry.Id;
    }

    private async Task<string> CalculateFileHashAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var originalPosition = stream.Position;
        stream.Position = 0;
        var hashBytes = await sha256.ComputeHashAsync(stream);
        stream.Position = originalPosition;
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return _mimeTypeMap.TryGetValue(extension, out var mimeType) ? mimeType : "application/octet-stream";
    }
}

