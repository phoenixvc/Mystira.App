using System.Net.Http.Json;
using Mystira.App.Contracts.Responses.Badges;

namespace Mystira.App.PWA.Services;

public class BadgesApiClient : BaseApiClient, IBadgesApiClient
{
    public BadgesApiClient(HttpClient httpClient, ILogger<BadgesApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public Task<List<BadgeResponse>?> GetBadgesByAgeGroupAsync(string ageGroup)
    {
        if (string.IsNullOrWhiteSpace(ageGroup))
        {
            return Task.FromResult<List<BadgeResponse>?>(new List<BadgeResponse>());
        }

        var encoded = Uri.EscapeDataString(ageGroup);
        return SendGetAsync<List<BadgeResponse>>($"api/badges?ageGroup={encoded}", "badges");
    }

    public Task<BadgeResponse?> GetBadgeByIdAsync(string badgeId)
    {
        if (string.IsNullOrWhiteSpace(badgeId))
        {
            return Task.FromResult<BadgeResponse?>(null);
        }

        var encoded = Uri.EscapeDataString(badgeId);
        return SendGetAsync<BadgeResponse>($"api/badges/{encoded}", "badge");
    }

    public Task<BadgeProgressResponse?> GetProfileBadgeProgressAsync(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return Task.FromResult<BadgeProgressResponse?>(null);
        }

        var encoded = Uri.EscapeDataString(profileId);
        return SendGetAsync<BadgeProgressResponse>($"api/badges/profile/{encoded}", "badge progress");
    }

    public async Task<List<AxisAchievementResponse>?> GetAxisAchievementsAsync(string ageGroupId)
    {
        if (string.IsNullOrWhiteSpace(ageGroupId))
        {
            return new List<AxisAchievementResponse>();
        }

        try
        {
            var encoded = Uri.EscapeDataString(ageGroupId);
            var response = await HttpClient.GetAsync($"api/admin/badges/axis-achievements?ageGroupId={encoded}");

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("Failed to fetch axis achievements for ageGroup {AgeGroup} with status {Status}", ageGroupId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<List<AxisAchievementResponse>>(JsonOptions);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error fetching axis achievements for ageGroup {AgeGroup}", ageGroupId);
            return null;
        }
    }

    public string GetBadgeImageResourceEndpointUrl(string imageId)
    {
        if (string.IsNullOrWhiteSpace(imageId))
        {
            return string.Empty;
        }

        var encoded = Uri.EscapeDataString(imageId);
        return $"{GetApiBaseAddressPublic()}api/badges/images/{encoded}";
    }
}
