using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Wolverine handler for EndGameSessionCommand.
/// Marks a session as completed and sets the end time.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class EndGameSessionCommandHandler
{
    /// <summary>
    /// Handles the EndGameSessionCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<GameSession?> Handle(
        EndGameSessionCommand command,
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

        // Update session status
        session.Status = SessionStatus.Completed;
        session.EndTime = DateTime.UtcNow;

        // Update in repository
        await repository.UpdateAsync(session);

        // Persist changes
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Ended game session {SessionId}", session.Id);

        return session;
    }
}
