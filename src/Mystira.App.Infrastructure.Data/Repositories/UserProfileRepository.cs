using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for UserProfile entity with domain-specific queries
/// </summary>
public class UserProfileRepository : Repository<UserProfile>, IUserProfileRepository
{
    public UserProfileRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserProfile>> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Include(p => p.EarnedBadges)
            .Where(p => p.AccountId == accountId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserProfile>> GetGuestProfilesAsync()
    {
        return await _dbSet
            .Include(p => p.EarnedBadges)
            .Where(p => p.IsGuest)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserProfile>> GetNonGuestProfilesAsync()
    {
        return await _dbSet
            .Include(p => p.EarnedBadges)
            .Where(p => !p.IsGuest)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
