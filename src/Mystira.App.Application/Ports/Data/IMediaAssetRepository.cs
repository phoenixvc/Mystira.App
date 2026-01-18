using Ardalis.Specification;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for MediaAsset entity with domain-specific queries
/// </summary>
public interface IMediaAssetRepository : IRepository<MediaAsset, string>, IRepositoryBase<MediaAsset>
{
    Task<MediaAsset?> GetByMediaIdAsync(string mediaId);
    Task<bool> ExistsByMediaIdAsync(string mediaId);
    Task<IEnumerable<string>> GetMediaIdsAsync(IEnumerable<string> mediaIds);
    IQueryable<MediaAsset> GetQueryable();
}

