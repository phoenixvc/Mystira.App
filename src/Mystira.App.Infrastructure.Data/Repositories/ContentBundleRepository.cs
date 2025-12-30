using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for ContentBundle entity
/// </summary>
public class ContentBundleRepository : Repository<ContentBundle>, IContentBundleRepository
{
    public ContentBundleRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ContentBundle>> GetByAgeGroupAsync(string ageGroup)
    {
        return await _dbSet.Where(b => b.AgeGroup == ageGroup).ToListAsync();
    }
}

