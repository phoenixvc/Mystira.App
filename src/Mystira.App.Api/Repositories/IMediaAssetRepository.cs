using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Api.Models;

namespace Mystira.App.Api.Repositories;

/// <summary>
/// Repository interface for MediaAsset entity with domain-specific queries
/// </summary>
public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetByIdAsync(string id);
    Task<MediaAsset?> GetByMediaIdAsync(string mediaId);
    Task<IEnumerable<MediaAsset>> GetAllAsync();
    Task<IEnumerable<MediaAsset>> FindAsync(Expression<Func<MediaAsset, bool>> predicate);
    IQueryable<MediaAsset> GetQueryable();
    Task<MediaAsset> AddAsync(MediaAsset entity);
    Task UpdateAsync(MediaAsset entity);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<bool> ExistsByMediaIdAsync(string mediaId);
    Task<IEnumerable<string>> GetMediaIdsAsync(IEnumerable<string> mediaIds);
    Task<int> CountAsync(Expression<Func<MediaAsset, bool>>? predicate = null);
}

