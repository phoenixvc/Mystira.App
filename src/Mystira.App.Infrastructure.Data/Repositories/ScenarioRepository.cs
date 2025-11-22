using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Scenario entity
/// </summary>
public class ScenarioRepository : Repository<Scenario>, IScenarioRepository
{
    public ScenarioRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Scenario>> GetByAgeGroupAsync(string ageGroup)
    {
        return await _dbSet.Where(s => s.AgeGroup == ageGroup).ToListAsync();
    }

    public async Task<Scenario?> GetByTitleAsync(string title)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Title == title);
    }

    public async Task<bool> ExistsByTitleAsync(string title)
    {
        return await _dbSet.AnyAsync(s => s.Title == title);
    }

    public IQueryable<Scenario> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<int> CountAsync(Expression<Func<Scenario, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _dbSet.CountAsync();
        }
        return await _dbSet.CountAsync(predicate);
    }
}

