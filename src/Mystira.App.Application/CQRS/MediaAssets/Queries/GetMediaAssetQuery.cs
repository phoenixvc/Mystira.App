using Mystira.App.Application.Interfaces;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

/// <summary>
/// Query to retrieve media asset metadata by ID.
/// Cached for 5 minutes as media metadata is relatively static.
/// </summary>
public record GetMediaAssetQuery(string MediaId) : IQuery<MediaAsset?>, ICacheableQuery
{
    public string CacheKey => $"MediaAsset:{MediaId}";
    public int CacheDurationSeconds => 300; // 5 minutes
};
