using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for CharacterMap entity
/// </summary>
public class CharacterMapRepository : Repository<CharacterMap>, ICharacterMapRepository
{
    public CharacterMapRepository(DbContext context) : base(context)
    {
    }

    public async Task<CharacterMap?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _dbSet.AnyAsync(c => c.Name == name);
    }
}

