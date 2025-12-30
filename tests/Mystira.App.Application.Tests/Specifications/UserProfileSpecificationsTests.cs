using FluentAssertions;
using Mystira.App.Application.Specifications;
using Mystira.Domain.Models;

namespace Mystira.App.Application.Tests.Specifications;

/// <summary>
/// Unit tests for UserProfile specifications.
/// </summary>
public class UserProfileSpecificationsTests
{
    private readonly List<UserProfile> _profiles;

    public UserProfileSpecificationsTests()
    {
        _profiles = new List<UserProfile>
        {
            CreateProfile("1", "account1", "Player One", isGuest: false, isNpc: false, hasOnboarded: true, ageGroup: "teen"),
            CreateProfile("2", "account1", "Player Two", isGuest: false, isNpc: false, hasOnboarded: true, ageGroup: "adult"),
            CreateProfile("3", "account2", "Guest Player", isGuest: true, isNpc: false, hasOnboarded: false, ageGroup: "teen"),
            CreateProfile("4", "account2", "NPC Character", isGuest: false, isNpc: true, hasOnboarded: false, ageGroup: "adult"),
            CreateProfile("5", "account3", "New Player", isGuest: false, isNpc: false, hasOnboarded: false, ageGroup: "child"),
        };
    }

    [Fact]
    public void ProfilesByAccountSpec_ShouldFilterByAccountId()
    {
        // Arrange
        var spec = new ProfilesByAccountSpec("account1");

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.AccountId == "account1");
    }

    [Fact]
    public void GuestProfilesSpec_ShouldFilterGuestsOnly()
    {
        // Arrange
        var spec = new GuestProfilesSpec();

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().IsGuest.Should().BeTrue();
    }

    [Fact]
    public void NonGuestProfilesSpec_ShouldExcludeGuests()
    {
        // Arrange
        var spec = new NonGuestProfilesSpec();

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(4);
        result.Should().OnlyContain(p => !p.IsGuest);
    }

    [Fact]
    public void NpcProfilesSpec_ShouldFilterNpcsOnly()
    {
        // Arrange
        var spec = new NpcProfilesSpec();

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().IsNpc.Should().BeTrue();
    }

    [Fact]
    public void OnboardedProfilesSpec_ShouldFilterOnboardedOnly()
    {
        // Arrange
        var spec = new OnboardedProfilesSpec();

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.HasCompletedOnboarding);
    }

    [Fact]
    public void ProfilesByAgeGroupSpec_ShouldFilterByAgeGroup()
    {
        // Arrange
        var spec = new ProfilesByAgeGroupSpec("teen");

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.AgeGroupName == "teen");
    }

    [Fact]
    public void UserProfileByIdSpec_ShouldMatchById()
    {
        // Arrange
        var spec = new UserProfileByIdSpec("3");

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("3");
    }

    [Fact]
    public void ProfilesByNamePatternSpec_ShouldMatchPattern()
    {
        // Arrange
        var spec = new ProfilesByNamePatternSpec("player");

        // Act
        var result = _profiles.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(4);
    }

    private static UserProfile CreateProfile(
        string id,
        string accountId,
        string name,
        bool isGuest,
        bool isNpc,
        bool hasOnboarded,
        string ageGroup)
    {
        return new UserProfile
        {
            Id = id,
            AccountId = accountId,
            Name = name,
            IsGuest = isGuest,
            IsNpc = isNpc,
            HasCompletedOnboarding = hasOnboarded,
            AgeGroupName = ageGroup,
            CreatedAt = DateTime.UtcNow
        };
    }
}
