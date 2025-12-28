using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Tests.Models;

public class UserProfileTests
{
    #region CurrentAge Tests

    [Fact]
    public void CurrentAge_WhenDateOfBirthIsNull_ReturnsNull()
    {
        // Arrange
        var profile = new UserProfile { DateOfBirth = null };

        // Act
        var age = profile.CurrentAge;

        // Assert
        age.Should().BeNull();
    }

    [Fact]
    public void CurrentAge_WhenDateOfBirthIsSet_ReturnsCorrectAge()
    {
        // Arrange
        var today = DateTime.Today;
        var birthDate = today.AddYears(-10);
        var profile = new UserProfile { DateOfBirth = birthDate };

        // Act
        var age = profile.CurrentAge;

        // Assert
        age.Should().Be(10);
    }

    [Fact]
    public void CurrentAge_WhenBirthdayNotYetOccurredThisYear_ReturnsCorrectAge()
    {
        // Arrange
        var today = DateTime.Today;
        // Set birthday to 6 months from now, but 10 years ago
        var birthDate = today.AddYears(-10).AddMonths(6);
        var profile = new UserProfile { DateOfBirth = birthDate };

        // Act
        var age = profile.CurrentAge;

        // Assert
        // If birthday hasn't happened yet this year, age should be one less
        if (birthDate.Date > today.AddYears(-10))
        {
            age.Should().Be(9);
        }
        else
        {
            age.Should().Be(10);
        }
    }

    [Fact]
    public void CurrentAge_WhenBirthdayIsToday_ReturnsCorrectAge()
    {
        // Arrange
        var today = DateTime.Today;
        var birthDate = today.AddYears(-8);
        var profile = new UserProfile { DateOfBirth = birthDate };

        // Act
        var age = profile.CurrentAge;

        // Assert
        age.Should().Be(8);
    }

    #endregion

    #region GetAgeGroupFromBirthDate Tests

    [Fact]
    public void GetAgeGroupFromBirthDate_WhenDateOfBirthIsNull_ReturnsNull()
    {
        // Arrange
        var profile = new UserProfile { DateOfBirth = null };

        // Act
        var ageGroup = profile.GetAgeGroupFromBirthDate();

        // Assert
        ageGroup.Should().BeNull();
    }

    [Theory]
    [InlineData(1, "1-2")]
    [InlineData(2, "1-2")]
    [InlineData(3, "3-5")]
    [InlineData(5, "3-5")]
    [InlineData(6, "6-9")]
    [InlineData(9, "6-9")]
    [InlineData(10, "10-12")]
    [InlineData(12, "10-12")]
    [InlineData(13, "13-18")]
    [InlineData(18, "13-18")]
    [InlineData(19, "19-150")]
    [InlineData(50, "19-150")]
    public void GetAgeGroupFromBirthDate_ReturnsCorrectAgeGroup(int age, string expectedAgeGroup)
    {
        // Arrange
        var today = DateTime.Today;
        var birthDate = today.AddYears(-age);
        var profile = new UserProfile { DateOfBirth = birthDate };

        // Act
        var ageGroup = profile.GetAgeGroupFromBirthDate();

        // Assert
        ageGroup.Should().NotBeNull();
        ageGroup!.Value.Should().Be(expectedAgeGroup);
    }

    [Fact]
    public void UpdateAgeGroupFromBirthDate_UpdatesAgeGroupCorrectly()
    {
        // Arrange
        var today = DateTime.Today;
        var profile = new UserProfile
        {
            DateOfBirth = today.AddYears(-7),
            AgeGroupName = "1-2" // Start with wrong age group
        };

        // Act
        profile.UpdateAgeGroupFromBirthDate();

        // Assert
        profile.AgeGroupName.Should().Be("6-9");
    }

    [Fact]
    public void UpdateAgeGroupFromBirthDate_WhenDateOfBirthIsNull_DoesNotChangeAgeGroup()
    {
        // Arrange
        var profile = new UserProfile
        {
            DateOfBirth = null,
            AgeGroupName = "6-9"
        };

        // Act
        profile.UpdateAgeGroupFromBirthDate();

        // Assert
        profile.AgeGroupName.Should().Be("6-9");
    }

    #endregion

    #region Badge Tests

