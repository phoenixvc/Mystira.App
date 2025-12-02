using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Handler for StartGameSessionCommand
/// Creates a new game session with initial state
/// </summary>
public class StartGameSessionCommandHandler : ICommandHandler<StartGameSessionCommand, GameSession>
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StartGameSessionCommandHandler> _logger;

    public StartGameSessionCommandHandler(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<StartGameSessionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession> Handle(
        StartGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        // Validate request
        if (string.IsNullOrEmpty(request.ScenarioId))
            throw new ArgumentException("ScenarioId is required");
        if (string.IsNullOrEmpty(request.AccountId))
            throw new ArgumentException("AccountId is required");
        if (string.IsNullOrEmpty(request.ProfileId))
            throw new ArgumentException("ProfileId is required");
        if (request.PlayerNames == null || !request.PlayerNames.Any())
            throw new ArgumentException("At least one player name is required");

        // Create new game session
        var session = new GameSession
        {
            Id = Guid.NewGuid().ToString("N"),
            ScenarioId = request.ScenarioId,
            AccountId = request.AccountId,
            ProfileId = request.ProfileId,
            PlayerNames = request.PlayerNames,
            TargetAgeGroup = AgeGroup.Parse(request.TargetAgeGroup) ?? new AgeGroup(6, 9),
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow,
            ChoiceHistory = new List<SessionChoice>(),
            EchoHistory = new List<EchoLog>(),
            Achievements = new List<SessionAchievement>(),
            CompassValues = new Dictionary<string, CompassTracking>()
        };

        // Add to repository
        await _repository.AddAsync(session);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Started game session {SessionId} for scenario {ScenarioId}, account {AccountId}",
            session.Id, request.ScenarioId, request.AccountId);

        return session;
    }
}
