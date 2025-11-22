using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for querying and retrieving media assets
/// </summary>
public interface IMediaQueryService
{
    Task<MediaQueryResponse> GetMediaAsync(MediaQueryRequest request);
    Task<MediaAsset?> GetMediaByIdAsync(string mediaId);
    Task<(Stream stream, string contentType, string fileName)?> GetMediaFileAsync(string mediaId);
    Task<MediaAsset?> GetMediaByFileNameAsync(string fileName);
    Task<string?> GetMediaUrlAsync(string fileName);
    Task<MediaValidationResult> ValidateMediaReferencesAsync(List<string> mediaReferences);
    Task<MediaUsageStats> GetMediaUsageStatsAsync();
}

