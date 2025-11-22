using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing bundle uploads
/// </summary>
public interface IBundleService
{
    /// <summary>
    /// Validates a bundle file
    /// </summary>
    /// <param name="bundleFile">The bundle file to validate</param>
    /// <returns>Validation result</returns>
    Task<BundleValidationResult> ValidateBundleAsync(IFormFile bundleFile);

    /// <summary>
    /// Uploads and processes a bundle file
    /// </summary>
    /// <param name="bundleFile">The bundle file to upload</param>
    /// <param name="request">Upload configuration</param>
    /// <returns>Upload result</returns>
    Task<BundleUploadResult> UploadBundleAsync(IFormFile bundleFile, BundleUploadRequest request);
}
