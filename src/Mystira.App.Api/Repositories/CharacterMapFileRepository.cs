using Microsoft.EntityFrameworkCore;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Repositories;

/// <summary>
/// Repository implementation for CharacterMapFile singleton entity
/// </summary>
public class CharacterMapFileRepository : ICharacterMapFileRepository
{
    private readonly MystiraAppDbContext _context;
    private readonly DbSet<CharacterMapFile> _dbSet;

    public CharacterMapFileRepository(MystiraAppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<CharacterMapFile>();
    }

    public async Task<CharacterMapFile?> GetAsync()
    {
        return await _dbSet.FirstOrDefaultAsync();
    }

    public async Task<CharacterMapFile> AddOrUpdateAsync(CharacterMapFile entity)
    {
        var existing = await GetAsync();
        if (existing != null)
        {
            existing.Characters = entity.Characters;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = entity.Version;
            _dbSet.Update(existing);
            return existing;
        }

        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task DeleteAsync()
    {
        var existing = await GetAsync();
        if (existing != null)
        {
            _dbSet.Remove(existing);
        }
    }
}

