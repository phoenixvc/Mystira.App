using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class BadgeRepository : Repository<Badge>, IBadgeRepository
{
    public BadgeRepository(MystiraAppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Badge>> GetByAgeGroupAsync(string ageGroupId)
    {
        // Avoid Cosmos ORDER BY to prevent composite index requirement; sort in memory at caller.
        return await _dbSet
            .Where(x => x.AgeGroupId == ageGroupId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Badge>> GetByCompassAxisAsync(string compassAxisId)
    {
        return await _dbSet
            .Where(x => x.CompassAxisId == compassAxisId)
            .OrderBy(x => x.AgeGroupId)
            .ThenBy(x => x.Tier)
            .ThenBy(x => x.TierOrder)
            .ToListAsync();
    }

    public async Task<Badge?> GetByAgeGroupAxisAndTierAsync(string ageGroupId, string compassAxisId, int tierOrder)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.AgeGroupId == ageGroupId
                                   && x.CompassAxisId == compassAxisId
                                   && x.TierOrder == tierOrder);
    }
}
