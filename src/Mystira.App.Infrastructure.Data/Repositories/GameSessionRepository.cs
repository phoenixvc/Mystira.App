using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for GameSession entity with domain-specific queries
/// </summary>
public class GameSessionRepository : Repository<GameSession>, IGameSessionRepository
{
    public GameSessionRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<GameSession>> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Where(s => s.AccountId == accountId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<GameSession>> GetByProfileIdAsync(string profileId)
    {
        return await _dbSet
            .Where(s => s.ProfileId == profileId || s.PlayerNames.Contains(profileId))
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<GameSession>> GetInProgressSessionsAsync(string accountId)
    {
        return await _dbSet
            .Where(s => s.AccountId == accountId && 
                       (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused))
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<GameSession?> GetActiveSessionForScenarioAsync(string accountId, string scenarioId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.AccountId == accountId &&
                                      s.ScenarioId == scenarioId &&
                                      (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused));
    }

    public async Task<int> GetActiveSessionsCountAsync()
    {
        return await _dbSet
            .CountAsync(s => s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused);
    }
}

