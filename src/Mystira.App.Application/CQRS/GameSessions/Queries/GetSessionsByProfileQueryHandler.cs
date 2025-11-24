using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Contracts.Responses.GameSessions;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.GameSessions.Queries;

/// <summary>
/// Handler for GetSessionsByProfileQuery
/// Retrieves all sessions for a specific user profile
/// </summary>
public class GetSessionsByProfileQueryHandler : IQueryHandler<GetSessionsByProfileQuery, List<GameSessionResponse>>
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetSessionsByProfileQueryHandler> _logger;

    public GetSessionsByProfileQueryHandler(
        IGameSessionRepository repository,
        ILogger<GetSessionsByProfileQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<GameSessionResponse>> Handle(
        GetSessionsByProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ProfileId))
            throw new ArgumentException("ProfileId is required");

        var spec = new SessionsByProfileSpecification(request.ProfileId);
        var sessions = await _repository.ListAsync(spec);

        var response = sessions.Select(s => new GameSessionResponse
        {
            Id = s.Id,
            ScenarioId = s.ScenarioId,
            AccountId = s.AccountId,
            ProfileId = s.ProfileId,
            PlayerNames = s.PlayerNames,
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
        }).ToList();

        _logger.LogDebug("Retrieved {Count} sessions for profile {ProfileId}", response.Count, request.ProfileId);

        return response;
    }
}
