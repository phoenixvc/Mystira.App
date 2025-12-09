using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.GameSessions.Commands;

/// <summary>
/// Handler for ProgressSceneCommand
/// Updates the current scene of a game session
/// </summary>
public class ProgressSceneCommandHandler : ICommandHandler<ProgressSceneCommand, GameSession?>
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProgressSceneCommandHandler> _logger;

    public ProgressSceneCommandHandler(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ProgressSceneCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession?> Handle(
        ProgressSceneCommand command,
        CancellationToken cancellationToken)
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

        var session = await _repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", request.SessionId);
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
        await _repository.UpdateAsync(session);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Progressed session {SessionId} to scene {SceneId}",
            session.Id, request.SceneId);

        return session;
    }
}
