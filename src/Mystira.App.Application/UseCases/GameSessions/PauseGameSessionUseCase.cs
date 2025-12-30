using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for pausing an active game session
/// </summary>
public class PauseGameSessionUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PauseGameSessionUseCase> _logger;

    public PauseGameSessionUseCase(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PauseGameSessionUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession> ExecuteAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Game session not found: {sessionId}", nameof(sessionId));
        }

        if (session.Status != SessionStatus.InProgress)
        {
            throw new InvalidOperationException($"Can only pause sessions in progress. Current status: {session.Status}");
        }

        session.Status = SessionStatus.Paused;
        session.IsPaused = true;
        session.PausedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Paused game session: {SessionId}", sessionId);
        return session;
    }
}

