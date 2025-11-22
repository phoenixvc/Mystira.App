using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Azure.Services;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing media assets - composes upload and query services
/// </summary>
public class MediaApiService : IMediaApiService
{
    private readonly IMediaUploadService _uploadService;
    private readonly IMediaQueryService _queryService;
    private readonly Mystira.App.Infrastructure.Data.Repositories.IMediaAssetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzureBlobService _blobStorageService;
    private readonly ILogger<MediaApiService> _logger;

    public MediaApiService(
        IMediaUploadService uploadService,
        IMediaQueryService queryService,
        IMediaAssetRepository repository,
        IUnitOfWork unitOfWork,
        IAzureBlobService blobStorageService,
        ILogger<MediaApiService> logger)
    {
        _uploadService = uploadService;
        _queryService = queryService;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    // Query methods - delegate to MediaQueryService
    public Task<MediaQueryResponse> GetMediaAsync(MediaQueryRequest request) =>
        _queryService.GetMediaAsync(request);

    public Task<MediaAsset?> GetMediaByIdAsync(string mediaId) =>
        _queryService.GetMediaByIdAsync(mediaId);

    public Task<(Stream stream, string contentType, string fileName)?> GetMediaFileAsync(string mediaId) =>
        _queryService.GetMediaFileAsync(mediaId);

    public Task<MediaAsset?> GetMediaByFileNameAsync(string fileName) =>
        _queryService.GetMediaByFileNameAsync(fileName);

    public Task<string?> GetMediaUrlAsync(string fileName) =>
        _queryService.GetMediaUrlAsync(fileName);

    public Task<MediaValidationResult> ValidateMediaReferencesAsync(List<string> mediaReferences) =>
        _queryService.ValidateMediaReferencesAsync(mediaReferences);

    public Task<MediaUsageStats> GetMediaUsageStatsAsync() =>
        _queryService.GetMediaUsageStatsAsync();

    // Upload methods - delegate to MediaUploadService
    public Task<MediaAsset> UploadMediaAsync(IFormFile file, string mediaId, string mediaType, string? description = null, List<string>? tags = null) =>
        _uploadService.UploadMediaAsync(file, mediaId, mediaType, description, tags);

    public Task<BulkUploadResult> BulkUploadMediaAsync(IFormFile[] files, bool autoDetectType = true, bool overwriteExisting = false) =>
        _uploadService.BulkUploadMediaAsync(files, autoDetectType, overwriteExisting);

    // Management methods - update and delete
    public async Task<Domain.Models.MediaAsset> UpdateMediaAsync(string mediaId, MediaUpdateRequest updateData)
    {
        var mediaAsset = await GetMediaByIdAsync(mediaId);
        if (mediaAsset == null)
        {
            throw new KeyNotFoundException($"Media with ID '{mediaId}' not found");
        }

        // Update properties
        if (updateData.Description != null)
        {
            mediaAsset.Description = updateData.Description;
        }

        if (updateData.Tags != null)
        {
            mediaAsset.Tags = updateData.Tags;
        }

        if (!string.IsNullOrEmpty(updateData.MediaType))
        {
            mediaAsset.MediaType = updateData.MediaType;
        }

        mediaAsset.UpdatedAt = DateTime.UtcNow;
        mediaAsset.Version = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        await _repository.UpdateAsync(mediaAsset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Media updated successfully: {MediaId}", mediaId);

        return mediaAsset;
    }

    public async Task<bool> DeleteMediaAsync(string mediaId)
    {
        var mediaAsset = await GetMediaByIdAsync(mediaId);
        if (mediaAsset == null)
        {
            return false;
        }

        try
        {
            // Extract blob name from URL for deletion
            var uri = new Uri(mediaAsset.Url);
            var blobName = Path.GetFileName(uri.LocalPath);

            // Delete from blob storage
            await _blobStorageService.DeleteMediaAsync(blobName);

            // Delete from database
            await _repository.DeleteAsync(mediaAsset.Id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Media deleted successfully: {MediaId}", mediaId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media: {MediaId}", mediaId);
            throw;
        }
    }
}
