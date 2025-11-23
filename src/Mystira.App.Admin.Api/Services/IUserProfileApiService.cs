using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Domain.Models;
using ContractsCreateGuestProfileRequest = Mystira.App.Contracts.Requests.UserProfiles.CreateGuestProfileRequest;
using ContractsCreateMultipleProfilesRequest = Mystira.App.Contracts.Requests.UserProfiles.CreateMultipleProfilesRequest;
using ContractsCreateUserProfileRequest = Mystira.App.Contracts.Requests.UserProfiles.CreateUserProfileRequest;
using ContractsUpdateUserProfileRequest = Mystira.App.Contracts.Requests.UserProfiles.UpdateUserProfileRequest;

namespace Mystira.App.Admin.Api.Services;

public interface IUserProfileApiService
{
    Task<UserProfile> CreateProfileAsync(ContractsCreateUserProfileRequest request);
    Task<UserProfile> CreateGuestProfileAsync(ContractsCreateGuestProfileRequest request);
    Task<List<UserProfile>> CreateMultipleProfilesAsync(ContractsCreateMultipleProfilesRequest request);
    Task<UserProfile?> GetProfileAsync(string name);
    Task<UserProfile?> GetProfileByIdAsync(string id);
    Task<UserProfile?> UpdateProfileAsync(string name, ContractsUpdateUserProfileRequest request);
    Task<UserProfile?> UpdateProfileByIdAsync(string id, ContractsUpdateUserProfileRequest request);
    Task<bool> DeleteProfileAsync(string name);
    Task<bool> CompleteOnboardingAsync(string name);
    Task<List<UserProfile>> GetAllProfilesAsync();
    Task<List<UserProfile>> GetNonGuestProfilesAsync();
    Task<List<UserProfile>> GetGuestProfilesAsync();
    Task<bool> AssignCharacterToProfileAsync(string profileId, string characterId, bool isNpc = false);
}
