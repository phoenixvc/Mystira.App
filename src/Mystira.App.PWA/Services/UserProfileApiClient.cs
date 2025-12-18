using System.Net.Http.Json;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for user profile-related operations
/// </summary>
public class UserProfileApiClient : BaseApiClient, IUserProfileApiClient
{
    public UserProfileApiClient(HttpClient httpClient, ILogger<UserProfileApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<UserProfile?> GetProfileAsync(string id)
    {
        return await SendGetAsync<UserProfile>(
            $"api/userprofiles/{id}",
            $"profile {id}",
            requireAuth: false,
            onSuccess: result => Logger.LogInformation("Successfully fetched profile {Id}", id));
    }

    public async Task<UserProfile?> GetProfileByIdAsync(string id)
    {
        return await GetProfileAsync(id);
    }

    public async Task<List<UserProfile>?> GetProfilesByAccountAsync(string accountId)
    {
        try
        {
            Logger.LogInformation("Fetching profiles for account {AccountId} from API...", accountId);

            var response = await HttpClient.GetAsync($"api/userprofiles/account/{accountId}");

            if (response.IsSuccessStatusCode)
            {
                var profiles = await response.Content.ReadFromJsonAsync<List<UserProfile>>(JsonOptions);
                Logger.LogInformation("Successfully fetched {Count} profiles for account {AccountId}", profiles?.Count ?? 0, accountId);
                return profiles;
            }
            else
            {
                Logger.LogWarning("Failed to fetch profiles with status: {StatusCode} for account: {AccountId}", response.StatusCode, accountId);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching profiles for account {AccountId} from API.", accountId);
            return null;
        }
    }

    public async Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request)
    {
        try
        {
            Logger.LogInformation("Creating profile {Name} via API...", request.Name);

            var response = await HttpClient.PostAsJsonAsync("api/userprofiles", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfile>(JsonOptions);
                Logger.LogInformation("Successfully created profile {Name} with ID {Id}", request.Name, profile?.Id);
                return profile;
            }
            else
            {
                Logger.LogWarning("Failed to create profile with status: {StatusCode} for name: {Name}", response.StatusCode, request.Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating profile {Name} via API.", request.Name);
            return null;
        }
    }

    public async Task<List<UserProfile>?> CreateMultipleProfilesAsync(CreateMultipleProfilesRequest request)
    {
        try
        {
            Logger.LogInformation("Creating {Count} profiles via API...", request.Profiles.Count);

            var response = await HttpClient.PostAsJsonAsync("api/userprofiles/batch", request, JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var profiles = await response.Content.ReadFromJsonAsync<List<UserProfile>>(JsonOptions);
                Logger.LogInformation("Successfully created {Count} profiles", profiles?.Count ?? 0);
                return profiles;
            }
            else
            {
                Logger.LogWarning("Failed to create multiple profiles with status: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating multiple profiles via API.");
            return null;
        }
    }

    public async Task<UserProfile?> UpdateProfileAsync(string id, UpdateUserProfileRequest request)
    {
        return await SendPutAsync<UpdateUserProfileRequest, UserProfile>(
            $"api/userprofiles/{id}",
            request,
            $"profile {id}",
            requireAuth: false,
            onSuccess: result => Logger.LogInformation("Successfully updated profile {Id}", id));
    }

    public async Task<bool> DeleteProfileAsync(string id)
    {
        return await SendDeleteAsync(
            $"api/userprofiles/{id}",
            $"profile {id}",
            requireAuth: false);
    }
}

