using System.Collections.Concurrent;
using Mystira.App.Contracts.Responses.Badges;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class AchievementsService : IAchievementsService
{
    private readonly IBadgesApiClient _badgesApiClient;
    private readonly ILogger<AchievementsService> _logger;
    private readonly ConcurrentDictionary<string, List<BadgeResponse>> _badgeConfigCache = new();
    private readonly ConcurrentDictionary<string, List<AxisAchievementResponse>> _axisAchievementCache = new();

    public AchievementsService(IBadgesApiClient badgesApiClient, ILogger<AchievementsService> logger)
    {
        _badgesApiClient = badgesApiClient;
        _logger = logger;
    }

    public async Task<AchievementsLoadResult> GetAchievementsAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            return AchievementsLoadResult.Fail("No profile selected.");
        }

        try
        {
            var progress = await _badgesApiClient.GetProfileBadgeProgressAsync(profile.Id);
            if (progress == null)
            {
                return AchievementsLoadResult.Fail("Unable to load achievements right now. Please try again.");
            }

            var ageGroupId = !string.IsNullOrWhiteSpace(progress.AgeGroupId)
                ? progress.AgeGroupId
                : (!string.IsNullOrWhiteSpace(profile.AgeGroup) ? profile.AgeGroup : "6-9");

            // Cache badge configuration and axis achievements by ageGroupId to avoid redundant calls
            // when multiple profiles share the same age group (e.g. on the Achievements landing page).
            if (!_badgeConfigCache.TryGetValue(ageGroupId, out var badgeConfiguration))
            {
                badgeConfiguration = await _badgesApiClient.GetBadgesByAgeGroupAsync(ageGroupId) ?? new List<BadgeResponse>();
                _badgeConfigCache[ageGroupId] = badgeConfiguration;
            }

            if (!_axisAchievementCache.TryGetValue(ageGroupId, out var axisAchievements))
            {
                axisAchievements = await _badgesApiClient.GetAxisAchievementsAsync(ageGroupId) ?? new List<AxisAchievementResponse>();
                _axisAchievementCache[ageGroupId] = axisAchievements;
            }

            var axes = AchievementsMapper.MapAxes(
                badgeConfiguration,
                progress,
                axisAchievements,
                imageId => _badgesApiClient.GetBadgeImageResourceEndpointUrl(imageId));

            var model = new AchievementsViewModel
            {
                ProfileId = profile.Id,
                ProfileName = profile.Name,
                AgeGroupId = ageGroupId,
                Axes = axes
            };

            return AchievementsLoadResult.Success(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading achievements for profile {ProfileId}", profile.Id);
            return AchievementsLoadResult.Fail("Unable to load achievements right now. Please try again.");
        }
    }
}
