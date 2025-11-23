using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for MediaAsset entity with domain-specific queries
/// </summary>
public interface IMediaAssetRepository : IRepository<MediaAsset>
{
    Task<MediaAsset?> GetByMediaIdAsync(string mediaId);
    Task<bool> ExistsByMediaIdAsync(string mediaId);
    Task<IEnumerable<string>> GetMediaIdsAsync(IEnumerable<string> mediaIds);
    IQueryable<MediaAsset> GetQueryable();
}

