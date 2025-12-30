using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Wolverine handler for GetAchievementsQuery.
/// Retrieves all achievements for a game session.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetAchievementsQueryHandler
{
    /// <summary>
    /// Handles the GetAchievementsQuery.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<SessionAchievement>> Handle(
        GetAchievementsQuery request,
        IGameSessionRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        var session = await repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            logger.LogWarning("Session not found: {SessionId}", request.SessionId);
            return new List<SessionAchievement>();
        }

        var achievements = session.Achievements ?? new List<SessionAchievement>();

        logger.LogDebug("Retrieved {Count} achievements for session {SessionId}",
            achievements.Count, request.SessionId);

        return achievements;
    }
}
