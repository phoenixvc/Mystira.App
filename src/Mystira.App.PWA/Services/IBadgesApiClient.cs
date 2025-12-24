using Mystira.Contracts.App.Responses.Badges;

namespace Mystira.App.PWA.Services;

public interface IBadgesApiClient
{
    Task<List<BadgeResponse>?> GetBadgesByAgeGroupAsync(string ageGroup);
    Task<BadgeResponse?> GetBadgeByIdAsync(string badgeId);
    Task<BadgeProgressResponse?> GetProfileBadgeProgressAsync(string profileId);

    Task<List<AxisAchievementResponse>?> GetAxisAchievementsAsync(string ageGroupId);

    string GetBadgeImageResourceEndpointUrl(string imageId);
}
