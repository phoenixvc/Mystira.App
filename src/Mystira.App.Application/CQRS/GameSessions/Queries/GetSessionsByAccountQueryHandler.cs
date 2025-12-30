using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Specifications;
using Mystira.Contracts.App.Models.GameSessions;
using Mystira.Contracts.App.Responses.GameSessions;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Wolverine handler for GetSessionsByAccountQuery.
/// Retrieves all sessions for a specific account.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class GetSessionsByAccountQueryHandler
{
    /// <summary>
    /// Handles the GetSessionsByAccountQuery.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<List<GameSessionResponse>> Handle(
        GetSessionsByAccountQuery request,
        IGameSessionRepository repository,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.AccountId))
        {
            throw new ArgumentException("AccountId is required");
        }

        var spec = new SessionsByAccountSpec(request.AccountId);
        var sessions = await repository.ListAsync(spec);

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

        logger.LogDebug("Retrieved {Count} sessions for account {AccountId}", response.Count, request.AccountId);

        return response;
    }
}
