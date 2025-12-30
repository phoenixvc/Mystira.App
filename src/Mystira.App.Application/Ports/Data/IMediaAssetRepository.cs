using Mystira.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

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

