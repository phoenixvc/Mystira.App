using Mystira.Contracts.App.Responses.Badges;
using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public static class AchievementsMapper
{
    public static List<AxisAchievementsSectionViewModel> MapAxes(
        IReadOnlyList<BadgeResponse>? badgeConfiguration,
        BadgeProgressResponse? progress,
        IReadOnlyList<AxisAchievementResponse>? axisAchievements,
        Func<string, string?> imageUrlResolver)
    {
        var config = badgeConfiguration ?? Array.Empty<BadgeResponse>();
        var axisProgresses = progress?.AxisProgresses ?? new List<AxisProgressResponse>();

        var configByAxis = config
            .GroupBy(b => b.CompassAxisId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(b => b.TierOrder).ToList(), StringComparer.OrdinalIgnoreCase);

        var progressByAxis = axisProgresses
            .GroupBy(a => a.AxisId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var axisAchievementList = axisAchievements ?? Array.Empty<AxisAchievementResponse>();

        var axisIds = configByAxis.Keys
            .Concat(progressByAxis.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<AxisAchievementsSectionViewModel>();

        foreach (var axisId in axisIds)
        {
            progressByAxis.TryGetValue(axisId, out var axisProgress);
            var axisName = axisProgress?.AxisName ?? axisId;

            var currentScore = axisProgress?.CurrentScore ?? 0f;
            if (currentScore <= 0f && axisProgress?.Tiers?.Count > 0)
            {
                currentScore = axisProgress.Tiers.Max(t => t.ProgressToThreshold);
            }

            var axisCopy = axisAchievementList
                .Where(a => string.Equals(a.CompassAxisId, axisId, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(a.CompassAxisId, axisName, StringComparison.OrdinalIgnoreCase))
                .Select(a => new AxisAchievementCopy
                {
                    Direction = a.AxesDirection,
                    Description = a.Description
                })
                .OrderBy(a => a.Direction, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tiers = new List<BadgeTierViewModel>();

            var configTiers = configByAxis.TryGetValue(axisId, out var tierConfig)
                ? tierConfig
                : new List<BadgeResponse>();

            var progressTiers = axisProgress?.Tiers ?? new List<BadgeTierProgressResponse>();
            var progressTierByBadgeId = progressTiers
                .Where(t => !string.IsNullOrWhiteSpace(t.BadgeId))
                .ToDictionary(t => t.BadgeId, t => t, StringComparer.OrdinalIgnoreCase);

            foreach (var badge in configTiers)
            {
                progressTierByBadgeId.TryGetValue(badge.Id, out var progressTier);

                var tierName = !string.IsNullOrWhiteSpace(badge.Tier) ? badge.Tier : (progressTier?.Tier ?? string.Empty);
                var tierOrder = badge.TierOrder != 0 ? badge.TierOrder : (progressTier?.TierOrder ?? 0);
                var required = badge.RequiredScore > 0 ? badge.RequiredScore : (progressTier?.RequiredScore ?? 0);
                var imageId = !string.IsNullOrWhiteSpace(badge.ImageId) ? badge.ImageId : (progressTier?.ImageId ?? string.Empty);

                tiers.Add(new BadgeTierViewModel
                {
                    BadgeId = badge.Id,
                    Tier = tierName,
                    TierOrder = tierOrder,
                    Title = !string.IsNullOrWhiteSpace(badge.Title) ? badge.Title : (progressTier?.Title ?? string.Empty),
                    Description = !string.IsNullOrWhiteSpace(badge.Description) ? badge.Description : (progressTier?.Description ?? string.Empty),
                    RequiredScore = required,
                    ImageId = imageId,
                    ImageUrl = !string.IsNullOrWhiteSpace(imageId) ? imageUrlResolver(imageId) : null,
                    IsEarned = progressTier?.IsEarned ?? false,
                    EarnedAt = progressTier?.EarnedAt,
                    CurrentScore = progressTier?.ProgressToThreshold ?? currentScore
                });
            }

            if (configTiers.Count == 0 && progressTiers.Count > 0)
            {
                tiers.AddRange(progressTiers.Select(t => new BadgeTierViewModel
                {
                    BadgeId = t.BadgeId,
                    Tier = t.Tier,
                    TierOrder = t.TierOrder,
                    Title = t.Title,
                    Description = t.Description,
                    RequiredScore = t.RequiredScore,
                    ImageId = t.ImageId,
                    ImageUrl = !string.IsNullOrWhiteSpace(t.ImageId) ? imageUrlResolver(t.ImageId) : null,
                    IsEarned = t.IsEarned,
                    EarnedAt = t.EarnedAt,
                    CurrentScore = t.ProgressToThreshold != 0 ? t.ProgressToThreshold : currentScore
                }));
            }

            results.Add(new AxisAchievementsSectionViewModel
            {
                AxisId = axisId,
                AxisName = axisName,
                CurrentScore = currentScore,
                AxisCopy = axisCopy,
                Tiers = tiers.OrderBy(t => t.TierOrder).ToList()
            });
        }

        return results;
    }
}
