using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.BadgeConfigurations.Queries;

/// <summary>
/// Query to retrieve a badge configuration by ID.
/// Cached for 10 minutes as badge configurations rarely change.
/// </summary>
public record GetBadgeConfigurationQuery(string BadgeId) : IQuery<BadgeConfiguration?>, ICacheableQuery
{
    public string CacheKey => $"BadgeConfiguration:{BadgeId}";
    public int CacheDurationSeconds => 600; // 10 minutes
};
