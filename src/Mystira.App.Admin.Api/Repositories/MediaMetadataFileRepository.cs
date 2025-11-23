using Microsoft.EntityFrameworkCore;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Repositories;

/// <summary>
/// Repository implementation for MediaMetadataFile singleton entity
/// </summary>
public class MediaMetadataFileRepository : IMediaMetadataFileRepository
{
    private readonly MystiraAppDbContext _context;
    private readonly DbSet<MediaMetadataFile> _dbSet;

    public MediaMetadataFileRepository(MystiraAppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<MediaMetadataFile>();
    }

    public async Task<MediaMetadataFile?> GetAsync()
    {
        return await _dbSet.FirstOrDefaultAsync();
    }

    public async Task<MediaMetadataFile> AddOrUpdateAsync(MediaMetadataFile entity)
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

