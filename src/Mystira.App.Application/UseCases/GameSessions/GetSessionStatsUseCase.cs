using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Models.GameSessions;
using Mystira.Contracts.App.Responses.GameSessions;

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

        session.RecalculateCompassProgressFromHistory();

        var compassValues = session.CompassValues.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.CurrentValue
        );

        var progress = session.PlayerCompassProgressTotals
            .Select(p => new PlayerCompassProgressDto
            {
                PlayerId = p.PlayerId,
                Axis = p.Axis,
                Total = (int)p.Total
            })
            .ToList();

        var recentEchoes = session.EchoHistory
            .OrderByDescending(e => e.Timestamp)
            .Take(5)
            .Cast<object>()
            .ToList();

        var stats = new SessionStatsResponse
        {
            CompassValues = compassValues,
            PlayerCompassProgressTotals = progress,
            RecentEchoes = recentEchoes,
            Achievements = session.Achievements?.Cast<object>().ToList() ?? new List<object>(),
            TotalChoices = session.ChoiceHistory?.Count ?? 0,
            SessionDuration = session.EndTime?.Subtract(session.StartTime) ?? DateTime.UtcNow.Subtract(session.StartTime)
        };

        _logger.LogDebug("Retrieved stats for game session: {SessionId}", sessionId);
        return stats;
    }
}
