using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public class AchievementsFilterOptions
{
    public bool ShowEarned { get; set; } = true;
    public bool ShowInProgress { get; set; } = false;
}

public static class AchievementsFiltering
{
    public static IReadOnlyList<AxisAchievementsSectionViewModel> Apply(AchievementsViewModel model, AchievementsFilterOptions options)
    {
        var results = new List<AxisAchievementsSectionViewModel>();

        foreach (var axis in model.Axes)
        {
            var tiers = axis.Tiers
                .Where(t => (t.IsEarned && options.ShowEarned) || (!t.IsEarned && options.ShowInProgress))
                .OrderBy(t => t.TierOrder)
                .ToList();

            if (tiers.Count == 0)
            {
                continue;
            }

            results.Add(new AxisAchievementsSectionViewModel
            {
                AxisId = axis.AxisId,
                AxisName = axis.AxisName,
                CurrentScore = axis.CurrentScore,
                AxisCopy = axis.AxisCopy.ToList(),
                Tiers = tiers
            });
        }

        return results;
    }
}
