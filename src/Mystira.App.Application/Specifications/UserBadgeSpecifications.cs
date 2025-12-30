using Ardalis.Specification;
using Mystira.Domain.Models;

namespace Mystira.App.Application.Specifications;

/// <summary>
/// Specification to filter user badges by profile ID.
/// Migrated from UserBadgesByProfileSpecification.
/// </summary>
public sealed class UserBadgesByProfileSpec : BaseEntitySpecification<UserBadge>
{
    public UserBadgesByProfileSpec(string userProfileId)
    {
        Query.Where(b => b.UserProfileId == userProfileId)
             .OrderByDescending(b => b.EarnedAt);
    }
}

/// <summary>
/// Specification to filter user badges by profile and axis.
/// Migrated from UserBadgesByAxisSpecification.
/// </summary>
public sealed class UserBadgesByAxisSpec : BaseEntitySpecification<UserBadge>
{
    public UserBadgesByAxisSpec(string userProfileId, string axis)
    {
        Query.Where(b => b.UserProfileId == userProfileId && b.Axis == axis)
             .OrderByDescending(b => b.EarnedAt);
    }
}

/// <summary>
/// Specification to get a user badge by ID.
/// </summary>
public sealed class UserBadgeByIdSpec : SingleEntitySpecification<UserBadge>
{
    public UserBadgeByIdSpec(string id)
    {
        Query.Where(b => b.Id == id);
    }
}
