using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Handler for GetAchievementsQuery
/// Retrieves all achievements for a game session
/// </summary>
public class GetAchievementsQueryHandler : IQueryHandler<GetAchievementsQuery, List<SessionAchievement>>
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetAchievementsQueryHandler> _logger;

    public GetAchievementsQueryHandler(
        IGameSessionRepository repository,
        ILogger<GetAchievementsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<SessionAchievement>> Handle(
        GetAchievementsQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", request.SessionId);
            return new List<SessionAchievement>();
        }

        var achievements = session.Achievements ?? new List<SessionAchievement>();

        _logger.LogDebug("Retrieved {Count} achievements for session {SessionId}",
            achievements.Count, request.SessionId);

        return achievements;
    }
}
