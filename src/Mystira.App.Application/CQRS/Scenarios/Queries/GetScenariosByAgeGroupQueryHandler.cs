using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Wolverine handler for GetScenariosByAgeGroupQuery.
/// Demonstrates how to use Specification Pattern with CQRS queries.
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
        // Create specification for scenarios by age group
        var spec = new ScenariosByAgeGroupSpec(query.AgeGroup);

        // Use specification to query repository
        var scenarios = await repository.ListAsync(spec);

        logger.LogDebug("Retrieved {Count} scenarios for age group: {AgeGroup}",
            scenarios.Count(), query.AgeGroup);

        return scenarios;
    }
}
