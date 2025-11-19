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
        string name)
    {
        // Arrange
        var userProfile = new UserProfile();
        var testTime = DateTime.UtcNow;
        var preferredThemes = new List<FantasyTheme>
        {
            FantasyTheme.ClassicFantasy,
            FantasyTheme.DragonsAndKnights
        };

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
        var theme = FantasyTheme.ClassicFantasy;

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
        var themes = new List<FantasyTheme>
        {
            FantasyTheme.ClassicFantasy,
            FantasyTheme.DragonsAndKnights,
            FantasyTheme.MysteryAndPuzzles
        };

        // Act
        userProfile.PreferredFantasyThemes = themes;

        // Assert
        userProfile.PreferredFantasyThemes.Should().BeEquivalentTo(themes);
        userProfile.PreferredFantasyThemes.Should().HaveCount(3);
    }

    [Fact]
    public void UserProfile_CurrentAge_CalculatesCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile();
        var today = DateTime.Today;
        userProfile.DateOfBirth = new DateTime(today.Year - 10, today.Month, today.Day);

        // Act
        var age = userProfile.CurrentAge;

        // Assert
        age.Should().Be(10);
    }


    [Fact]
    public void UpdateAgeGroupFromBirthDate_HandlesAgesAboveMax()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            DateOfBirth = DateTime.Today.AddYears(-25)
        };

        // Act
        userProfile.UpdateAgeGroupFromBirthDate();

        // Assert
        userProfile.AgeGroup.Should().Be(AgeGroup.Teens);
    }

    [Fact]
    public void GetAgeGroupFromBirthDate_ReturnsCorrectAgeGroup()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            DateOfBirth = DateTime.Today.AddYears(-10)
        };

        // Act
        var ageGroup = userProfile.GetAgeGroupFromBirthDate();

        // Assert
        ageGroup.Should().Be(AgeGroup.Preteens);
    }
}
