using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class AxisAchievementRepository : Repository<AxisAchievement>, IAxisAchievementRepository
{
    public AxisAchievementRepository(MystiraAppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AxisAchievement>> GetByAgeGroupAsync(string ageGroupId)
    {
        return await _dbSet
            .Where(x => x.AgeGroupId == ageGroupId)
            .ToListAsync();
    }

    public async Task<IEnumerable<AxisAchievement>> GetByCompassAxisAsync(string compassAxisId)
    {
        return await _dbSet
            .Where(x => x.CompassAxisId == compassAxisId)
            .ToListAsync();
    }
}
