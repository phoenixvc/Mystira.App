using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification to filter profiles by account ID
/// </summary>
public class ProfilesByAccountSpecification : BaseSpecification<UserProfile>
{
    public ProfilesByAccountSpecification(string accountId)
        : base(p => p.AccountId == accountId)
    {
        ApplyOrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter guest profiles
/// </summary>
public class GuestProfilesSpecification : BaseSpecification<UserProfile>
{
    public GuestProfilesSpecification()
        : base(p => p.IsGuest)
    {
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}

/// <summary>
/// Specification to filter non-guest profiles
/// </summary>
public class NonGuestProfilesSpecification : BaseSpecification<UserProfile>
{
    public NonGuestProfilesSpecification()
        : base(p => !p.IsGuest)
    {
        ApplyOrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter NPC profiles
/// </summary>
public class NpcProfilesSpecification : BaseSpecification<UserProfile>
{
    public NpcProfilesSpecification()
        : base(p => p.IsNpc)
    {
        ApplyOrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter profiles that have completed onboarding
/// </summary>
public class OnboardedProfilesSpecification : BaseSpecification<UserProfile>
{
    public OnboardedProfilesSpecification()
        : base(p => p.HasCompletedOnboarding)
    {
        ApplyOrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter profiles by age group
/// </summary>
public class ProfilesByAgeGroupSpecification : BaseSpecification<UserProfile>
{
    public ProfilesByAgeGroupSpecification(string ageGroup)
        : base(p => p.AgeGroupName == ageGroup)
    {
        ApplyOrderBy(p => p.Name);
    }
}
