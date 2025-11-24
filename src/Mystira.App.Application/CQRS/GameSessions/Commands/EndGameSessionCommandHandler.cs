using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Handler for EndGameSessionCommand
/// Marks a session as completed and sets the end time
/// </summary>
public class EndGameSessionCommandHandler : ICommandHandler<EndGameSessionCommand, GameSession?>
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EndGameSessionCommandHandler> _logger;

    public EndGameSessionCommandHandler(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<EndGameSessionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession?> Handle(
        EndGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", command.SessionId);
            return null;
        }

        // Update session status
        session.Status = SessionStatus.Completed;
        session.EndTime = DateTime.UtcNow;

        // Update in repository
        await _repository.UpdateAsync(session);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ended game session {SessionId}", session.Id);

        return session;
    }
}
