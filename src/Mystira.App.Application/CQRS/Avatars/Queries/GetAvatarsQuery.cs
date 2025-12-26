using Mystira.App.Application.Interfaces;
using Mystira.Contracts.App.Responses.Media;

namespace Mystira.App.Application.CQRS.Avatars.Queries;

/// <summary>
/// Query to retrieve all avatar configurations grouped by age group.
/// </summary>
public record GetAvatarsQuery : IQuery<AvatarResponse>, ICacheableQuery
{
    public string CacheKey => "AllAvatars";
    public int CacheDurationSeconds => 600; // 10 minutes
}
