using FluentAssertions;
using Mystira.App.Application.CQRS.UserBadges.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.CQRS.UserBadges;

public class UserBadgeQueryTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var badges = new List<UserBadge>
        {
            new()
            {
                Id = "badge-1",
                UserProfileId = "profile-1",
                BadgeConfigurationId = "config-1",
                Axis = "Courage",
                EarnedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = "badge-2",
                UserProfileId = "profile-1",
                BadgeConfigurationId = "config-2",
                Axis = "Wisdom",
                EarnedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = "badge-3",
                UserProfileId = "profile-1",
                BadgeConfigurationId = "config-3",
                Axis = "Courage",
                EarnedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = "badge-4",
                UserProfileId = "profile-2",
                BadgeConfigurationId = "config-4",
                Axis = "Compassion",
                EarnedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        DbContext.UserBadges.AddRange(badges);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserBadgesQuery_ReturnsAllBadgesForUser()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesQuery("profile-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().OnlyContain(b => b.UserProfileId == "profile-1");
        result.Should().Contain(b => b.Id == "badge-1");
        result.Should().Contain(b => b.Id == "badge-2");
        result.Should().Contain(b => b.Id == "badge-3");
    }

    [Fact]
    public async Task GetUserBadgesQuery_OrdersByEarnedAtDescending()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesQuery("profile-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Most recent first
        result[0].Id.Should().Be("badge-3"); // -1 day
        result[1].Id.Should().Be("badge-2"); // -3 days
        result[2].Id.Should().Be("badge-1"); // -5 days
    }

    [Fact]
    public async Task GetUserBadgesQuery_WhenNoMatches_ReturnsEmpty()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesQuery("non-existent-profile");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserBadgesQuery_DoesNotUseCaching()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesQuery("profile-1");

        // Act - First call
        var result1 = await Mediator.Send(query);

        // Add a new badge for the same user
        DbContext.UserBadges.Add(new UserBadge
        {
            Id = "badge-new",
            UserProfileId = "profile-1",
            BadgeConfigurationId = "config-new",
            Axis = "Test",
            EarnedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        // Act - Second call (should reflect new badge immediately - no caching)
        var result2 = await Mediator.Send(query);

        // Assert
        result1.Should().HaveCount(3);
        result2.Should().HaveCount(4); // Should include new badge (not cached)
        result2.Should().Contain(b => b.Id == "badge-new");
    }

    [Fact]
    public async Task GetUserBadgesQuery_IsolatesUserData()
    {
        // Arrange
        await SeedTestDataAsync();
        var queryProfile1 = new GetUserBadgesQuery("profile-1");
        var queryProfile2 = new GetUserBadgesQuery("profile-2");

        // Act
        var resultProfile1 = await Mediator.Send(queryProfile1);
        var resultProfile2 = await Mediator.Send(queryProfile2);

        // Assert
        resultProfile1.Should().HaveCount(3);
        resultProfile1.Should().OnlyContain(b => b.UserProfileId == "profile-1");

        resultProfile2.Should().HaveCount(1);
        resultProfile2.Should().OnlyContain(b => b.UserProfileId == "profile-2");
    }

    [Fact]
    public async Task GetUserBadgesByAxisQuery_FiltersCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesByAxisQuery("profile-1", "Courage");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.UserProfileId == "profile-1" && b.Axis == "Courage");
        result.Should().Contain(b => b.Id == "badge-1");
        result.Should().Contain(b => b.Id == "badge-3");
    }

    [Fact]
    public async Task GetUserBadgesByAxisQuery_OrdersByEarnedAtDescending()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesByAxisQuery("profile-1", "Courage");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("badge-3"); // More recent
        result[1].Id.Should().Be("badge-1"); // Older
    }

    [Fact]
    public async Task GetUserBadgesByAxisQuery_WhenNoMatches_ReturnsEmpty()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesByAxisQuery("profile-1", "NonExistentAxis");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserBadgesByAxisQuery_DoesNotCrossPollinate()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetUserBadgesByAxisQuery("profile-1", "Compassion");

        // Act
        var result = await Mediator.Send(query);

        // Assert - Profile 1 has no Compassion badges (profile 2 does)
        result.Should().BeEmpty();
    }
}
