using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Wolverine handler for GetScenariosByAgeGroupQuery.
/// </summary>
public static class GetScenariosByAgeGroupQueryHandler
{
    /// <summary>
    /// Handles the GetScenariosByAgeGroupQuery by retrieving scenarios filtered by age group from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<IEnumerable<Scenario>> Handle(
        GetScenariosByAgeGroupQuery query,
        IScenarioRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        // Use domain repository method to query scenarios by age group
        var scenarios = await repository.GetByAgeGroupAsync(query.AgeGroup);

        logger.LogDebug("Retrieved {Count} scenarios for age group: {AgeGroup}",
            scenarios.Count(), query.AgeGroup);

        return scenarios;
    }
}
