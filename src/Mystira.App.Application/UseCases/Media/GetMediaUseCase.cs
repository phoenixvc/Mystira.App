using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.Media;

/// <summary>
/// Use case for retrieving a media asset by ID
/// </summary>
public class GetMediaUseCase
{
    private readonly Mystira.App.Infrastructure.Data.Repositories.IMediaAssetRepository _repository;
    private readonly ILogger<GetMediaUseCase> _logger;

    public GetMediaUseCase(
        Mystira.App.Infrastructure.Data.Repositories.IMediaAssetRepository repository,
        ILogger<GetMediaUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MediaAsset?> ExecuteAsync(string mediaId)
    {
        if (string.IsNullOrWhiteSpace(mediaId))
        {
            throw new ArgumentException("Media ID is required", nameof(mediaId));
        }

        var mediaAsset = await _repository.GetByMediaIdAsync(mediaId);

        if (mediaAsset == null)
        {
            _logger.LogWarning("Media asset not found: {MediaId}", mediaId);
        }

        return mediaAsset;
    }
}

