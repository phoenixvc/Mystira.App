using Mystira.App.Application.Interfaces;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve a single scenario by ID (read operation).
/// Cached for 5 minutes as scenarios are relatively static.
/// </summary>
public record GetScenarioQuery(string ScenarioId) : IQuery<Scenario?>, ICacheableQuery
{
    public string CacheKey => $"Scenario:{ScenarioId}";
    public int CacheDurationSeconds => 300; // 5 minutes
};
