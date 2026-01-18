using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Models.GameSessions;
using Mystira.Contracts.App.Responses.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Wolverine handler for GetSessionsByProfileQuery.
/// Retrieves all sessions for a specific user profile.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetSessionsByProfileQueryHandler
{
    /// <summary>
    /// Handles the GetSessionsByProfileQuery.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<GameSessionResponse>> Handle(
        GetSessionsByProfileQuery request,
        IGameSessionRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.ProfileId))
        {
            throw new ArgumentException("ProfileId is required");
        }

        var sessions = await repository.GetByProfileIdAsync(request.ProfileId);

        var response = sessions.Select(s =>
        {
            s.RecalculateCompassProgressFromHistory();

            return new GameSessionResponse
            {
                Id = s.Id,
                ScenarioId = s.ScenarioId,
                AccountId = s.AccountId,
                ProfileId = s.ProfileId,
                PlayerNames = s.PlayerNames,
                PlayerCompassProgressTotals = s.PlayerCompassProgressTotals.Select(p => new PlayerCompassProgressDto
                {
                    PlayerId = p.PlayerId,
                    Axis = p.Axis,
                    Total = (int)p.Total
                }).ToList(),
                Status = s.Status.ToString(),
                CurrentSceneId = s.CurrentSceneId,
                ChoiceCount = s.ChoiceHistory?.Count ?? 0,
                EchoCount = s.EchoHistory?.Count ?? 0,
                AchievementCount = s.Achievements?.Count ?? 0,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                ElapsedTime = s.GetTotalElapsedTime(),
                IsPaused = s.Status == Domain.Models.SessionStatus.Paused,
                SceneCount = s.ChoiceHistory?.Select(c => c.SceneId).Distinct().Count() ?? 0,
                TargetAgeGroup = s.TargetAgeGroup.Value
            };
        }).ToList();

        logger.LogDebug("Retrieved {Count} sessions for profile {ProfileId}", response.Count, request.ProfileId);

        return response;
    }
}
