using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Models.GameSessions;
using Mystira.App.Contracts.Responses.GameSessions;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Handler for GetInProgressSessionsQuery
/// Retrieves sessions that are currently in progress or paused
/// </summary>
public class GetInProgressSessionsQueryHandler : IQueryHandler<GetInProgressSessionsQuery, List<GameSessionResponse>>
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetInProgressSessionsQueryHandler> _logger;

    public GetInProgressSessionsQueryHandler(
        IGameSessionRepository repository,
        ILogger<GetInProgressSessionsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<GameSessionResponse>> Handle(
        GetInProgressSessionsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.AccountId))
        {
            throw new ArgumentException("AccountId is required");
        }

        var spec = new InProgressSessionsSpecification(request.AccountId);
        var sessions = await _repository.ListAsync(spec);

        // Defensive: if historical data contains duplicates (e.g., retries that created multiple active sessions),
        // only return the most recent active session per (ScenarioId, ProfileId) pair.
        var ordered = sessions
            .OrderByDescending(s => s.StartTime)
            .ToList();

        // Filter out "zombie" sessions: active status but with no starting scene and no history.
        // These can be created by partial start flows (e.g., character assignment completed but game never began).
        var meaningfulSessions = ordered
            .Where(s => !IsEffectivelyEmptyActiveSession(s))
            .ToList();

        if (meaningfulSessions.Count != ordered.Count)
        {
            _logger.LogWarning(
                "Filtered empty in-progress sessions for account {AccountId}: {OriginalCount} -> {FilteredCount}",
                request.AccountId,
                ordered.Count,
                meaningfulSessions.Count);
        }

        var uniqueSessions = meaningfulSessions
            .GroupBy(s => $"{s.ScenarioId}::{s.ProfileId}".ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        if (uniqueSessions.Count != meaningfulSessions.Count)
        {
            _logger.LogWarning(
                "Deduplicated in-progress sessions for account {AccountId}: {OriginalCount} -> {UniqueCount}",
                request.AccountId,
                meaningfulSessions.Count,
                uniqueSessions.Count);
        }

        var response = uniqueSessions.Select(s =>
        {
            s.RecalculateCompassProgressFromHistory();

            return new GameSessionResponse
            {
                Id = s.Id,
                ScenarioId = s.ScenarioId,
                AccountId = s.AccountId,
                ProfileId = s.ProfileId,
                PlayerNames = s.PlayerNames,
                CharacterAssignments = s.CharacterAssignments?.Select(ca => new CharacterAssignmentDto
                {
                    CharacterId = ca.CharacterId,
                    CharacterName = ca.CharacterName,
                    Image = ca.Image,
                    Audio = ca.Audio,
                    Role = ca.Role,
                    Archetype = ca.Archetype,
                    IsUnused = ca.IsUnused,
                    PlayerAssignment = ca.PlayerAssignment == null ? null : new PlayerAssignmentDto
                    {
                        Type = ca.PlayerAssignment.Type,
                        ProfileId = ca.PlayerAssignment.ProfileId,
                        ProfileName = ca.PlayerAssignment.ProfileName,
                        ProfileImage = ca.PlayerAssignment.ProfileImage,
                        SelectedAvatarMediaId = ca.PlayerAssignment.SelectedAvatarMediaId,
                        GuestName = ca.PlayerAssignment.GuestName,
                        GuestAgeRange = ca.PlayerAssignment.GuestAgeRange,
                        GuestAvatar = ca.PlayerAssignment.GuestAvatar,
                        SaveAsProfile = ca.PlayerAssignment.SaveAsProfile
                    }
                }).ToList() ?? new List<CharacterAssignmentDto>(),
                PlayerCompassProgressTotals = s.PlayerCompassProgressTotals.Select(p => new PlayerCompassProgressDto
                {
                    PlayerId = p.PlayerId,
                    Axis = p.Axis,
                    Total = p.Total
                }).ToList(),
                Status = s.Status,
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

        _logger.LogDebug("Retrieved {Count} in-progress sessions for account {AccountId}", response.Count, request.AccountId);

        return response;
    }

    private static bool IsEffectivelyEmptyActiveSession(GameSession session)
    {
        // "Empty" means: no known current scene AND no history. These sessions are not useful to resume.
        var hasScene = !string.IsNullOrWhiteSpace(session.CurrentSceneId);
        var hasChoices = session.ChoiceHistory?.Count > 0;
        var hasEchoes = session.EchoHistory?.Count > 0;
        var hasAchievements = session.Achievements?.Count > 0;

        return !hasScene && !hasChoices && !hasEchoes && !hasAchievements;
    }
}
