using MediatR;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Storage;

namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

/// <summary>
/// Handler for retrieving media file content for download/streaming.
/// Coordinates between metadata repository and blob storage.
/// </summary>
public class GetMediaFileQueryHandler
    : IQueryHandler<GetMediaFileQuery, (Stream stream, string contentType, string fileName)?>
{
    private readonly IMediaAssetRepository _repository;
    private readonly IBlobService _blobService;
    private readonly ILogger<GetMediaFileQueryHandler> _logger;

    public GetMediaFileQueryHandler(
        IMediaAssetRepository repository,
        IBlobService blobService,
        ILogger<GetMediaFileQueryHandler> logger)
    {
        _repository = repository;
        _blobService = blobService;
        _logger = logger;
    }

    public async Task<(Stream stream, string contentType, string fileName)?> Handle(
        GetMediaFileQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving media file for MediaId: {MediaId}", request.MediaId);

        // 1. Get media asset metadata by external MediaId (not DB primary key)
        var mediaAsset = await _repository.GetByMediaIdAsync(request.MediaId);
        if (mediaAsset == null)
        {
            _logger.LogWarning("Media asset not found by MediaId: {MediaId}", request.MediaId);
            return null;
        }

        // 2. Validate media has URL/blob reference
        if (string.IsNullOrEmpty(mediaAsset.Url))
        {
            _logger.LogWarning("Media asset {MediaId} has no URL", request.MediaId);
            return null;
        }

        try
        {
            // 3. Download file from blob storage
            // Extract blob name from URL (assuming URL format: https://.../container/blobname)
            var blobName = ExtractBlobNameFromUrl(mediaAsset.Url);
            var stream = await _blobService.DownloadMediaAsync(blobName);

            if (stream == null)
            {
                _logger.LogWarning("Failed to download blob for MediaId: {MediaId}", request.MediaId);
                return null;
            }

            // 4. Return file stream with metadata
            var contentType = mediaAsset.MimeType ?? "application/octet-stream";
            var fileName = GetFileName(mediaAsset);

            // Some streams returned by cloud SDKs (e.g., Azure RetriableStream) do not support Length
            // Avoid touching Length to prevent NotSupportedException
            if (stream.CanSeek)
            {
                try
                {
                    var size = stream.Length; // safe when CanSeek == true
                    _logger.LogInformation(
                        "Successfully retrieved media file: {MediaId}, Size: {Size} bytes",
                        request.MediaId,
                        size);
                }
                catch (NotSupportedException)
                {
                    _logger.LogInformation(
                        "Successfully retrieved media file: {MediaId}, Size: unknown (non-seekable)",
                        request.MediaId);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Successfully retrieved media file: {MediaId}, Size: unknown (streaming)",
                    request.MediaId);
            }

            return (stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading media file: {MediaId}", request.MediaId);
            throw;
        }
    }

    private string ExtractBlobNameFromUrl(string url)
    {
        // Extract blob name from URL
        // URL format: https://account.blob.core.windows.net/container/path/to/file.ext
        var uri = new Uri(url);
        var segments = uri.Segments;

        // Skip container name (first segment after host) and get the rest
        if (segments.Length > 2)
        {
            return string.Join("", segments.Skip(2)).TrimStart('/');
        }

        // Fallback: use last segment as blob name
        return segments.Last().TrimStart('/');
    }

    private string GetFileName(Domain.Models.MediaAsset mediaAsset)
    {
        // Prefer explicit filename, fallback to MediaId with extension
        if (!string.IsNullOrEmpty(mediaAsset.MediaId))
        {
            var extension = GetFileExtension(mediaAsset.MimeType ?? "");
            return $"{mediaAsset.MediaId}{extension}";
        }

        return $"media-{mediaAsset.Id}.bin";
    }

    private string GetFileExtension(string mimeType)
    {
        return mimeType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "audio/mpeg" => ".mp3",
            "audio/wav" => ".wav",
            "audio/ogg" => ".ogg",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            "application/json" => ".json",
            "text/plain" => ".txt",
            _ => ".bin"
        };
    }
}
