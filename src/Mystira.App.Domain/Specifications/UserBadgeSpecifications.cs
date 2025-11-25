using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

public class UserBadgesByProfileSpecification : BaseSpecification<UserBadge>
{
    public UserBadgesByProfileSpecification(string userProfileId)
        : base(b => b.UserProfileId == userProfileId)
    {
        ApplyOrderByDescending(b => b.EarnedAt);
    }
}

public class UserBadgesByAxisSpecification : BaseSpecification<UserBadge>
{
    public UserBadgesByAxisSpecification(string userProfileId, string axis)
        : base(b => b.UserProfileId == userProfileId && b.Axis == axis)
    {
        ApplyOrderByDescending(b => b.EarnedAt);
    }
}
