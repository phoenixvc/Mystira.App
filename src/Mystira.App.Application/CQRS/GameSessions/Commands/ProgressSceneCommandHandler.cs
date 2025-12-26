using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Wolverine handler for ProgressSceneCommand.
/// Updates the current scene of a game session.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class ProgressSceneCommandHandler
{
    /// <summary>
    /// Handles the ProgressSceneCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<GameSession?> Handle(
        ProgressSceneCommand command,
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        var request = command.Request;

        // Validate request
        if (string.IsNullOrEmpty(request.SessionId))
        {
            throw new ArgumentException("SessionId is required");
        }

        if (string.IsNullOrEmpty(request.SceneId))
        {
            throw new ArgumentException("SceneId is required");
        }

        var session = await repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            logger.LogWarning("Session not found: {SessionId}", request.SessionId);
            return null;
        }

        // Verify session is in progress
        if (session.Status != SessionStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot progress scene in session with status: {session.Status}");
        }

        // Update current scene
        session.CurrentSceneId = request.SceneId;

        // Update in repository
        await repository.UpdateAsync(session);

        // Persist changes
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Progressed session {SessionId} to scene {SceneId}",
            session.Id, request.SceneId);

        return session;
    }
}
