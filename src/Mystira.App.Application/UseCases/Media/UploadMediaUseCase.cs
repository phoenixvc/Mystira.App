using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Storage;
using Mystira.Contracts.App.Requests.Media;
using Mystira.App.Domain.Models;
using Mystira.Shared.Media;

namespace Mystira.App.Application.UseCases.Media;

/// <summary>
/// Use case for uploading a media asset
/// </summary>
public class UploadMediaUseCase
{
    private readonly IMediaAssetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobService _blobStorageService;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<UploadMediaUseCase> _logger;

    public UploadMediaUseCase(
        IMediaAssetRepository repository,
        IUnitOfWork unitOfWork,
        IBlobService blobStorageService,
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
        var resolvedMediaId = await ValidateAndResolveMediaId(request.MediaId ?? string.Empty, request.FileName);

        // Check if media with this ID already exists
        var existingMedia = await _repository.GetByMediaIdAsync(resolvedMediaId);
        if (existingMedia != null)
        {
            throw new InvalidOperationException($"Media with ID '{resolvedMediaId}' already exists");
        }

        // Calculate file hash
        ArgumentNullException.ThrowIfNull(request.FileStream, nameof(request.FileStream));
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

    private static void ValidateMediaFile(UploadMediaRequest request)
    {
        if (request == null || request.FileStream == null)
        {
            throw new ArgumentException("File is required");
        }

        if (request.FileSizeBytes == 0)
        {
            throw new ArgumentException("File size must be greater than zero");
        }

        var maxSizeBytes = MimeTypeRegistry.GetMaxFileSizeBytes(request.MediaType);
        if (request.FileSizeBytes > maxSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size for {request.MediaType} files");
        }

        if (!MimeTypeRegistry.IsValidExtension(request.FileName, request.MediaType))
        {
            var extension = Path.GetExtension(request.FileName);
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

    private static string GetMimeType(string fileName) => MimeTypeRegistry.GetMimeType(fileName);
}

