using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Storage;

namespace Mystira.App.Application.UseCases.Media;

/// <summary>
/// Use case for deleting a media asset
/// </summary>
public class DeleteMediaUseCase
{
    private readonly IMediaAssetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobService _blobStorageService;
    private readonly ILogger<DeleteMediaUseCase> _logger;

    public DeleteMediaUseCase(
        IMediaAssetRepository repository,
        IUnitOfWork unitOfWork,
        IBlobService blobStorageService,
        ILogger<DeleteMediaUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string mediaId)
    {
        if (string.IsNullOrWhiteSpace(mediaId))
        {
            throw new ArgumentException("Media ID is required", nameof(mediaId));
        }

        var mediaAsset = await _repository.GetByMediaIdAsync(mediaId);
        if (mediaAsset == null)
        {
            _logger.LogWarning("Media asset not found for deletion: {MediaId}", mediaId);
            return false;
        }

        try
        {
            // Delete from blob storage
            if (!string.IsNullOrEmpty(mediaAsset.Url))
            {
                try
                {
                    var uri = new Uri(mediaAsset.Url);
                    var blobName = Path.GetFileName(uri.LocalPath);
                    await _blobStorageService.DeleteMediaAsync(blobName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete media from blob storage: {MediaId}", mediaId);
                    // Continue with database deletion even if blob deletion fails
                }
            }

            // Delete from database
            await _repository.DeleteAsync(mediaAsset.Id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Media deleted successfully: {MediaId}", mediaId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media: {MediaId}", mediaId);
            throw;
        }
    }
}

