using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for assigning a character to a game session
/// </summary>
public class SelectCharacterUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SelectCharacterUseCase> _logger;

    public SelectCharacterUseCase(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<SelectCharacterUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameSession> ExecuteAsync(string sessionId, string characterId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            throw new ArgumentException("Character ID cannot be null or empty", nameof(characterId));
        }

        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Game session not found: {sessionId}", nameof(sessionId));
        }

        session.SelectedCharacterId = characterId;
        await _repository.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Selected character {CharacterId} for game session {SessionId}", characterId, sessionId);
        return session;
    }
}

