using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
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
            throw new ArgumentException("AccountId is required");

        var spec = new InProgressSessionsSpecification(request.AccountId);
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

        _logger.LogDebug("Retrieved {Count} in-progress sessions for account {AccountId}", response.Count, request.AccountId);

        return response;
    }
}
