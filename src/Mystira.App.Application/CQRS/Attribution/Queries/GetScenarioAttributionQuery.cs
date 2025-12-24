using Mystira.App.Application.Interfaces;
using Mystira.Contracts.App.Responses.Attribution;

namespace Mystira.App.Application.CQRS.Attribution.Queries;

/// <summary>
/// Query to retrieve attribution/creator credits for a scenario.
/// Cached for 10 minutes as attribution data changes infrequently.
/// </summary>
public record GetScenarioAttributionQuery(string ScenarioId) : IQuery<ContentAttributionResponse?>, ICacheableQuery
{
    public string CacheKey => $"ScenarioAttribution:{ScenarioId}";
    public int CacheDurationSeconds => 600; // 10 minutes
}
