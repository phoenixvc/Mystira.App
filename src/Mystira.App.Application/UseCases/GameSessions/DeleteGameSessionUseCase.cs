using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for deleting a game session
/// </summary>
public class DeleteGameSessionUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteGameSessionUseCase> _logger;

    public DeleteGameSessionUseCase(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteGameSessionUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Game session not found for deletion: {SessionId}", sessionId);
            return false;
        }

        await _repository.DeleteAsync(sessionId);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted game session: {SessionId}", sessionId);
        return true;
    }
}

