using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for MediaAsset entity
/// </summary>
public class MediaAssetRepository : Repository<MediaAsset>, IMediaAssetRepository
{
    public MediaAssetRepository(DbContext context) : base(context)
    {
    }

    public async Task<MediaAsset?> GetByMediaIdAsync(string mediaId)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.MediaId == mediaId);
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
}

