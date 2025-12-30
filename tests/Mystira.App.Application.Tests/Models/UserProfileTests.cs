using FluentAssertions;
using Mystira.Domain.Models;

namespace Mystira.App.Application.Tests.Models;

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
        userProfile.AgeGroup.Should().Be(new AgeGroup("6-9")); // Default value
        userProfile.HasCompletedOnboarding.Should().BeFalse();
        userProfile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
}
