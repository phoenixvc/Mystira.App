using Mystira.App.Contracts.Responses.Scenarios;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve scenarios with game state for a specific account.
/// Returns scenarios along with associated game session progress.
/// Not cached as game state changes frequently.
/// </summary>
public record GetScenariosWithGameStateQuery(string AccountId)
    : IQuery<ScenarioGameStateResponse>;
