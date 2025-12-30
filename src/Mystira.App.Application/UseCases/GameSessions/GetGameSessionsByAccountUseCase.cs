using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.UseCases.GameSessions;

/// <summary>
/// Use case for retrieving game sessions by account ID
/// </summary>
public class GetGameSessionsByAccountUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly ILogger<GetGameSessionsByAccountUseCase> _logger;

    public GetGameSessionsByAccountUseCase(
        IGameSessionRepository repository,
        ILogger<GetGameSessionsByAccountUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<GameSession>> ExecuteAsync(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        var sessions = await _repository.GetByAccountIdAsync(accountId);
        var sessionList = sessions.ToList();

        _logger.LogInformation("Retrieved {Count} game sessions for account {AccountId}", sessionList.Count, accountId);

        return sessionList;
    }
}

