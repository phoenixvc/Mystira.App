using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for UserBadge entity
/// </summary>
public class UserBadgeRepository : Repository<UserBadge>, IUserBadgeRepository
{
    public UserBadgeRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserBadge>> GetByUserProfileIdAsync(string userProfileId)
    {
        return await _dbSet
            .Where(b => b.UserProfileId == userProfileId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<UserBadge?> GetByUserProfileIdAndBadgeConfigIdAsync(string userProfileId, string badgeConfigurationId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(b => b.UserProfileId == userProfileId && b.BadgeConfigurationId == badgeConfigurationId);
    }

    public async Task<IEnumerable<UserBadge>> GetByGameSessionIdAsync(string gameSessionId)
    {
        return await _dbSet
            .Where(b => b.GameSessionId == gameSessionId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserBadge>> GetByScenarioIdAsync(string scenarioId)
    {
        return await _dbSet
            .Where(b => b.ScenarioId == scenarioId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserBadge>> GetByUserProfileIdAndAxisAsync(string userProfileId, string axis)
    {
        return await _dbSet
            .Where(b => b.UserProfileId == userProfileId && b.Axis == axis)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync();
    }
}

