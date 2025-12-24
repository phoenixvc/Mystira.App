using Mystira.App.Application.Interfaces;
using Mystira.Contracts.App.Responses.Attribution;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Query to retrieve IP registration status for a scenario.
/// Cached for 10 minutes as IP status changes infrequently.
/// </summary>
public record GetScenarioIpStatusQuery(string ScenarioId) : IQuery<IpVerificationResponse?>, ICacheableQuery
{
    public string CacheKey => $"ScenarioIpStatus:{ScenarioId}";
    public int CacheDurationSeconds => 600; // 10 minutes
}
