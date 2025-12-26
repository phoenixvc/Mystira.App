using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Wolverine handler for GetContentBundlesByAgeGroupQuery
/// Retrieves content bundles filtered by age group
/// </summary>
public static class GetContentBundlesByAgeGroupQueryHandler
{
    public static async Task<IEnumerable<ContentBundle>> Handle(
        GetContentBundlesByAgeGroupQuery request,
        IContentBundleRepository repository,
        ILogger<GetContentBundlesByAgeGroupQuery> logger,
        CancellationToken ct)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.AgeGroup))
        {
            logger.LogWarning("Age group cannot be null or empty");
            throw new ArgumentException("Age group cannot be null or empty", nameof(request.AgeGroup));
        }

        // Create specification for reusable query logic
        var spec = new ContentBundlesByAgeGroupSpec(request.AgeGroup);

        // Execute query using repository
        var bundles = await repository.ListAsync(spec);

        logger.LogDebug("Retrieved {Count} bundles for age group {AgeGroup}",
            bundles.Count(), request.AgeGroup);

        return bundles;
    }
}
