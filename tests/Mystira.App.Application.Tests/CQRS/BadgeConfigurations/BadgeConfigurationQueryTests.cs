using FluentAssertions;
using Mystira.App.Application.CQRS.BadgeConfigurations.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.CQRS.BadgeConfigurations;

public class BadgeConfigurationQueryTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var badges = new List<BadgeConfiguration>
        {
            new()
            {
                Id = "badge-1",
                Name = "Brave Heart",
                Message = "Showed courage in the face of danger",
                Axis = "Courage",
                Threshold = 75,
                ImageId = "img-1"
            },
            new()
            {
                Id = "badge-2",
                Name = "Wise Mind",
                Message = "Made a wise decision",
                Axis = "Wisdom",
                Threshold = 80,
                ImageId = "img-2"
            },
            new()
            {
                Id = "badge-3",
                Name = "Kind Soul",
                Message = "Showed compassion to others",
                Axis = "Compassion",
                Threshold = 70,
                ImageId = "img-3"
            }
        };

        DbContext.BadgeConfigurations.AddRange(badges);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllBadgeConfigurationsQuery_ReturnsAllBadges()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllBadgeConfigurationsQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(b => b.Name == "Brave Heart");
        result.Should().Contain(b => b.Name == "Wise Mind");
        result.Should().Contain(b => b.Name == "Kind Soul");
    }

    [Fact]
    public async Task GetAllBadgeConfigurationsQuery_UsesCaching()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetAllBadgeConfigurationsQuery();

        // Act - First call (cache miss, hits database)
        var result1 = await Mediator.Send(query);

        // Add a new badge to database (should not appear in cached result)
        DbContext.BadgeConfigurations.Add(new BadgeConfiguration
        {
            Id = "badge-4",
            Name = "New Badge",
            Message = "This should not appear in cached result",
            Axis = "Test",
            Threshold = 50,
            ImageId = "img-4"
        });
        await DbContext.SaveChangesAsync();

        // Act - Second call (cache hit, should return same 3 badges)
        var result2 = await Mediator.Send(query);

        // Assert
        result1.Should().HaveCount(3);
        result2.Should().HaveCount(3); // Still 3 due to cache
        result2.Should().NotContain(b => b.Name == "New Badge");

        // Clear cache and query again (should now show 4 badges)
        ClearCache();
        var result3 = await Mediator.Send(query);
        result3.Should().HaveCount(4);
        result3.Should().Contain(b => b.Name == "New Badge");
    }

    [Fact]
    public async Task GetBadgeConfigurationQuery_ReturnsSingleBadge()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetBadgeConfigurationQuery("badge-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("badge-1");
        result.Name.Should().Be("Brave Heart");
        result.Axis.Should().Be("Courage");
        result.Threshold.Should().Be(75);
    }

    [Fact]
    public async Task GetBadgeConfigurationQuery_WhenNotFound_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetBadgeConfigurationQuery("non-existent-id");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBadgeConfigurationQuery_UsesCaching()
    {
        // Arrange
        await SeedTestDataAsync();
        var badgeId = "badge-1";
        var query = new GetBadgeConfigurationQuery(badgeId);

        // Act - First call (cache miss)
        var result1 = await Mediator.Send(query);

        // Modify the badge in database
        var badge = await DbContext.BadgeConfigurations.FindAsync(badgeId);
        badge!.Name = "Modified Name";
        await DbContext.SaveChangesAsync();

        // Act - Second call (cache hit, should return original name)
        var result2 = await Mediator.Send(query);

        // Assert
        result1!.Name.Should().Be("Brave Heart");
        result2!.Name.Should().Be("Brave Heart"); // Still cached

        // Clear cache and query again (should now show modified name)
        ClearCache();
        var result3 = await Mediator.Send(query);
        result3!.Name.Should().Be("Modified Name");
    }

    [Fact]
    public async Task GetBadgeConfigurationsByAxisQuery_ReturnsFilteredBadges()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetBadgeConfigurationsByAxisQuery("Courage");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Brave Heart");
        result.First().Axis.Should().Be("Courage");
    }

    [Fact]
    public async Task GetBadgeConfigurationsByAxisQuery_WhenNoMatch_ReturnsEmpty()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetBadgeConfigurationsByAxisQuery("NonExistentAxis");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleConcurrentQueries_UseSeparateCacheKeys()
    {
        // Arrange
        await SeedTestDataAsync();
        var query1 = new GetBadgeConfigurationQuery("badge-1");
        var query2 = new GetBadgeConfigurationQuery("badge-2");

        // Act - Query both badges
        var result1 = await Mediator.Send(query1);
        var result2 = await Mediator.Send(query2);

        // Verify both are cached by modifying database
        var badge1 = await DbContext.BadgeConfigurations.FindAsync("badge-1");
        var badge2 = await DbContext.BadgeConfigurations.FindAsync("badge-2");
        badge1!.Name = "Modified 1";
        badge2!.Name = "Modified 2";
        await DbContext.SaveChangesAsync();

        // Act - Query again (should get cached versions)
        var result1Cached = await Mediator.Send(query1);
        var result2Cached = await Mediator.Send(query2);

        // Assert - Both should still have original names (cached)
        result1Cached!.Name.Should().Be("Brave Heart");
        result2Cached!.Name.Should().Be("Wise Mind");
    }
}
