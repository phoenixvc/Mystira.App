using System.Net.Http.Json;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for avatar-related operations
/// </summary>
public class AvatarApiClient : BaseApiClient, IAvatarApiClient
{
    public AvatarApiClient(HttpClient httpClient, ILogger<AvatarApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<Dictionary<string, List<string>>?> GetAvatarsAsync()
    {
        try
        {
            Logger.LogInformation("Fetching avatars from API...");

            var response = await HttpClient.GetAsync("api/avatars");

            if (response.IsSuccessStatusCode)
            {
                var avatarResponse = await response.Content.ReadFromJsonAsync<AvatarResponse>(JsonOptions);
                Logger.LogInformation("Successfully fetched avatars");
                return avatarResponse?.AgeGroupAvatars;
            }
            else
            {
                Logger.LogWarning("API request failed with status: {StatusCode}. Unable to fetch avatars.", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching avatars from API");
            return null;
        }
    }

    public async Task<List<string>?> GetAvatarsByAgeGroupAsync(string ageGroup)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                return null;
            }

            Logger.LogInformation("Fetching avatars for age group {AgeGroup} from API...", ageGroup);

            var response = await HttpClient.GetAsync($"api/avatars/{ageGroup}");

            if (response.IsSuccessStatusCode)
            {
                var configResponse = await response.Content.ReadFromJsonAsync<AvatarConfigurationResponse>(JsonOptions);
                Logger.LogInformation("Successfully fetched {Count} avatars for age group {AgeGroup}", configResponse?.AvatarMediaIds?.Count ?? 0, ageGroup);
                return configResponse?.AvatarMediaIds;
            }
            else
            {
                Logger.LogWarning("API request failed with status: {StatusCode}. Unable to fetch avatars for age group {AgeGroup}.", response.StatusCode, ageGroup);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching avatars for age group {AgeGroup} from API", ageGroup);
            return null;
        }
    }
}

