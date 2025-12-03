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

        // Validate request
        if (string.IsNullOrEmpty(request.SessionId))
            throw new ArgumentException("SessionId is required");
        if (string.IsNullOrEmpty(request.SceneId))
            throw new ArgumentException("SceneId is required");
        if (string.IsNullOrEmpty(request.ChoiceText))
            throw new ArgumentException("ChoiceText is required");
        if (string.IsNullOrEmpty(request.NextSceneId))
            throw new ArgumentException("NextSceneId is required");

        var session = await _repository.GetByIdAsync(request.SessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", request.SessionId);
            return null;
        }

        // Verify session is in progress
        if (session.Status != SessionStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot make choice in session with status: {session.Status}");
        }

        // Record the choice
        var choice = new SessionChoice
        {
            SceneId = request.SceneId,
            ChoiceText = request.ChoiceText,
            NextScene = request.NextSceneId,
            ChosenAt = DateTime.UtcNow
        };

        session.ChoiceHistory ??= new List<SessionChoice>();
        session.ChoiceHistory.Add(choice);

        // Update current scene
        session.CurrentSceneId = request.NextSceneId;

        // Update in repository
        await _repository.UpdateAsync(session);

        // Persist changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded choice in session {SessionId}: {ChoiceText} -> {NextSceneId}",
            session.Id, request.ChoiceText, request.NextSceneId);

        return session;
    }
}
