using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Models.GameSessions;
using Mystira.App.Contracts.Responses.GameSessions;
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
}
