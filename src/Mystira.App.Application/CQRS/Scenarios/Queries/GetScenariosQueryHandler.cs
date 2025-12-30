using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Wolverine handler for GetScenariosQuery.
/// Retrieves all scenarios - this is a read-only operation that doesn't modify state.
/// </summary>
public static class GetScenariosQueryHandler
{
    /// <summary>
    /// Handles the GetScenariosQuery by retrieving all scenarios from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<IEnumerable<Scenario>> Handle(
        GetScenariosQuery query,
        IScenarioRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        var scenarios = await repository.GetAllAsync();

        logger.LogDebug("Retrieved {Count} scenarios", scenarios.Count());

        return scenarios;
    }
}
