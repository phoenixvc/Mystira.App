using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Wolverine handler for GetPaginatedScenariosQuery.
/// </summary>
public static class GetPaginatedScenariosQueryHandler
{
    /// <summary>
    /// Handles the GetPaginatedScenariosQuery by retrieving paginated scenarios from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<IEnumerable<Scenario>> Handle(
        GetPaginatedScenariosQuery query,
        IScenarioRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        // Use queryable for pagination
        var scenarios = await repository.GetQueryable()
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        logger.LogDebug("Retrieved page {PageNumber} with {Count} scenarios (page size: {PageSize})",
            query.PageNumber, scenarios.Count, query.PageSize);

        return scenarios;
    }
}
