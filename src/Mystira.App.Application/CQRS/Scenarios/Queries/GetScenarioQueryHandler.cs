using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Wolverine handler for GetScenarioQuery.
/// Retrieves a single scenario by ID.
/// </summary>
public static class GetScenarioQueryHandler
{
    /// <summary>
    /// Handles the GetScenarioQuery by retrieving a scenario from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Scenario?> Handle(
        GetScenarioQuery query,
        IScenarioRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.ScenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(query.ScenarioId));
        }

        var scenario = await repository.GetByIdAsync(query.ScenarioId);

        if (scenario == null)
        {
            logger.LogWarning("Scenario not found: {ScenarioId}", query.ScenarioId);
        }
        else
        {
            logger.LogDebug("Retrieved scenario: {ScenarioId}", query.ScenarioId);
        }

        return scenario;
    }
}
