using FluentAssertions;
using Mystira.Contracts.App.Responses.Badges;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

public class AchievementsMapperTests
{
    [Fact]
    public void MapAxes_MergesConfigurationWithProgress_AndResolvesImageUrls()
    {
        var config = new List<BadgeResponse>
        {
            new()
            {
                Id = "badge-bronze",
                Name = "Brave Beginner Badge",
                AgeGroupId = "6-9",
                CompassAxisId = "Courage",
                Tier = "Bronze",
                TierOrder = 1,
                Title = "Brave Beginner",
                Description = "You chose the brave path.",
                RequiredScore = 10,
                ImageId = "courage-bronze"
            },
            new()
            {
                Id = "badge-silver",
                Name = "Brave Explorer Badge",
                AgeGroupId = "6-9",
                CompassAxisId = "Courage",
                Tier = "Silver",
                TierOrder = 2,
                Title = "Brave Explorer",
                Description = "You kept going.",
                RequiredScore = 20,
                ImageId = "courage-silver"
            }
        };

        var progress = new BadgeProgressResponse
        {
            AgeGroupId = "6-9",
            Badge = new BadgeResponse { Id = "badge-bronze", Name = "Brave Beginner Badge", Description = "You chose the brave path." },
            CurrentValue = 12,
            TargetValue = 20,
            AxisProgresses = new List<AxisProgressResponse>
            {
                new()
                {
                    CompassAxisId = "Courage",
                    CompassAxisName = "Courage",
                    AxisName = "Courage",
                    CurrentScore = 12,
                    CurrentLevel = 1,
                    Tiers = new List<BadgeTierProgressResponse>
                    {
                        new()
                        {
                            BadgeId = "badge-bronze",
                            Tier = "Bronze",
                            TierName = "Bronze",
                            TierOrder = 1,
                            Title = "Brave Beginner",
                            Description = "You chose the brave path.",
                            RequiredScore = 10,
                            ImageId = "courage-bronze",
                            IsEarned = true,
                            EarnedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ProgressToThreshold = 12,
                            RemainingScore = 0,
                            TotalBadges = 2,
                            EarnedBadges = 1
                        }
                    }
                }
            }
        };

        var axisAchievements = new List<AxisAchievementResponse>
        {
            new()
            {
                Id = "axis-copy-1",
                Name = "Courage Achievement",
                AgeGroupId = "6-9",
                CompassAxisId = "Courage",
                CompassAxisName = "Courage",
                AxisName = "Courage",
                AxesDirection = "positive",
                Description = "Try bold choices to help others.",
                Level = 1,
                ScoreThreshold = 10
            }
        };

        var result = AchievementsMapper.MapAxes(config, progress, axisAchievements, imageId => $"https://cdn.test/{imageId}.png");

        result.Should().HaveCount(1);
        var axis = result[0];
        axis.AxisId.Should().Be("Courage");
        axis.CurrentScore.Should().Be(12);
        axis.AxisCopy.Should().ContainSingle(c => c.Direction == "positive");

        axis.Tiers.Should().HaveCount(2);
        axis.Tiers[0].Tier.Should().Be("Bronze");
        axis.Tiers[0].IsEarned.Should().BeTrue();
        axis.Tiers[0].EarnedAt.Should().NotBeNull();
        axis.Tiers[0].ImageUrl.Should().Be("https://cdn.test/courage-bronze.png");

        axis.Tiers[1].Tier.Should().Be("Silver");
        axis.Tiers[1].IsEarned.Should().BeFalse();
        axis.Tiers[1].ImageUrl.Should().Be("https://cdn.test/courage-silver.png");
    }

    [Fact]
    public void MapAxes_WhenAxisScoreIsZero_FallsBackToTierProgressValues()
    {
        var config = new List<BadgeResponse>
        {
            new()
            {
                Id = "badge-bronze",
                Name = "Wise Beginner Badge",
                AgeGroupId = "6-9",
                CompassAxisId = "Wisdom",
                Tier = "Bronze",
                TierOrder = 1,
                Title = "Wise Beginner",
                Description = "You learned something.",
                RequiredScore = 5,
                ImageId = "wisdom-bronze"
            }
        };

        var progress = new BadgeProgressResponse
        {
            AgeGroupId = "6-9",
            Badge = new BadgeResponse { Id = "badge-bronze", Name = "Wise Beginner Badge", Description = "You learned something." },
            CurrentValue = 3,
            TargetValue = 5,
            AxisProgresses = new List<AxisProgressResponse>
            {
                new()
                {
                    CompassAxisId = "Wisdom",
                    CompassAxisName = "Wisdom",
                    AxisName = "Wisdom",
                    CurrentScore = 0,
                    CurrentLevel = 0,
                    Tiers = new List<BadgeTierProgressResponse>
                    {
                        new()
                        {
                            BadgeId = "badge-bronze",
                            Tier = "Bronze",
                            TierName = "Bronze",
                            TierOrder = 1,
                            Title = "Wise Beginner",
                            Description = "You learned something.",
                            RequiredScore = 5,
                            ImageId = "wisdom-bronze",
                            IsEarned = false,
                            ProgressToThreshold = 3,
                            RemainingScore = 2,
                            TotalBadges = 1,
                            EarnedBadges = 0
                        }
                    }
                }
            }
        };

        var result = AchievementsMapper.MapAxes(config, progress, axisAchievements: null, imageId => imageId);
        result.Should().ContainSingle();
        result[0].CurrentScore.Should().Be(3);
        result[0].Tiers[0].CurrentScore.Should().Be(3);
    }

    [Fact]
    public void MapAxes_PlatinumAndDiamondTiers_ShowsProgressCorrectly()
    {
        var config = new List<BadgeResponse>
        {
            new() { Id = "p", Name = "Platinum Badge", CompassAxisId = "A", Tier = "Platinum", TierOrder = 4, RequiredScore = 100, Description = "Platinum tier" },
            new() { Id = "d", Name = "Diamond Badge", CompassAxisId = "A", Tier = "Diamond", TierOrder = 5, RequiredScore = 200, Description = "Diamond tier" }
        };

        var progress = new BadgeProgressResponse
        {
            Badge = new BadgeResponse { Id = "p", Name = "Platinum Badge", Description = "Platinum tier" },
            CurrentValue = 50,
            TargetValue = 100,
            AxisProgresses = new List<AxisProgressResponse>
            {
                new()
                {
                    CompassAxisId = "A",
                    AxisName = "A",
                    CurrentScore = 50,
                    CurrentLevel = 3,
                    Tiers = new List<BadgeTierProgressResponse>
                    {
                        // Simulate specific progress for Diamond but not Platinum
                        new() { BadgeId = "p", TierName = "Platinum", TierOrder = 4, RequiredScore = 100, ProgressToThreshold = 0, TotalBadges = 2, EarnedBadges = 0 },
                        new() { BadgeId = "d", TierName = "Diamond", TierOrder = 5, RequiredScore = 200, ProgressToThreshold = 50, TotalBadges = 2, EarnedBadges = 0 }
                    }
                }
            }
        };

        var result = AchievementsMapper.MapAxes(config, progress, null, id => id);

        var tiers = result[0].Tiers;
        var platinum = tiers.First(t => t.Tier == "Platinum");
        var diamond = tiers.First(t => t.Tier == "Diamond");

        // Platinum should use its specific ProgressToThreshold (0)
        platinum.ProgressPercent.Should().Be(0);
        // Diamond should use its specific ProgressToThreshold (50)
        diamond.ProgressPercent.Should().Be(25);
    }
}
