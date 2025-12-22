using Ardalis.Specification;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Specifications;

/// <summary>
/// Specification to get a user profile by ID.
/// </summary>
public sealed class UserProfileByIdSpec : SingleEntitySpecification<UserProfile>
{
    public UserProfileByIdSpec(string id)
    {
        Query.Where(p => p.Id == id);
    }
}

/// <summary>
/// Specification to filter profiles by account ID.
/// Migrated from ProfilesByAccountSpecification.
/// </summary>
public sealed class ProfilesByAccountSpec : BaseEntitySpecification<UserProfile>
{
    public ProfilesByAccountSpec(string accountId)
    {
        Query.Where(p => p.AccountId == accountId)
             .OrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter guest profiles.
/// Migrated from GuestProfilesSpecification.
/// </summary>
public sealed class GuestProfilesSpec : BaseEntitySpecification<UserProfile>
{
    public GuestProfilesSpec()
    {
        Query.Where(p => p.IsGuest)
             .OrderByDescending(p => p.CreatedAt);
    }
}

/// <summary>
/// Specification to filter non-guest profiles.
/// Migrated from NonGuestProfilesSpecification.
/// </summary>
public sealed class NonGuestProfilesSpec : BaseEntitySpecification<UserProfile>
{
    public NonGuestProfilesSpec()
    {
        Query.Where(p => !p.IsGuest)
             .OrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter NPC profiles.
/// Migrated from NpcProfilesSpecification.
/// </summary>
public sealed class NpcProfilesSpec : BaseEntitySpecification<UserProfile>
{
    public NpcProfilesSpec()
    {
        Query.Where(p => p.IsNpc)
             .OrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter profiles that have completed onboarding.
/// Migrated from OnboardedProfilesSpecification.
/// </summary>
public sealed class OnboardedProfilesSpec : BaseEntitySpecification<UserProfile>
{
    public OnboardedProfilesSpec()
    {
        Query.Where(p => p.HasCompletedOnboarding)
             .OrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to filter profiles by age group.
/// Migrated from ProfilesByAgeGroupSpecification.
/// </summary>
public sealed class ProfilesByAgeGroupSpec : BaseEntitySpecification<UserProfile>
{
    public ProfilesByAgeGroupSpec(string ageGroup)
    {
        Query.Where(p => p.AgeGroupName == ageGroup)
             .OrderBy(p => p.Name);
    }
}

/// <summary>
/// Specification to get profiles with pagination.
/// </summary>
public sealed class ProfilesPaginatedSpec : BaseEntitySpecification<UserProfile>
{
    public ProfilesPaginatedSpec(int skip, int take, string? accountId = null, bool? isGuest = null)
    {
        var query = Query.AsTracking();

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(p => p.AccountId == accountId);
        }

        if (isGuest.HasValue)
        {
            query = query.Where(p => p.IsGuest == isGuest.Value);
        }

        query.OrderBy(p => p.Name)
             .Skip(skip)
             .Take(take);
    }
}

/// <summary>
/// Specification to search profiles by name pattern.
/// </summary>
public sealed class ProfilesByNamePatternSpec : BaseEntitySpecification<UserProfile>
{
    public ProfilesByNamePatternSpec(string namePattern)
    {
        Query.Where(p => p.Name.ToLower().Contains(namePattern.ToLower()))
             .OrderBy(p => p.Name);
    }
}
