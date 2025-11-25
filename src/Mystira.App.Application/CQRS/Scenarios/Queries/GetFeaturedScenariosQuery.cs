using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve featured scenarios for the home page.
/// Featured scenarios are those marked with IsFeatured = true.
/// Cached for 10 minutes as featured content changes infrequently.
/// </summary>
public record GetFeaturedScenariosQuery : IQuery<List<Scenario>>, ICacheableQuery
{
    public string CacheKey => "FeaturedScenarios";
    public int CacheDurationSeconds => 600; // 10 minutes
}
