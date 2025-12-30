using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Wolverine handler for MakeChoiceCommand.
/// Records a player's choice in the game session and updates the current scene.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class MakeChoiceCommandHandler
{
    /// <summary>
    /// Handles the MakeChoiceCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<GameSession?> Handle(
        MakeChoiceCommand command,
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
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

        var session = await repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            logger.LogWarning("Session not found: {SessionId}", request.SessionId);
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

        await repository.UpdateAsync(session);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded choice in session {SessionId}: {ChoiceText} -> {NextSceneId} (PlayerId={PlayerId})",
            session.Id,
            request.ChoiceText,
            request.NextSceneId,
            playerId);

        return session;
    }
}
