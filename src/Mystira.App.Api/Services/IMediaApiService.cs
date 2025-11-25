using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing media assets
/// </summary>
/// <remarks>
/// DEPRECATED: This service violates hexagonal architecture.
/// Controllers should use IMediator (CQRS pattern) instead.
/// Use GetMediaAssetQuery, GetMediaFileQuery, UploadMediaCommand, etc.
/// This interface will be removed in a future version.
/// </remarks>
[Obsolete("Use IMediator with CQRS queries/commands instead. See ARCHITECTURAL_REFACTORING_PLAN.md")]
public interface IMediaApiService
{
    /// <summary>
    /// Gets all media assets with optional filtering
    /// </summary>
    /// <param name="request">Query parameters for filtering</param>
    /// <returns>Paginated list of media assets</returns>
    Task<MediaQueryResponse> GetMediaAsync(MediaQueryRequest request);

    /// <summary>
    /// Gets a specific media asset by ID
    /// </summary>
    /// <param name="mediaId">The media ID</param>
    /// <returns>The media asset or null if not found</returns>
    Task<Domain.Models.MediaAsset?> GetMediaByIdAsync(string mediaId);

    /// <summary>
    /// Uploads a single media file
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="mediaId">The media ID</param>
    /// <param name="mediaType">The media type (audio, video, image)</param>
    /// <param name="description">Optional description</param>
    /// <param name="tags">Optional tags</param>
    /// <returns>The created media asset</returns>
    Task<Domain.Models.MediaAsset> UploadMediaAsync(IFormFile file, string mediaId, string mediaType, string? description = null, List<string>? tags = null);

    /// <summary>
    /// Uploads multiple media files
    /// </summary>
    /// <param name="files">The files to upload</param>
    /// <param name="autoDetectType">Whether to auto-detect media type from file extension</param>
    /// <param name="overwriteExisting">Whether to overwrite existing media with same ID</param>
    /// <returns>Bulk upload result</returns>
    Task<BulkUploadResult> BulkUploadMediaAsync(IFormFile[] files, bool autoDetectType = true, bool overwriteExisting = false);

    /// <summary>
    /// Updates a media asset
    /// </summary>
    /// <param name="mediaId">The media ID</param>
    /// <param name="updateData">The updated media data</param>
    /// <returns>The updated media asset</returns>
    Task<Domain.Models.MediaAsset> UpdateMediaAsync(string mediaId, MediaUpdateRequest updateData);

    /// <summary>
    /// Deletes a media asset
    /// </summary>
    /// <param name="mediaId">The media ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteMediaAsync(string mediaId);

    /// <summary>
    /// Validates that media references exist
    /// </summary>
    /// <param name="mediaReferences">List of media IDs to validate</param>
    /// <returns>Validation result with missing media IDs</returns>
    Task<MediaValidationResult> ValidateMediaReferencesAsync(List<string> mediaReferences);

    /// <summary>
    /// Gets media usage statistics
    /// </summary>
    /// <returns>Media usage statistics</returns>
    Task<MediaUsageStats> GetMediaUsageStatsAsync();

    /// <summary>
    /// Gets the file content for a media asset
    /// </summary>
    /// <param name="mediaId">The media ID</param>
    /// <returns>File stream and content type</returns>
    Task<(Stream stream, string contentType, string fileName)?> GetMediaFileAsync(string mediaId);

    /// <summary>
    /// Gets a media asset by filename, resolving through metadata
    /// </summary>
    /// <param name="fileName">The filename to resolve</param>
    /// <returns>The media asset or null if not found</returns>
    Task<Domain.Models.MediaAsset?> GetMediaByFileNameAsync(string fileName);

    /// <summary>
    /// Gets the URL for a media asset by filename, resolving through metadata
    /// </summary>
    /// <param name="fileName">The filename to resolve</param>
    /// <returns>The media URL or null if not found</returns>
    Task<string?> GetMediaUrlAsync(string fileName);
}
