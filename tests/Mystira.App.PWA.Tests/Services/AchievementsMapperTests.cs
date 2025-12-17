using FluentAssertions;
using Mystira.App.Contracts.Responses.Badges;
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
            AxisProgresses =
            {
                new AxisProgressResponse
                {
                    AxisId = "Courage",
                    AxisName = "Courage",
                    CurrentScore = 12,
                    Tiers =
                    {
                        new BadgeTierProgressResponse
                        {
                            BadgeId = "badge-bronze",
                            Tier = "Bronze",
                            TierOrder = 1,
                            Title = "Brave Beginner",
                            Description = "You chose the brave path.",
                            RequiredScore = 10,
                            ImageId = "courage-bronze",
                            IsEarned = true,
                            EarnedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            ProgressToThreshold = 12,
                            RemainingScore = 0
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
                AgeGroupId = "6-9",
                CompassAxisId = "Courage",
                CompassAxisName = "Courage",
                AxesDirection = "positive",
                Description = "Try bold choices to help others."
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
            AxisProgresses =
            {
                new AxisProgressResponse
                {
                    AxisId = "Wisdom",
                    AxisName = "Wisdom",
                    CurrentScore = 0,
                    Tiers =
                    {
                        new BadgeTierProgressResponse
                        {
                            BadgeId = "badge-bronze",
                            Tier = "Bronze",
                            TierOrder = 1,
                            Title = "Wise Beginner",
                            Description = "You learned something.",
                            RequiredScore = 5,
                            ImageId = "wisdom-bronze",
                            IsEarned = false,
                            ProgressToThreshold = 3,
                            RemainingScore = 2
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
}
