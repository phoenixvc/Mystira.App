using Mystira.App.Application.Interfaces;
using Mystira.App.Contracts.Responses.Attribution;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Query to retrieve attribution/creator credits for a content bundle.
/// Cached for 10 minutes as attribution data changes infrequently.
/// </summary>
public record GetBundleAttributionQuery(string BundleId) : IQuery<ContentAttributionResponse?>, ICacheableQuery
{
    public string CacheKey => $"BundleAttribution:{BundleId}";
    public int CacheDurationSeconds => 600; // 10 minutes
}
