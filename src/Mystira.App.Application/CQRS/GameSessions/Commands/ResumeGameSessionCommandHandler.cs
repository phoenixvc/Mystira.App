using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Wolverine handler for ResumeGameSessionCommand.
/// Resumes a paused game session.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class ResumeGameSessionCommandHandler
{
    /// <summary>
    /// Handles the ResumeGameSessionCommand.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<GameSession?> Handle(
        ResumeGameSessionCommand command,
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

        if (session.Status != SessionStatus.Paused)
        {
            logger.LogWarning("Session {SessionId} was not paused. Current status: {Status}. Proceeding anyway.",
                command.SessionId, session.Status);
        }

        // Calculate elapsed time during pause and add to total
        if (session.PausedAt.HasValue)
        {
            // The model's GetTotalElapsedTime handles this, but we could also update ElapsedTime here
            logger.LogDebug("Session was paused for {Duration}", DateTime.UtcNow - session.PausedAt.Value);
        }

        // Update session status
        session.Status = SessionStatus.InProgress;
        session.IsPaused = false;
        session.PausedAt = null;

        // Update in repository
        await repository.UpdateAsync(session);

        // Persist changes
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Resumed game session {SessionId}", session.Id);

        return session;
    }
}
