using FluentAssertions;
using Mystira.App.PWA.Models;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

public class AchievementsFilteringTests
{
    [Fact]
    public void Apply_WhenShowOnlyEarned_RemovesInProgressTiersAndEmptyAxes()
    {
        var model = new AchievementsViewModel
        {
            ProfileId = "p1",
            ProfileName = "Player",
            AgeGroupId = "6-9",
            Axes =
            {
                new AxisAchievementsSectionViewModel
                {
                    AxisId = "Courage",
                    AxisName = "Courage",
                    CurrentScore = 10,
                    Tiers =
                    {
                        new BadgeTierViewModel { BadgeId = "b1", Tier = "Bronze", TierOrder = 1, Title = "Bronze", RequiredScore = 5, IsEarned = true, CurrentScore = 10 },
                        new BadgeTierViewModel { BadgeId = "b2", Tier = "Silver", TierOrder = 2, Title = "Silver", RequiredScore = 15, IsEarned = false, CurrentScore = 10 }
                    }
                },
                new AxisAchievementsSectionViewModel
                {
                    AxisId = "Wisdom",
                    AxisName = "Wisdom",
                    CurrentScore = 2,
                    Tiers =
                    {
                        new BadgeTierViewModel { BadgeId = "b3", Tier = "Bronze", TierOrder = 1, Title = "Bronze", RequiredScore = 5, IsEarned = false, CurrentScore = 2 }
                    }
                }
            }
        };

        var options = new AchievementsFilterOptions { ShowEarned = true, ShowInProgress = false };

        var result = AchievementsFiltering.Apply(model, options);

        result.Should().HaveCount(1);
        result[0].AxisId.Should().Be("Courage");
        result[0].Tiers.Should().ContainSingle(t => t.IsEarned);
    }

    [Fact]
    public void Apply_WhenShowOnlyInProgress_RemovesEarnedTiersAndEmptyAxes()
    {
        var model = new AchievementsViewModel
        {
            ProfileId = "p1",
            ProfileName = "Player",
            AgeGroupId = "6-9",
            Axes =
            {
                new AxisAchievementsSectionViewModel
                {
                    AxisId = "Courage",
                    AxisName = "Courage",
                    CurrentScore = 10,
                    Tiers =
                    {
                        new BadgeTierViewModel { BadgeId = "b1", Tier = "Bronze", TierOrder = 1, Title = "Bronze", RequiredScore = 5, IsEarned = true, CurrentScore = 10 }
                    }
                },
                new AxisAchievementsSectionViewModel
                {
                    AxisId = "Wisdom",
                    AxisName = "Wisdom",
                    CurrentScore = 2,
                    Tiers =
                    {
                        new BadgeTierViewModel { BadgeId = "b3", Tier = "Bronze", TierOrder = 1, Title = "Bronze", RequiredScore = 5, IsEarned = false, CurrentScore = 2 }
                    }
                }
            }
        };

        var options = new AchievementsFilterOptions { ShowEarned = false, ShowInProgress = true };

        var result = AchievementsFiltering.Apply(model, options);

        result.Should().HaveCount(1);
        result[0].AxisId.Should().Be("Wisdom");
        result[0].Tiers.Should().ContainSingle(t => !t.IsEarned);
    }
}
