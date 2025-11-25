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
        var media = await _repository.GetByIdAsync(request.MediaId);
        _logger.LogDebug("Retrieved media asset {MediaId}", request.MediaId);
        return media;
    }
}
