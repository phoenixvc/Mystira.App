using FluentAssertions;
using Mystira.App.Application.CQRS.UserBadges.Commands;
using Mystira.App.Application.CQRS.UserBadges.Queries;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.UserBadges;

public class UserBadgeTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Seed BadgeConfigurations
        var badgeConfigs = new List<BadgeConfiguration>
        {
            new()
            {
                Id = "badge-config-courage-1",
                Name = "First Step of Courage",
                Message = "You showed courage!",
                Axis = "Courage",
                Threshold = 5.0f,
                Level = 1
            },
            new()
            {
                Id = "badge-config-courage-2",
                Name = "Courageous Heart",
                Message = "Your courage grows!",
                Axis = "Courage",
                Threshold = 15.0f,
                Level = 2
            },
            new()
            {
                Id = "badge-config-wisdom-1",
                Name = "Seeker of Knowledge",
                Message = "You seek wisdom!",
                Axis = "Wisdom",
                Threshold = 5.0f,
                Level = 1
            }
        };
        DbContext.BadgeConfigurations.AddRange(badgeConfigs);

        // Seed UserProfiles
        var profiles = new List<UserProfile>
        {
            new()
            {
                Id = "profile-1",
                Name = "TestPlayer",
                AgeGroupName = "6-9",
                EarnedBadges = new List<UserBadge>
                {
                    new()
                    {
                        Id = "badge-1",
                        UserProfileId = "profile-1",
                        BadgeConfigurationId = "badge-config-courage-1",
                        BadgeName = "First Step of Courage",
                        BadgeMessage = "You showed courage!",
                        Axis = "Courage",
                        TriggerValue = 6.0f,
                        AwardedAt = DateTime.UtcNow.AddDays(-1)
                    }
                }
            },
            new()
            {
                Id = "profile-2",
                Name = "NewPlayer",
                AgeGroupName = "6-9",
                EarnedBadges = new List<UserBadge>()
            }
        };
        DbContext.UserProfiles.AddRange(profiles);

        await DbContext.SaveChangesAsync();
    }

    #region GetUserBadgesQuery Tests

    [Fact]
    public async Task GetUserBadgesQuery_WithExistingBadges_ReturnsBadges()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesQuery("profile-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].BadgeName.Should().Be("First Step of Courage");
        result[0].Axis.Should().Be("Courage");
    }

    [Fact]
    public async Task GetUserBadgesQuery_WithNoBadges_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesQuery("profile-2");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserBadgesQuery_WithNonExistentProfile_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserBadgesForAxisQuery Tests

    [Fact]
    public async Task GetUserBadgesForAxisQuery_WithMatchingAxis_ReturnsBadges()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesForAxisQuery("profile-1", "Courage");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().OnlyContain(b => b.Axis == "Courage");
    }

    [Fact]
    public async Task GetUserBadgesForAxisQuery_WithNoMatchingAxis_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesForAxisQuery("profile-1", "Wisdom");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region HasUserEarnedBadgeQuery Tests

    [Fact]
    public async Task HasUserEarnedBadgeQuery_WithEarnedBadge_ReturnsTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new HasUserEarnedBadgeQuery("profile-1", "badge-config-courage-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasUserEarnedBadgeQuery_WithUnearnedBadge_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new HasUserEarnedBadgeQuery("profile-1", "badge-config-wisdom-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasUserEarnedBadgeQuery_WithNonExistentProfile_ReturnsFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new HasUserEarnedBadgeQuery("non-existent", "badge-config-courage-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AwardBadgeCommand Tests

    [Fact]
    public async Task AwardBadgeCommand_WithValidRequest_AwardsBadge()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-2",
            BadgeConfigurationId = "badge-config-wisdom-1",
            TriggerValue = 7.5f,
            GameSessionId = "session-123",
            ScenarioId = "scenario-456"
        };
        var command = new AwardBadgeCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.UserProfileId.Should().Be("profile-2");
        result.BadgeConfigurationId.Should().Be("badge-config-wisdom-1");
        result.BadgeName.Should().Be("Seeker of Knowledge");
        result.Axis.Should().Be("Wisdom");
        result.TriggerValue.Should().Be(7.5f);
        result.GameSessionId.Should().Be("session-123");
        result.ScenarioId.Should().Be("scenario-456");
        result.AwardedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AwardBadgeCommand_WithNonExistentProfile_ThrowsException()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "non-existent",
            BadgeConfigurationId = "badge-config-wisdom-1",
            TriggerValue = 7.5f
        };
        var command = new AwardBadgeCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task AwardBadgeCommand_WithNonExistentBadgeConfig_ThrowsException()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-2",
            BadgeConfigurationId = "non-existent-badge",
            TriggerValue = 7.5f
        };
        var command = new AwardBadgeCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    #endregion

    #region GetBadgeStatisticsQuery Tests

    [Fact]
    public async Task GetBadgeStatisticsQuery_WithBadges_ReturnsStatistics()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetBadgeStatisticsQuery("profile-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalBadgesEarned.Should().Be(1);
        result.BadgesByAxis.Should().ContainKey("Courage");
    }

    [Fact]
    public async Task GetBadgeStatisticsQuery_WithNoBadges_ReturnsZeroStats()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetBadgeStatisticsQuery("profile-2");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalBadgesEarned.Should().Be(0);
    }

    #endregion
}
