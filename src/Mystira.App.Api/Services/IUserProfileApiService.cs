using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// DEPRECATED: This service violates hexagonal architecture.
/// Controllers should use IMediator (CQRS pattern) instead.
/// </summary>
/// <remarks>
/// Migration guide:
/// - CreateProfileAsync → CreateUserProfileCommand
/// - CreateGuestProfileAsync → (Create CreateGuestProfileCommand if needed)
/// - CreateMultipleProfilesAsync → CreateMultipleProfilesCommand
/// - GetProfileByIdAsync → GetUserProfileQuery
/// - UpdateProfileByIdAsync → UpdateUserProfileCommand
/// - DeleteProfileAsync → DeleteUserProfileCommand
/// - CompleteOnboardingAsync → CompleteOnboardingCommand
/// - GetAllProfilesAsync → (Create GetAllProfilesQuery if needed)
/// - GetNonGuestProfilesAsync → (Create GetNonGuestProfilesQuery if needed)
/// - GetGuestProfilesAsync → (Create GetGuestProfilesQuery if needed)
/// - AssignCharacterToProfileAsync → AssignCharacterToProfileCommand
/// See ARCHITECTURAL_REFACTORING_PLAN.md for details.
/// </remarks>
[Obsolete("Use IMediator with CQRS queries/commands instead. See ARCHITECTURAL_REFACTORING_PLAN.md")]
public interface IUserProfileApiService
{
    Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request);
    Task<UserProfile> CreateGuestProfileAsync(CreateGuestProfileRequest request);
    Task<List<UserProfile>> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request);
    Task<UserProfile?> GetProfileByIdAsync(string id);
    Task<UserProfile?> UpdateProfileByIdAsync(string id, UpdateUserProfileRequest request);
    Task<bool> DeleteProfileAsync(string id);
    Task<bool> CompleteOnboardingAsync(string id);
    Task<List<UserProfile>> GetAllProfilesAsync();
    Task<List<UserProfile>> GetNonGuestProfilesAsync();
    Task<List<UserProfile>> GetGuestProfilesAsync();
    Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false);
}
