using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Repositories;

/// <summary>
/// Repository implementation for MediaAsset entity
/// </summary>
public class MediaAssetRepository : IMediaAssetRepository
{
    private readonly MystiraAppDbContext _context;
    private readonly DbSet<MediaAsset> _dbSet;

    public MediaAssetRepository(MystiraAppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<MediaAsset>();
    }

    public async Task<MediaAsset?> GetByIdAsync(string id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<MediaAsset?> GetByMediaIdAsync(string mediaId)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.MediaId == mediaId);
    }

    public async Task<IEnumerable<MediaAsset>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<MediaAsset>> FindAsync(Expression<Func<MediaAsset, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public IQueryable<MediaAsset> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<MediaAsset> AddAsync(MediaAsset entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await _dbSet.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(MediaAsset entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await GetByIdAsync(id) != null;
    }

    public async Task<bool> ExistsByMediaIdAsync(string mediaId)
    {
        return await _dbSet.AnyAsync(m => m.MediaId == mediaId);
    }

    public async Task<IEnumerable<string>> GetMediaIdsAsync(IEnumerable<string> mediaIds)
    {
        return await _dbSet
            .Where(m => mediaIds.Contains(m.MediaId))
            .Select(m => m.MediaId)
            .ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<MediaAsset, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _dbSet.CountAsync();
        }
        return await _dbSet.CountAsync(predicate);
    }
}

