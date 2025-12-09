using Mystira.App.Application.Interfaces;
using Mystira.App.Contracts.Responses.Media;

namespace Mystira.App.Application.CQRS.Avatars.Queries;

/// <summary>
/// Query to retrieve avatars for a specific age group.
/// </summary>
public record GetAvatarsByAgeGroupQuery(string AgeGroup)
    : IQuery<AvatarConfigurationResponse?>, ICacheableQuery
{
    public string CacheKey => $"Avatars:AgeGroup:{AgeGroup}";
    public int CacheDurationSeconds => 600; // 10 minutes
}
