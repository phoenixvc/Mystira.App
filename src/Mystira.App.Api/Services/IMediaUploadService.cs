using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for handling media file uploads
/// </summary>
public interface IMediaUploadService
{
    /// <summary>
    /// Uploads a single media file
    /// </summary>
    Task<Domain.Models.MediaAsset> UploadMediaAsync(IFormFile file, string mediaId, string mediaType, string? description = null, List<string>? tags = null);

    /// <summary>
    /// Uploads multiple media files
    /// </summary>
    Task<BulkUploadResult> BulkUploadMediaAsync(IFormFile[] files, bool autoDetectType = true, bool overwriteExisting = false);
}

