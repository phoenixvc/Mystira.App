using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Handler for PauseGameSessionCommand
/// Pauses an active game session and records the pause time
/// </summary>
public class PauseGameSessionCommandHandler : ICommandHandler<PauseGameSessionCommand, GameSession?>
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PauseGameSessionCommandHandler> _logger;

    public PauseGameSessionCommandHandler(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PauseGameSessionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession?> Handle(
        PauseGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", command.SessionId);
            return null;
        }

        if (session.Status != SessionStatus.InProgress)
        {
            _logger.LogWarning("Cannot pause session {SessionId} - not in progress. Current status: {Status}",
                command.SessionId, session.Status);
            return null;
        }

        // Update session status
        session.Status = SessionStatus.Paused;
        session.IsPaused = true;
        session.PausedAt = DateTime.UtcNow;

        // Update in repository
        await _repository.UpdateAsync(session);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Paused game session {SessionId}", session.Id);

        return session;
    }
}
