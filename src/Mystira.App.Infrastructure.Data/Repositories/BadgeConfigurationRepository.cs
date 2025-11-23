using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for BadgeConfiguration entity
/// </summary>
public class BadgeConfigurationRepository : Repository<BadgeConfiguration>, IBadgeConfigurationRepository
{
    public BadgeConfigurationRepository(DbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<BadgeConfiguration>> GetByAxisAsync(string axis)
    {
        return await _dbSet.Where(bc => bc.Axis == axis).ToListAsync();
    }
}

