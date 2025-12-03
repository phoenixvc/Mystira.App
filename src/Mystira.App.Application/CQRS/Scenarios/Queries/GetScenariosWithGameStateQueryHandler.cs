using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            var hasPlayed = sessions.Any();
            var isCompleted = completedCount > 0;

            // Determine game state enum
            var gameState = !hasPlayed ? ScenarioGameState.NotStarted
                : isCompleted ? ScenarioGameState.Completed
                : ScenarioGameState.InProgress;

            return new ScenarioWithGameState
            {
                ScenarioId = scenario.Id,
                Title = scenario.Title,
                Description = scenario.Description,
                AgeGroup = scenario.AgeGroup,
                Difficulty = scenario.Difficulty.ToString(),
                SessionLength = scenario.SessionLength.ToString(),
                CoreAxes = scenario.CoreAxes?.Select(a => a.Value).ToList() ?? new List<string>(),
                Tags = scenario.Tags?.ToArray() ?? [],
                Archetypes = scenario.Archetypes?.Select(a => a.ToString()).ToArray() ?? [],
                GameState = gameState,
                LastPlayedAt = lastSession?.StartTime,
                PlayCount = sessions.Count,
                Image = scenario.Image
            };
        }).ToList();

        var response = new ScenarioGameStateResponse
        {
            Scenarios = scenariosWithState,
            TotalCount = scenarios.Count
        };

        _logger.LogInformation(
            "Retrieved {Total} scenarios for account {AccountId}",
            response.TotalCount,
            request.AccountId);

        return response;
    }
}
