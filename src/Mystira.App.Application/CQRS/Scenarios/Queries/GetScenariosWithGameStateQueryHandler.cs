using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.Scenarios;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Handler for retrieving scenarios with associated game session state.
/// Combines scenario data with player's game session progress.
/// </summary>
public class GetScenariosWithGameStateQueryHandler
    : IQueryHandler<GetScenariosWithGameStateQuery, ScenarioGameStateResponse>
{
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ILogger<GetScenariosWithGameStateQueryHandler> _logger;

    public GetScenariosWithGameStateQueryHandler(
        IScenarioRepository scenarioRepository,
        IGameSessionRepository gameSessionRepository,
        ILogger<GetScenariosWithGameStateQueryHandler> logger)
    {
        _scenarioRepository = scenarioRepository;
        _gameSessionRepository = gameSessionRepository;
        _logger = logger;
    }

    public async Task<ScenarioGameStateResponse> Handle(
        GetScenariosWithGameStateQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving scenarios with game state for account: {AccountId}",
            request.AccountId);

        // 1. Get all active scenarios using direct LINQ query
        var scenarios = await _scenarioRepository.GetQueryable()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Title)
            .ToListAsync(cancellationToken);

        // 2. Get all game sessions for this account
        var gameSessions = await _gameSessionRepository.GetByAccountIdAsync(request.AccountId);

        // 3. Build response with game state
        var scenariosWithState = scenarios.Select(scenario =>
        {
            var sessions = gameSessions
                .Where(gs => gs.ScenarioId == scenario.Id)
                .OrderByDescending(gs => gs.StartTime)
                .ToList();

            var lastSession = sessions.FirstOrDefault();
            var completedCount = sessions.Count(gs => gs.Status == Domain.Models.SessionStatus.Completed);

            return new ScenarioWithGameState
            {
                Scenario = scenario,
                HasPlayed = sessions.Any(),
                IsCompleted = completedCount > 0,
                CompletedCount = completedCount,
                LastPlayedAt = lastSession?.StartTime,
                LastSessionStatus = lastSession?.Status.ToString()
            };
        }).ToList();

        var response = new ScenarioGameStateResponse
        {
            Scenarios = scenariosWithState,
            TotalScenarios = scenarios.Count,
            PlayedScenarios = scenariosWithState.Count(s => s.HasPlayed),
            CompletedScenarios = scenariosWithState.Count(s => s.IsCompleted)
        };

        _logger.LogInformation(
            "Retrieved {Total} scenarios: {Played} played, {Completed} completed for account {AccountId}",
            response.TotalScenarios,
            response.PlayedScenarios,
            response.CompletedScenarios,
            request.AccountId);

        return response;
    }
}
