using FluentAssertions;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.Domain;

public class AgeGroupConstantsTests
{
    #region AllAgeGroups Tests

    [Fact]
    public void AllAgeGroups_ContainsExpectedGroups()
    {
        // Assert
        AgeGroupConstants.AllAgeGroups.Should().HaveCount(6);
        AgeGroupConstants.AllAgeGroups.Should().Contain("1-2");
        AgeGroupConstants.AllAgeGroups.Should().Contain("3-5");
        AgeGroupConstants.AllAgeGroups.Should().Contain("6-9");
        AgeGroupConstants.AllAgeGroups.Should().Contain("10-12");
        AgeGroupConstants.AllAgeGroups.Should().Contain("13-18");
        AgeGroupConstants.AllAgeGroups.Should().Contain("19+");
    }

    #endregion

    #region GetAgeGroupForAge Tests

    [Theory]
    [InlineData(1, "1-2")]
    [InlineData(2, "1-2")]
    [InlineData(3, "3-5")]
    [InlineData(4, "3-5")]
    [InlineData(5, "3-5")]
    [InlineData(6, "6-9")]
    [InlineData(7, "6-9")]
    [InlineData(8, "6-9")]
    [InlineData(9, "6-9")]
    [InlineData(10, "10-12")]
    [InlineData(11, "10-12")]
    [InlineData(12, "10-12")]
    [InlineData(13, "13-18")]
    [InlineData(15, "13-18")]
    [InlineData(18, "13-18")]
    [InlineData(19, "19+")]
    [InlineData(25, "19+")]
    [InlineData(50, "19+")]
    public void GetAgeGroupForAge_ReturnsCorrectGroup(int age, string expectedGroup)
    {
        // Act
        var result = AgeGroupConstants.GetAgeGroupForAge(age);

        // Assert
        result.Should().Be(expectedGroup);
    }

    [Theory]
    [InlineData(0, "1-2")]   // Edge case: under 1
    [InlineData(-1, "1-2")]  // Edge case: negative
    public void GetAgeGroupForAge_WithEdgeCases_ReturnsDefaultGroup(int age, string expectedGroup)
    {
        // Act
        var result = AgeGroupConstants.GetAgeGroupForAge(age);

        // Assert
        result.Should().Be(expectedGroup);
    }

    #endregion

    #region GetDisplayName Tests

    [Theory]
    [InlineData("1-2", "Ages 1-2 (Toddlers)")]
    [InlineData("3-5", "Ages 3-5 (Preschoolers)")]
    [InlineData("6-9", "Ages 6-9 (School Age)")]
    [InlineData("10-12", "Ages 10-12 (Preteens)")]
    [InlineData("13-18", "Ages 13-18 (Teens)")]
    [InlineData("19+", "Ages 19+ (Adults)")]
    public void GetDisplayName_ReturnsCorrectDisplayName(string ageGroup, string expectedName)
    {
        // Act
        var result = AgeGroupConstants.GetDisplayName(ageGroup);

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void GetDisplayName_WithUnknownGroup_ReturnsGroupAsIs()
    {
        // Act
        var result = AgeGroupConstants.GetDisplayName("unknown");

        // Assert
        result.Should().Be("unknown");
    }

    #endregion
}
