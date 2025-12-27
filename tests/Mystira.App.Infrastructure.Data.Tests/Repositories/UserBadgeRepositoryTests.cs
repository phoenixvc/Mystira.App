using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;

namespace Mystira.App.Infrastructure.Data.Tests.Repositories;

public class UserBadgeRepositoryTests : IDisposable
{
    private readonly MystiraAppDbContext _context;
    private readonly UserBadgeRepository _repository;

    public UserBadgeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MystiraAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MystiraAppDbContext(options);
        _repository = new UserBadgeRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByUserProfileIdAsync_WithNoBadges_ReturnsEmptyCollection()
    {
        // Arrange
        var userProfileId = Guid.NewGuid().ToString();

        // Act
        var result = await _repository.GetByUserProfileIdAsync(userProfileId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserProfileIdAsync_WithBadges_ReturnsBadgesOrderedByEarnedAtDescending()
    {
        // Arrange
        var userProfileId = Guid.NewGuid().ToString();
        var badge1 = CreateTestBadge(userProfileId, DateTime.UtcNow.AddDays(-2));
        var badge2 = CreateTestBadge(userProfileId, DateTime.UtcNow.AddDays(-1));
        var badge3 = CreateTestBadge(userProfileId, DateTime.UtcNow);

        await _context.Set<UserBadge>().AddRangeAsync(badge1, badge2, badge3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserProfileIdAsync(userProfileId);

        // Assert
        result.Should().HaveCount(3);
        result.First().EarnedAt.Should().Be(badge3.EarnedAt);
        result.Last().EarnedAt.Should().Be(badge1.EarnedAt);
    }

    [Fact]
    public async Task GetByUserProfileIdAndBadgeConfigIdAsync_WithMatchingBadge_ReturnsBadge()
    {
        // Arrange
        var userProfileId = Guid.NewGuid().ToString();
        var badgeConfigId = Guid.NewGuid().ToString();
        var badge = CreateTestBadge(userProfileId, DateTime.UtcNow, badgeConfigId);

        await _context.Set<UserBadge>().AddAsync(badge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserProfileIdAndBadgeConfigIdAsync(userProfileId, badgeConfigId);

        // Assert
        result.Should().NotBeNull();
        result!.UserProfileId.Should().Be(userProfileId);
        result.BadgeConfigurationId.Should().Be(badgeConfigId);
    }

    [Fact]
    public async Task GetByUserProfileIdAndBadgeConfigIdAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var userProfileId = Guid.NewGuid().ToString();
        var badgeConfigId = Guid.NewGuid().ToString();

        // Act
        var result = await _repository.GetByUserProfileIdAndBadgeConfigIdAsync(userProfileId, badgeConfigId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByGameSessionIdAsync_WithMatchingBadges_ReturnsBadges()
    {
        // Arrange
        var gameSessionId = Guid.NewGuid().ToString();
        var badge1 = CreateTestBadge(Guid.NewGuid().ToString(), DateTime.UtcNow, gameSessionId: gameSessionId);
        var badge2 = CreateTestBadge(Guid.NewGuid().ToString(), DateTime.UtcNow.AddMinutes(-5), gameSessionId: gameSessionId);

        await _context.Set<UserBadge>().AddRangeAsync(badge1, badge2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByGameSessionIdAsync(gameSessionId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByScenarioIdAsync_WithMatchingBadges_ReturnsBadges()
    {
        // Arrange
        var scenarioId = Guid.NewGuid().ToString();
        var badge1 = CreateTestBadge(Guid.NewGuid().ToString(), DateTime.UtcNow, scenarioId: scenarioId);
        var badge2 = CreateTestBadge(Guid.NewGuid().ToString(), DateTime.UtcNow.AddMinutes(-5), scenarioId: scenarioId);

        await _context.Set<UserBadge>().AddRangeAsync(badge1, badge2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByScenarioIdAsync(scenarioId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserProfileIdAndAxisAsync_WithMatchingBadges_ReturnsBadges()
    {
        // Arrange
        var userProfileId = Guid.NewGuid().ToString();
        var axis = "courage";
        var badge1 = CreateTestBadge(userProfileId, DateTime.UtcNow, axis: axis);
        var badge2 = CreateTestBadge(userProfileId, DateTime.UtcNow.AddMinutes(-5), axis: axis);
        var otherAxisBadge = CreateTestBadge(userProfileId, DateTime.UtcNow, axis: "wisdom");

        await _context.Set<UserBadge>().AddRangeAsync(badge1, badge2, otherAxisBadge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserProfileIdAndAxisAsync(userProfileId, axis);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.Axis == axis);
    }

    [Fact]
    public async Task AddAsync_WithValidBadge_AddsBadgeToContext()
    {
        // Arrange
        var badge = CreateTestBadge(Guid.NewGuid().ToString(), DateTime.UtcNow);

        // Act
        var result = await _repository.AddAsync(badge);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        var storedBadge = await _context.Set<UserBadge>().FindAsync(badge.Id);
        storedBadge.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_WithNullBadge_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static UserBadge CreateTestBadge(
        string userProfileId,
        DateTime earnedAt,
        string? badgeConfigId = null,
        string? gameSessionId = null,
        string? scenarioId = null,
        string? axis = null)
    {
        return new UserBadge
        {
            Id = Guid.NewGuid().ToString(),
            UserProfileId = userProfileId,
            BadgeConfigurationId = badgeConfigId ?? Guid.NewGuid().ToString(),
            GameSessionId = gameSessionId ?? Guid.NewGuid().ToString(),
            ScenarioId = scenarioId ?? Guid.NewGuid().ToString(),
            Axis = axis ?? "default",
            EarnedAt = earnedAt
        };
    }
}
