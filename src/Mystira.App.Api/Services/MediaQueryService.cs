using Microsoft.EntityFrameworkCore;
using Mystira.App.Api.Models;
using Mystira.App.Api.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for querying and retrieving media assets
/// </summary>
public class MediaQueryService : IMediaQueryService
{
    private readonly IMediaAssetRepository _repository;
    private readonly IMediaMetadataService _mediaMetadataService;
    private readonly ILogger<MediaQueryService> _logger;

    public MediaQueryService(
        IMediaAssetRepository repository,
        IMediaMetadataService mediaMetadataService,
        ILogger<MediaQueryService> logger)
    {
        _repository = repository;
        _mediaMetadataService = mediaMetadataService;
        _logger = logger;
    }

    public async Task<MediaQueryResponse> GetMediaAsync(MediaQueryRequest request)
    {
        var query = _repository.GetQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(m => m.MediaId.Contains(request.Search) ||
                                    m.Url.Contains(request.Search) ||
                                    (m.Description != null && m.Description.Contains(request.Search)));
        }

        if (!string.IsNullOrEmpty(request.MediaType))
        {
            query = query.Where(m => m.MediaType == request.MediaType);
        }

        if (request.Tags != null && request.Tags.Count > 0)
        {
            foreach (var tag in request.Tags)
            {
                query = query.Where(m => m.Tags.Contains(tag));
            }
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "filename" => request.SortDescending ? query.OrderByDescending(m => m.Url) : query.OrderBy(m => m.Url),
            "mediatype" => request.SortDescending ? query.OrderByDescending(m => m.MediaType) : query.OrderBy(m => m.MediaType),
            "filesize" => request.SortDescending ? query.OrderByDescending(m => m.FileSizeBytes) : query.OrderBy(m => m.FileSizeBytes),
            "updatedat" => request.SortDescending ? query.OrderByDescending(m => m.UpdatedAt) : query.OrderBy(m => m.UpdatedAt),
            _ => request.SortDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var media = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new MediaQueryResponse
        {
            Media = media,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<MediaAsset?> GetMediaByIdAsync(string mediaId)
    {
        return await _repository.GetByMediaIdAsync(mediaId);
    }

    public async Task<(Stream stream, string contentType, string fileName)?> GetMediaFileAsync(string mediaId)
    {
        try
        {
            var mediaAsset = await GetMediaByIdAsync(mediaId);
            if (mediaAsset == null)
            {
                return null;
            }

            // Download the file from the URL
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(mediaAsset.Url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var stream = await response.Content.ReadAsStreamAsync();
            var fileName = Path.GetFileName(new Uri(mediaAsset.Url).LocalPath);

            return (stream, mediaAsset.MimeType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media file: {MediaId}", mediaId);
            return null;
        }
    }

    public async Task<MediaAsset?> GetMediaByFileNameAsync(string fileName)
    {
        try
        {
            // Get metadata file to resolve filename to media ID
            var metadataFile = await _mediaMetadataService.GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                return null;
            }

            // Find metadata entry by filename
            var metadataEntry = metadataFile.Entries.FirstOrDefault(e => e.FileName == fileName);
            if (metadataEntry == null)
            {
                return null;
            }

            // Get the media asset by the resolved media ID
            return await GetMediaByIdAsync(metadataEntry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media by filename: {FileName}", fileName);
            return null;
        }
    }

    public async Task<string?> GetMediaUrlAsync(string fileName)
    {
        var mediaAsset = await GetMediaByFileNameAsync(fileName);
        return mediaAsset?.Url;
    }

    public async Task<MediaValidationResult> ValidateMediaReferencesAsync(List<string> mediaReferences)
    {
        var result = new MediaValidationResult();

        if (mediaReferences == null || mediaReferences.Count == 0)
        {
            result.IsValid = true;
            result.Message = "No media references to validate";
            return result;
        }

        var existingMediaIds = (await _repository.GetMediaIdsAsync(mediaReferences)).ToList();

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

    public async Task<MediaUsageStats> GetMediaUsageStatsAsync()
    {
        var stats = new MediaUsageStats();

        var allMedia = (await _repository.GetAllAsync()).ToList();

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
}

