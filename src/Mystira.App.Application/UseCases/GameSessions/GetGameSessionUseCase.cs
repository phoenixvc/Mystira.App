using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for retrieving a game session by ID
/// </summary>
public class GetGameSessionUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetGameSessionUseCase> _logger;

    public GetGameSessionUseCase(
        IGameSessionRepository repository,
        ILogger<GetGameSessionUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GameSession?> ExecuteAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        var session = await _repository.GetByIdAsync(sessionId);

        if (session == null)
        {
            _logger.LogWarning("Game session not found: {SessionId}", sessionId);
        }
        else
        {
            _logger.LogDebug("Retrieved game session: {SessionId}", sessionId);
        }

        return session;
    }
}

