using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Handler for MakeChoiceCommand
/// Records a player's choice in the game session and updates the current scene
/// </summary>
public class MakeChoiceCommandHandler : ICommandHandler<MakeChoiceCommand, GameSession?>
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MakeChoiceCommandHandler> _logger;

    public MakeChoiceCommandHandler(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<MakeChoiceCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession?> Handle(
        MakeChoiceCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrEmpty(request.SessionId))
        {
            throw new ArgumentException("SessionId is required");
        }

        if (string.IsNullOrEmpty(request.SceneId))
        {
            throw new ArgumentException("SceneId is required");
        }

        if (string.IsNullOrEmpty(request.ChoiceText))
        {
            throw new ArgumentException("ChoiceText is required");
        }

        if (string.IsNullOrEmpty(request.NextSceneId))
        {
            throw new ArgumentException("NextSceneId is required");
        }

        var session = await _repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", request.SessionId);
            return null;
        }

        if (session.Status != SessionStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot make choice in session with status: {session.Status}");
        }

        // Resolve deciding player using ActiveCharacter when possible (CQRS layer doesn't have scenario context,
        // so we rely on request providing SceneId and session CharacterAssignments)
        string? playerId = null;
        if (!string.IsNullOrWhiteSpace(request.SceneId))
        {
            // If the session already recorded the current scene, try to match assignment by SelectedCharacterId
            // Note: Without scenario context, we cannot read Scene.ActiveCharacter here.
            // We still prioritize explicit PlayerId in request when provided.
        }

        playerId = !string.IsNullOrWhiteSpace(request.PlayerId)
            ? request.PlayerId
            : session.ProfileId;

        var compassAxis = request.CompassAxis;
        var compassDirection = request.CompassDirection;
        var compassDelta = request.CompassDelta;

        var choice = new SessionChoice
        {
            SceneId = request.SceneId,
            ChoiceText = request.ChoiceText,
            NextScene = request.NextSceneId,
            PlayerId = playerId ?? string.Empty,
            CompassAxis = compassAxis,
            CompassDirection = compassDirection,
            CompassDelta = compassDelta,
            ChosenAt = DateTime.UtcNow,
            CompassChange = !string.IsNullOrWhiteSpace(compassAxis) && compassDelta.HasValue
                ? new CompassChange { Axis = compassAxis, Delta = compassDelta.Value }
                : null
        };

        session.ChoiceHistory ??= new List<SessionChoice>();
        session.ChoiceHistory.Add(choice);

        session.CurrentSceneId = request.NextSceneId;
        session.ElapsedTime = DateTime.UtcNow - session.StartTime;

        session.RecalculateCompassProgressFromHistory();

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded choice in session {SessionId}: {ChoiceText} -> {NextSceneId} (PlayerId={PlayerId})",
            session.Id,
            request.ChoiceText,
            request.NextSceneId,
            playerId);

        return session;
    }
}
