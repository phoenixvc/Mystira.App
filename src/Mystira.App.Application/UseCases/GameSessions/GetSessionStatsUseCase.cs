using Microsoft.Extensions.Logging;
using Mystira.App.Contracts.Responses.GameSessions;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for retrieving game session statistics
/// </summary>
public class GetSessionStatsUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetSessionStatsUseCase> _logger;

    public GetSessionStatsUseCase(
        IGameSessionRepository repository,
        ILogger<GetSessionStatsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SessionStatsResponse?> ExecuteAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Game session not found: {SessionId}", sessionId);
            return null;
        }

        var compassValues = session.CompassValues.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.CurrentValue
        );

        var recentEchoes = session.EchoHistory
            .OrderByDescending(e => e.Timestamp)
            .Take(5)
            .ToList();

        var stats = new SessionStatsResponse
        {
            CompassValues = compassValues,
            RecentEchoes = recentEchoes,
            Achievements = session.Achievements,
            TotalChoices = session.ChoiceHistory.Count,
            SessionDuration = session.EndTime?.Subtract(session.StartTime) ?? DateTime.UtcNow.Subtract(session.StartTime)
        };

        _logger.LogDebug("Retrieved stats for game session: {SessionId}", sessionId);
        return stats;
    }
}

