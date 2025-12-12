using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Application.Tests.Services;

public class BadgeAwardingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MystiraAppDbContext _dbContext;
    private readonly IBadgeAwardingService _badgeAwardingService;
    private readonly IBadgeRepository _badgeRepository;
    private readonly IUserBadgeRepository _userBadgeRepository;

    public BadgeAwardingServiceTests()
    {
        var services = new ServiceCollection();

        // Add in-memory database
        services.AddDbContext<MystiraAppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<MystiraAppDbContext>());

        // Add repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, Mystira.App.Infrastructure.Data.UnitOfWork.UnitOfWork>();

        // Add logging
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // Add the service to test
        services.AddScoped<IBadgeAwardingService, BadgeAwardingService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<MystiraAppDbContext>();
        _badgeAwardingService = _serviceProvider.GetRequiredService<IBadgeAwardingService>();
        _badgeRepository = _serviceProvider.GetRequiredService<IBadgeRepository>();
        _userBadgeRepository = _serviceProvider.GetRequiredService<IUserBadgeRepository>();
    }

    [Fact]
    public async Task AwardBadgesAsync_WithQualifyingScore_AwardsBadge()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile1",
            Name = "Test Player",
            AgeGroupName = "6-9"
        };

        var badge = new Badge
        {
            Id = "badge1",
            AgeGroupId = "6-9",
            CompassAxisId = "honesty",
            Tier = "bronze",
            TierOrder = 1,
            Title = "Honest Spark",
            Description = "First step on honesty",
            RequiredScore = 10f,
            ImageId = "image1"
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.Badges.AddAsync(badge);
        await _dbContext.SaveChangesAsync();

        var axisScores = new Dictionary<string, float> { { "honesty", 15f } };

        // Act
        var result = await _badgeAwardingService.AwardBadgesAsync(profile, axisScores);

        // Assert
        Assert.Single(result);
        Assert.Equal("badge1", result[0].BadgeId);
        Assert.Equal("Honest Spark", result[0].BadgeName);
        Assert.Equal("honesty", result[0].Axis);
        Assert.Equal(15f, result[0].TriggerValue);
    }

    [Fact]
    public async Task AwardBadgesAsync_WithBelowThresholdScore_NoAward()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile1",
            Name = "Test Player",
            AgeGroupName = "6-9"
        };

        var badge = new Badge
        {
            Id = "badge1",
            AgeGroupId = "6-9",
            CompassAxisId = "honesty",
            Tier = "bronze",
            TierOrder = 1,
            Title = "Honest Spark",
            Description = "First step on honesty",
            RequiredScore = 10f,
            ImageId = "image1"
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.Badges.AddAsync(badge);
        await _dbContext.SaveChangesAsync();

        var axisScores = new Dictionary<string, float> { { "honesty", 5f } };

        // Act
        var result = await _badgeAwardingService.AwardBadgesAsync(profile, axisScores);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AwardBadgesAsync_WithMultipleTiers_AwardsHighestQualified()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile1",
            Name = "Test Player",
            AgeGroupName = "6-9"
        };

        var badges = new List<Badge>
        {
            new()
            {
                Id = "badge1",
                AgeGroupId = "6-9",
                CompassAxisId = "honesty",
                Tier = "bronze",
                TierOrder = 1,
                Title = "Honest Spark",
                RequiredScore = 10f,
                ImageId = "image1"
            },
            new()
            {
                Id = "badge2",
                AgeGroupId = "6-9",
                CompassAxisId = "honesty",
                Tier = "silver",
                TierOrder = 2,
                Title = "Honest Light",
                RequiredScore = 25f,
                ImageId = "image2"
            },
            new()
            {
                Id = "badge3",
                AgeGroupId = "6-9",
                CompassAxisId = "honesty",
                Tier = "gold",
                TierOrder = 3,
                Title = "Honest Star",
                RequiredScore = 50f,
                ImageId = "image3"
            }
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        foreach (var badge in badges)
        {
            await _dbContext.Badges.AddAsync(badge);
        }
        await _dbContext.SaveChangesAsync();

        var axisScores = new Dictionary<string, float> { { "honesty", 35f } };

        // Act
        var result = await _badgeAwardingService.AwardBadgesAsync(profile, axisScores);

        // Assert
        Assert.Equal(2, result.Count); // Both bronze and silver should be awarded
        Assert.Contains(result, b => b.BadgeId == "badge1");
        Assert.Contains(result, b => b.BadgeId == "badge2");
        Assert.DoesNotContain(result, b => b.BadgeId == "badge3"); // Gold not reached
    }

    [Fact]
    public async Task AwardBadgesAsync_SkipsAlreadyEarnedBadges()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile1",
            Name = "Test Player",
            AgeGroupName = "6-9"
        };

        var badge = new Badge
        {
            Id = "badge1",
            AgeGroupId = "6-9",
            CompassAxisId = "honesty",
            Tier = "bronze",
            TierOrder = 1,
            Title = "Honest Spark",
            RequiredScore = 10f,
            ImageId = "image1"
        };

        // Create already earned badge
        var earnedBadge = new UserBadge
        {
            UserProfileId = "profile1",
            BadgeId = "badge1",
            BadgeName = "Honest Spark",
            BadgeMessage = "Already earned",
            Axis = "honesty",
            TriggerValue = 15f,
            Threshold = 10f,
            ImageId = "image1"
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.Badges.AddAsync(badge);
        await _dbContext.UserBadges.AddAsync(earnedBadge);
        await _dbContext.SaveChangesAsync();

        var axisScores = new Dictionary<string, float> { { "honesty", 20f } };

        // Act
        var result = await _badgeAwardingService.AwardBadgesAsync(profile, axisScores);

        // Assert - should not re-award already earned badge
        Assert.Empty(result);
    }

    [Fact]
    public async Task AwardBadgesAsync_WithMultipleAxes_AwardsBadgesPerAxis()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = "profile1",
            Name = "Test Player",
            AgeGroupName = "6-9"
        };

        var badges = new List<Badge>
        {
            new()
            {
                Id = "badge1",
                AgeGroupId = "6-9",
                CompassAxisId = "honesty",
                Tier = "bronze",
                TierOrder = 1,
                Title = "Honest Spark",
                RequiredScore = 10f,
                ImageId = "image1"
            },
            new()
            {
                Id = "badge2",
                AgeGroupId = "6-9",
                CompassAxisId = "bravery",
                Tier = "bronze",
                TierOrder = 1,
                Title = "Brave Heart",
                RequiredScore = 10f,
                ImageId = "image2"
            }
        };

        await _dbContext.UserProfiles.AddAsync(profile);
        foreach (var badge in badges)
        {
            await _dbContext.Badges.AddAsync(badge);
        }
        await _dbContext.SaveChangesAsync();

        var axisScores = new Dictionary<string, float>
        {
            { "honesty", 15f },
            { "bravery", 12f }
        };

        // Act
        var result = await _badgeAwardingService.AwardBadgesAsync(profile, axisScores);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.Axis == "honesty");
        Assert.Contains(result, b => b.Axis == "bravery");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
