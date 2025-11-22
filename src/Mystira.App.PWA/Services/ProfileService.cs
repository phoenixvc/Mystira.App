using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IProfileService
{
    Task<List<UserProfile>?> GetUserProfilesAsync(string accountId);
    Task<bool> HasProfilesAsync(string accountId);
    Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request);
    Task<UserProfile?> GetProfileAsync(string profileId);
    Task<bool> DeleteProfileAsync(string profileId);
    Task<UserProfile?> UpdateProfileAsync(string profileId, UpdateUserProfileRequest request);
}

public class ProfileService : IProfileService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(IApiClient apiClient, ILogger<ProfileService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<UserProfile>?> GetUserProfilesAsync(string accountId)
    {
        try
        {
            var profiles = await _apiClient.GetProfilesByAccountAsync(accountId);
            return profiles ?? new List<UserProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for account {AccountId}", accountId);
            return null;
        }
    }

    public async Task<bool> HasProfilesAsync(string accountId)
    {
        try
        {
            var profiles = await GetUserProfilesAsync(accountId);
            return profiles != null && profiles.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if account {AccountId} has profiles", accountId);
            return false;
        }
    }

    public async Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request)
    {
        try
        {
            return await _apiClient.CreateProfileAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return null;
        }
    }

    public async Task<UserProfile?> GetProfileAsync(string profileId)
    {
        try
        {
            return await _apiClient.GetProfileAsync(profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {ProfileId}", profileId);
            return null;
        }
    }

    public async Task<bool> DeleteProfileAsync(string profileId)
    {
        try
        {
            return await _apiClient.DeleteProfileAsync(profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {ProfileId}", profileId);
            return false;
        }
    }

    public async Task<UserProfile?> UpdateProfileAsync(string profileId, UpdateUserProfileRequest request)
    {
        try
        {
            return await _apiClient.UpdateProfileAsync(profileId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", profileId);
            return null;
        }
    }
}
