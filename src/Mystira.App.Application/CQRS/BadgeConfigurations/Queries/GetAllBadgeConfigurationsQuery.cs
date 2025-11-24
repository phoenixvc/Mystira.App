using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

/// <summary>
/// Query to retrieve all badge configurations.
/// Cached for 10 minutes as badge configurations rarely change.
/// </summary>
public record GetAllBadgeConfigurationsQuery() : IQuery<List<BadgeConfiguration>>, ICacheableQuery
{
    public string CacheKey => "BadgeConfigurations:All";
    public int CacheDurationSeconds => 600; // 10 minutes
};
