using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IUserProfileApiClient
{
    Task<UserProfile?> GetProfileAsync(string id);
    Task<UserProfile?> GetProfileByIdAsync(string id);
    Task<List<UserProfile>?> GetProfilesByAccountAsync(string accountId);
    Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request);
    Task<List<UserProfile>?> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request);
    Task<UserProfile?> UpdateProfileAsync(string id, UpdateUserProfileRequest request);
    Task<bool> DeleteProfileAsync(string id);
}

