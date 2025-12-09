using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Handler for GetSessionStatsQuery
/// Calculates and returns session statistics
/// </summary>
public class GetSessionStatsQueryHandler : IQueryHandler<GetSessionStatsQuery, SessionStatsResponse?>
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetSessionStatsQueryHandler> _logger;

    public GetSessionStatsQueryHandler(
        IGameSessionRepository repository,
        ILogger<GetSessionStatsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SessionStatsResponse?> Handle(
        GetSessionStatsQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", request.SessionId);
            return null;
        }

        // Build compass values (convert CompassTracking.CurrentValue to double)
        var compassValues = session.CompassValues?.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.CurrentValue
        ) ?? new Dictionary<string, double>();

        // Get recent echo logs from EchoHistory
        var recentEchoes = session.EchoHistory?
            .TakeLast(10)
            .ToList() ?? new List<EchoLog>();

        var stats = new SessionStatsResponse
        {
            CompassValues = compassValues,
            RecentEchoes = recentEchoes,
            Achievements = session.Achievements ?? new List<SessionAchievement>(),
            TotalChoices = session.ChoiceHistory?.Count ?? 0,
            SessionDuration = session.GetTotalElapsedTime()
        };

        _logger.LogDebug("Retrieved stats for session {SessionId}", request.SessionId);

        return stats;
    }
}
