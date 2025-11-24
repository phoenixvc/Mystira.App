using FluentAssertions;
using Mystira.App.Application.CQRS.UserBadges.Commands;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.UserBadges;

public class UserBadgeCommandTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Seed badge configurations
        var badgeConfigs = new List<BadgeConfiguration>
        {
            new()
            {
                Id = "badge-config-1",
                Name = "Brave Heart",
                Message = "Showed courage",
                Axis = "Courage",
                Threshold = 75,
                ImageId = "img-1"
            },
            new()
            {
                Id = "badge-config-2",
                Name = "Wise Mind",
                Message = "Made wise decision",
                Axis = "Wisdom",
                Threshold = 80,
                ImageId = "img-2"
            }
        };

        DbContext.BadgeConfigurations.AddRange(badgeConfigs);

        // Seed user profiles
        var profiles = new List<UserProfile>
        {
            new()
            {
                Id = "profile-1",
                AccountId = "account-1",
                Name = "Test User",
                AgeGroup = "10-12",
                IsGuest = false
            }
        };

        DbContext.UserProfiles.AddRange(profiles);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AwardBadgeCommand_CreatesNewBadge()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-1",
            BadgeConfigurationId = "badge-config-1",
            Axis = "Courage",
            TriggerValue = 80
        };
        var command = new AwardBadgeCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.UserProfileId.Should().Be("profile-1");
        result.BadgeConfigurationId.Should().Be("badge-config-1");
        result.Axis.Should().Be("Courage");
        result.TriggerValue.Should().Be(80);
        result.EarnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify badge is in database
        var savedBadge = await DbContext.UserBadges.FindAsync(result.Id);
        savedBadge.Should().NotBeNull();
        savedBadge!.UserProfileId.Should().Be("profile-1");
    }

    [Fact]
    public async Task AwardBadgeCommand_WithMissingUserProfileId_ThrowsException()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "", // Missing
            BadgeConfigurationId = "badge-config-1",
            Axis = "Courage"
        };
        var command = new AwardBadgeCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task AwardBadgeCommand_WithMissingBadgeConfigId_ThrowsException()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-1",
            BadgeConfigurationId = "", // Missing
            Axis = "Courage"
        };
        var command = new AwardBadgeCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task AwardBadgeCommand_CreatesUniqueIds()
    {
        // Arrange
        await SeedTestDataAsync();
        var request1 = new AwardBadgeRequest
        {
            UserProfileId = "profile-1",
            BadgeConfigurationId = "badge-config-1",
            Axis = "Courage"
        };
        var request2 = new AwardBadgeRequest
        {
            UserProfileId = "profile-1",
            BadgeConfigurationId = "badge-config-2",
            Axis = "Wisdom"
        };

        // Act
        var result1 = await Mediator.Send(new AwardBadgeCommand(request1));
        var result2 = await Mediator.Send(new AwardBadgeCommand(request2));

        // Assert
        result1.Id.Should().NotBe(result2.Id);
        result1.BadgeConfigurationId.Should().Be("badge-config-1");
        result2.BadgeConfigurationId.Should().Be("badge-config-2");

        // Verify both badges exist in database
        var allBadges = DbContext.UserBadges.ToList();
        allBadges.Should().HaveCount(2);
    }

    [Fact]
    public async Task AwardBadgeCommand_WithOptionalFields_SavesCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-1",
            BadgeConfigurationId = "badge-config-1",
            Axis = "Courage",
            TriggerValue = 85,
            GameSessionId = "session-123",
            ScenarioId = "scenario-456"
        };
        var command = new AwardBadgeCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.TriggerValue.Should().Be(85);
        result.GameSessionId.Should().Be("session-123");
        result.ScenarioId.Should().Be("scenario-456");
    }

    [Fact]
    public async Task AwardBadgeCommand_PersistsToDatabase()
    {
        // Arrange
        await SeedTestDataAsync();
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-1",
            BadgeConfigurationId = "badge-config-1",
            Axis = "Courage"
        };
        var command = new AwardBadgeCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Clear the DbContext to force a fresh query
        DbContext.ChangeTracker.Clear();

        // Query the badge from database
        var savedBadge = await DbContext.UserBadges.FindAsync(result.Id);

        // Assert
        savedBadge.Should().NotBeNull();
        savedBadge!.Id.Should().Be(result.Id);
        savedBadge.UserProfileId.Should().Be("profile-1");
        savedBadge.BadgeConfigurationId.Should().Be("badge-config-1");
        savedBadge.Axis.Should().Be("Courage");
    }
}
