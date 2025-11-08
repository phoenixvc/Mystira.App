using AutoFixture.Xunit2;
using DMfinity.Domain.Models;
using FluentAssertions;
using Xunit;

namespace DMfinity.Domain.Tests.Models;

public class UserProfileTests
{
    [Fact]
    public void UserProfile_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var userProfile = new UserProfile();

        // Assert
        userProfile.Name.Should().BeEmpty();
        userProfile.PreferredFantasyThemes.Should().NotBeNull().And.BeEmpty();
        userProfile.AgeGroup.Should().Be(AgeGroup.School); // Default value
        userProfile.HasCompletedOnboarding.Should().BeFalse();
        userProfile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [AutoData]
    public void UserProfile_SetProperties_SetsValuesCorrectly(
        string name,
        List<string> preferredThemes)
    {
        // Arrange
        var userProfile = new UserProfile();
        var testTime = DateTime.UtcNow;

        // Act
        userProfile.Name = name;
        userProfile.PreferredFantasyThemes = preferredThemes;
        userProfile.AgeGroup = AgeGroup.School;
        userProfile.HasCompletedOnboarding = true;
        userProfile.CreatedAt = testTime;

        // Assert
        userProfile.Name.Should().Be(name);
        userProfile.PreferredFantasyThemes.Should().BeEquivalentTo(preferredThemes);
        userProfile.AgeGroup.Should().Be(AgeGroup.School);
        userProfile.HasCompletedOnboarding.Should().BeTrue();
        userProfile.CreatedAt.Should().Be(testTime);
    }

    [Fact]
    public void UserProfile_AddPreferredTheme_AddsToList()
    {
        // Arrange
        var userProfile = new UserProfile();
        const string theme = "Classic Fantasy";

        // Act
        userProfile.PreferredFantasyThemes.Add(theme);

        // Assert
        userProfile.PreferredFantasyThemes.Should().HaveCount(1);
        userProfile.PreferredFantasyThemes.Should().Contain(theme);
    }

    [Fact]
    public void UserProfile_SetMultipleThemes_SetsCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile();
        var themes = new List<string> { "Classic Fantasy", "Dragons & Knights", "Mystery & Puzzles" };

        // Act
        userProfile.PreferredFantasyThemes = themes;

        // Assert
        userProfile.PreferredFantasyThemes.Should().BeEquivalentTo(themes);
        userProfile.PreferredFantasyThemes.Should().HaveCount(3);
    }
}

public class FantasyThemesTests
{
    [Fact]
    public void FantasyThemes_Available_ContainsExpectedThemes()
    {
        // Act & Assert
        FantasyThemes.Available.Should().NotBeEmpty();
        FantasyThemes.Available.Should().Contain("Classic Fantasy");
        FantasyThemes.Available.Should().Contain("Medieval Adventure");
        FantasyThemes.Available.Should().Contain("Magic & Wizards");
        FantasyThemes.Available.Should().Contain("Dragons & Knights");
        FantasyThemes.Available.Should().Contain("Forest Adventures");
        FantasyThemes.Available.Should().Contain("Mystery & Puzzles");
        FantasyThemes.Available.Should().Contain("Fairy Tales");
        FantasyThemes.Available.Should().Contain("Animal Companions");
        FantasyThemes.Available.Should().Contain("Underwater Worlds");
        FantasyThemes.Available.Should().Contain("Sky Adventures");
    }

    [Fact]
    public void FantasyThemes_Available_HasExpectedCount()
    {
        // Act & Assert
        FantasyThemes.Available.Should().HaveCount(10);
    }

    [Fact]
    public void FantasyThemes_Available_AllThemesAreStrings()
    {
        // Act & Assert
        FantasyThemes.Available.Should().AllBeOfType<string>();
        FantasyThemes.Available.Should().OnlyContain(theme => !string.IsNullOrEmpty(theme));
    }
}