using Mystira.App.Application.Interfaces;
using Mystira.Contracts.App.Responses.Attribution;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Query to retrieve IP registration status for a content bundle.
/// Cached for 10 minutes as IP status changes infrequently.
/// </summary>
public record GetBundleIpStatusQuery(string BundleId) : IQuery<IpVerificationResponse?>, ICacheableQuery
{
    public string CacheKey => $"BundleIpStatus:{BundleId}";
    public int CacheDurationSeconds => 600; // 10 minutes
}
