using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Wolverine handler for PauseGameSessionCommand.
/// Pauses an active game session and records the pause time.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class PauseGameSessionCommandHandler
{
    /// <summary>
    /// Handles the PauseGameSessionCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<GameSession?> Handle(
        PauseGameSessionCommand command,
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        var session = await repository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            logger.LogWarning("Session not found: {SessionId}", command.SessionId);
            return null;
        }

        if (session.Status != SessionStatus.InProgress)
        {
            logger.LogWarning("Cannot pause session {SessionId} - not in progress. Current status: {Status}",
                command.SessionId, session.Status);
            return null;
        }

        // Update session status
        session.Status = SessionStatus.Paused;
        session.IsPaused = true;
        session.PausedAt = DateTime.UtcNow;

        // Update in repository
        await repository.UpdateAsync(session);

        // Persist changes
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Paused game session {SessionId}", session.Id);

        return session;
    }
}
