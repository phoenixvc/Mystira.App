using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Wolverine handler for GetFeaturedScenariosQuery.
/// Returns scenarios marked as featured and active.
/// </summary>
public static class GetFeaturedScenariosQueryHandler
{
    /// <summary>
    /// Handles the GetFeaturedScenariosQuery by retrieving featured scenarios from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<Scenario>> Handle(
        GetFeaturedScenariosQuery query,
        IScenarioRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Retrieving featured scenarios");

        // Get featured and active scenarios
        var scenarios = await repository.GetQueryable()
            .Where(s => s.IsFeatured && s.IsActive)
            .OrderBy(s => s.Title)
            .ToListAsync(ct);

        logger.LogInformation("Found {Count} featured scenarios", scenarios.Count);

        return scenarios;
    }
}
