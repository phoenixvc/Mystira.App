using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Wolverine handler for GetAllContentBundlesQuery
/// Retrieves all active content bundles
/// </summary>
public static class GetAllContentBundlesQueryHandler
{
    public static async Task<IEnumerable<ContentBundle>> Handle(
        GetAllContentBundlesQuery request,
        IContentBundleRepository repository,
        ILogger<GetAllContentBundlesQuery> logger,
        CancellationToken ct)
    {
        // Get all content bundles
        var bundles = await repository.GetAllAsync();

        logger.LogDebug("Retrieved {Count} content bundles", bundles.Count());

        return bundles;
    }
}
