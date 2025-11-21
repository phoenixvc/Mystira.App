using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for GameSession entity with domain-specific queries
/// </summary>
public interface IGameSessionRepository : IRepository<GameSession>
{
    Task<IEnumerable<GameSession>> GetByAccountIdAsync(string accountId);
    Task<IEnumerable<GameSession>> GetByProfileIdAsync(string profileId);
    Task<IEnumerable<GameSession>> GetInProgressSessionsAsync(string accountId);
    Task<GameSession?> GetActiveSessionForScenarioAsync(string accountId, string scenarioId);
    Task<int> GetActiveSessionsCountAsync();
}

