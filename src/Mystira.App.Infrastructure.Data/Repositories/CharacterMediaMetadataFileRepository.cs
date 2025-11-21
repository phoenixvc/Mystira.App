using Microsoft.EntityFrameworkCore;
using Mystira.App.Api.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for CharacterMediaMetadataFile singleton entity
/// </summary>
public class CharacterMediaMetadataFileRepository : ICharacterMediaMetadataFileRepository
{
    private readonly DbContext _context;
    private readonly DbSet<CharacterMediaMetadataFile> _dbSet;

    public CharacterMediaMetadataFileRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<CharacterMediaMetadataFile>();
    }

    public async Task<CharacterMediaMetadataFile?> GetAsync()
    {
        return await _dbSet.FirstOrDefaultAsync();
    }

    public async Task<CharacterMediaMetadataFile> AddOrUpdateAsync(CharacterMediaMetadataFile entity)
    {
        var existing = await GetAsync();
        if (existing != null)
        {
            existing.Entries = entity.Entries;
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

