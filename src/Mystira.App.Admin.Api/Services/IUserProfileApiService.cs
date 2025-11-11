using Mystira.App.Domain.Models;
using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IUserProfileApiService
{
    Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request);
    Task<UserProfile> CreateGuestProfileAsync(CreateGuestProfileRequest request);
    Task<List<UserProfile>> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request);
    Task<UserProfile?> GetProfileAsync(string name);
    Task<UserProfile?> GetProfileByIdAsync(string id);
    Task<UserProfile?> UpdateProfileAsync(string name, UpdateUserProfileRequest request);
    Task<bool> DeleteProfileAsync(string name);
    Task<bool> CompleteOnboardingAsync(string name);
    Task<List<UserProfile>> GetAllProfilesAsync();
    Task<List<UserProfile>> GetNonGuestProfilesAsync();
    Task<List<UserProfile>> GetGuestProfilesAsync();
    Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false);
}