    [Fact]
    public void GetBadgesForAxis_ReturnsOnlyBadgesForSpecifiedAxis()
    {
        // Arrange
        var profile = new UserProfile();
        profile.EarnedBadges.Add(new UserBadge { Axis = "honesty", BadgeConfigurationId = "b1", EarnedAt = DateTime.UtcNow });
        profile.EarnedBadges.Add(new UserBadge { Axis = "courage", BadgeConfigurationId = "b2", EarnedAt = DateTime.UtcNow });
        profile.EarnedBadges.Add(new UserBadge { Axis = "honesty", BadgeConfigurationId = "b3", EarnedAt = DateTime.UtcNow });

        // Act
        var honestyBadges = profile.GetBadgesForAxis("honesty");

        // Assert
        honestyBadges.Should().HaveCount(2);
        honestyBadges.Should().OnlyContain(b => b.Axis == "honesty");
    }

    [Fact]
    public void GetBadgesForAxis_IsCaseInsensitive()
    {
        // Arrange
        var profile = new UserProfile();
        profile.EarnedBadges.Add(new UserBadge { Axis = "Honesty", BadgeConfigurationId = "b1", EarnedAt = DateTime.UtcNow });

        // Act
        var badges = profile.GetBadgesForAxis("HONESTY");

        // Assert
        badges.Should().HaveCount(1);
    }

    [Fact]
    public void GetBadgesForAxis_ReturnsSortedByEarnedDateDescending()
    {
        // Arrange
        var profile = new UserProfile();
        var older = DateTime.UtcNow.AddDays(-10);
        var newer = DateTime.UtcNow;
        profile.EarnedBadges.Add(new UserBadge { Axis = "honesty", BadgeConfigurationId = "b1", EarnedAt = older });
        profile.EarnedBadges.Add(new UserBadge { Axis = "honesty", BadgeConfigurationId = "b2", EarnedAt = newer });

        // Act
        var badges = profile.GetBadgesForAxis("honesty");

        // Assert
        badges[0].EarnedAt.Should().Be(newer);
        badges[1].EarnedAt.Should().Be(older);
    }

    [Fact]
    public void HasEarnedBadge_WhenBadgeExists_ReturnsTrue()
    {
        // Arrange
        var profile = new UserProfile();
        profile.EarnedBadges.Add(new UserBadge { BadgeConfigurationId = "badge-123" });

        // Act
        var result = profile.HasEarnedBadge("badge-123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasEarnedBadge_WhenBadgeDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var profile = new UserProfile();

        // Act
        var result = profile.HasEarnedBadge("badge-123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AddEarnedBadge_WhenBadgeNotAlreadyEarned_AddsBadge()
    {
        // Arrange
        var profile = new UserProfile();
        var badge = new UserBadge { BadgeConfigurationId = "badge-123", Axis = "honesty" };

        // Act
        profile.AddEarnedBadge(badge);

        // Assert
        profile.EarnedBadges.Should().HaveCount(1);
        profile.EarnedBadges[0].UserProfileId.Should().Be(profile.Id);
    }

    [Fact]
    public void AddEarnedBadge_WhenBadgeAlreadyEarned_DoesNotAddDuplicate()
    {
        // Arrange
        var profile = new UserProfile();
        var badge1 = new UserBadge { BadgeConfigurationId = "badge-123", Axis = "honesty" };
        var badge2 = new UserBadge { BadgeConfigurationId = "badge-123", Axis = "honesty" };
        profile.AddEarnedBadge(badge1);

        // Act
        profile.AddEarnedBadge(badge2);

        // Assert
        profile.EarnedBadges.Should().HaveCount(1);
    }

    #endregion

    #region AgeGroup Property Tests

    [Fact]
    public void AgeGroup_GetterParsesStoredString()
    {
        // Arrange
        var profile = new UserProfile { AgeGroupName = "10-12" };

        // Act
        var ageGroup = profile.AgeGroup;

        // Assert
        ageGroup.MinimumAge.Should().Be(10);
        ageGroup.MaximumAge.Should().Be(12);
    }

    [Fact]
    public void AgeGroup_SetterUpdatesStoredString()
    {
        // Arrange
        var profile = new UserProfile();
        var ageGroup = new AgeGroup(13, 18);

        // Act
        profile.AgeGroup = ageGroup;

        // Assert
        profile.AgeGroupName.Should().Be("13-18");
    }

    #endregion
}
