using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

public class GetMediaAssetQueryHandler : IQueryHandler<GetMediaAssetQuery, MediaAsset?>
{
    private readonly IMediaAssetRepository _repository;
    private readonly ILogger<GetMediaAssetQueryHandler> _logger;

    public GetMediaAssetQueryHandler(
        IMediaAssetRepository repository,
        ILogger<GetMediaAssetQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MediaAsset?> Handle(
        GetMediaAssetQuery request,
        CancellationToken cancellationToken)
    {
        // MediaId is an external identifier stored in the MediaAsset document; do not use the DB primary key.
        var media = await _repository.GetByMediaIdAsync(request.MediaId);
        if (media == null)
        {
            _logger.LogDebug("Media asset not found by MediaId {MediaId}", request.MediaId);
            return null;
        }

        _logger.LogDebug("Retrieved media asset by MediaId {MediaId}", request.MediaId);
        return media;
    }
}
