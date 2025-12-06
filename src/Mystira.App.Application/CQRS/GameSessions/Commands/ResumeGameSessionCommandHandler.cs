using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Handler for ResumeGameSessionCommand
/// Resumes a paused game session
/// </summary>
public class ResumeGameSessionCommandHandler : ICommandHandler<ResumeGameSessionCommand, GameSession?>
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResumeGameSessionCommandHandler> _logger;

    public ResumeGameSessionCommandHandler(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ResumeGameSessionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession?> Handle(
        ResumeGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", command.SessionId);
            return null;
        }

        if (session.Status != SessionStatus.Paused)
        {
            _logger.LogWarning("Cannot resume session {SessionId} - not paused. Current status: {Status}",
                command.SessionId, session.Status);
            return null;
        }

        // Calculate elapsed time during pause and add to total
        if (session.PausedAt.HasValue)
        {
            // The model's GetTotalElapsedTime handles this, but we could also update ElapsedTime here
            _logger.LogDebug("Session was paused for {Duration}", DateTime.UtcNow - session.PausedAt.Value);
        }

        // Update session status
        session.Status = SessionStatus.InProgress;
        session.IsPaused = false;
        session.PausedAt = null;

        // Update in repository
        await _repository.UpdateAsync(session);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Resumed game session {SessionId}", session.Id);

        return session;
    }
}
