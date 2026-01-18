using Ardalis.Specification;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for GameSession entity with domain-specific queries
/// </summary>
public interface IGameSessionRepository : IRepository<GameSession, string>, IRepositoryBase<GameSession>
{
    Task<IEnumerable<GameSession>> GetByAccountIdAsync(string accountId);
    Task<IEnumerable<GameSession>> GetByProfileIdAsync(string profileId);
    Task<IEnumerable<GameSession>> GetInProgressSessionsAsync(string accountId);
    Task<GameSession?> GetActiveSessionForScenarioAsync(string accountId, string scenarioId);
    Task<IEnumerable<GameSession>> GetActiveSessionsByScenarioAndAccountAsync(string scenarioId, string accountId);
    Task<int> GetActiveSessionsCountAsync();
}

