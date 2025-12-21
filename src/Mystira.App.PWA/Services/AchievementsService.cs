using System.Collections.Concurrent;
using Mystira.App.Contracts.Responses.Badges;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class AchievementsService : IAchievementsService
{
    private readonly IBadgesApiClient _badgesApiClient;
    private readonly ILogger<AchievementsService> _logger;
    private readonly ConcurrentDictionary<string, Task<List<BadgeResponse>?>> _badgeConfigTasks = new();
    private readonly ConcurrentDictionary<string, Task<List<AxisAchievementResponse>?>> _axisAchievementTasks = new();

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

            // Use Task-based caching to ensure multiple concurrent requests for the same age group
            // only trigger a single API call.
            var badgeConfiguration = await _badgeConfigTasks.GetOrAdd(ageGroupId,
                id => _badgesApiClient.GetBadgesByAgeGroupAsync(id)) ?? new List<BadgeResponse>();

            var axisAchievements = await _axisAchievementTasks.GetOrAdd(ageGroupId,
                id => _badgesApiClient.GetAxisAchievementsAsync(id)) ?? new List<AxisAchievementResponse>();

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
