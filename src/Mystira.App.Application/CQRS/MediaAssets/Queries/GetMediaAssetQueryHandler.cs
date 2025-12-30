using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

/// <summary>
/// Wolverine handler for retrieving a media asset by MediaId
/// </summary>
public static class GetMediaAssetQueryHandler
{
    public static async Task<MediaAsset?> Handle(
        GetMediaAssetQuery request,
        IMediaAssetRepository repository,
        ILogger<GetMediaAssetQuery> logger,
        CancellationToken ct)
    {
        // MediaId is an external identifier stored in the MediaAsset document; do not use the DB primary key.
        var media = await repository.GetByMediaIdAsync(request.MediaId);
        if (media == null)
        {
            logger.LogDebug("Media asset not found by MediaId {MediaId}", request.MediaId);
            return null;
        }

        logger.LogDebug("Retrieved media asset by MediaId {MediaId}", request.MediaId);
        return media;
    }
}
