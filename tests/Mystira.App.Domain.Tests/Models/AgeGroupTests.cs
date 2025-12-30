using Mystira.Domain.Models;

namespace Mystira.Domain.Tests.Models;

public class AgeGroupTests
{
    #region Constructor Tests

    [Theory]
    [InlineData("1-2", 1, 2)]
    [InlineData("3-5", 3, 5)]
    [InlineData("6-9", 6, 9)]
    [InlineData("10-12", 10, 12)]
    [InlineData("13-18", 13, 18)]
    [InlineData("19-150", 19, 150)]
    public void Constructor_WithValidString_ParsesCorrectly(string value, int expectedMin, int expectedMax)
    {
        // Act
        var ageGroup = new AgeGroup(value);

        // Assert
        ageGroup.MinimumAge.Should().Be(expectedMin);
        ageGroup.MaximumAge.Should().Be(expectedMax);
        ageGroup.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("abc-def")]
    public void Constructor_WithInvalidString_DefaultsToSixToNine(string value)
    {
        // Act
        var ageGroup = new AgeGroup(value);

        // Assert
        ageGroup.MinimumAge.Should().Be(6);
        ageGroup.MaximumAge.Should().Be(9);
    }

    [Fact]
    public void Constructor_WithIntParameters_SetsCorrectly()
    {
        // Act
        var ageGroup = new AgeGroup(10, 15);

        // Assert
        ageGroup.MinimumAge.Should().Be(10);
        ageGroup.MaximumAge.Should().Be(15);
        ageGroup.Value.Should().Be("10-15");
    }

    [Fact]
    public void DefaultConstructor_DefaultsToSixToNine()
    {
        // Act
        var ageGroup = new AgeGroup();

        // Assert
        ageGroup.MinimumAge.Should().Be(6);
        ageGroup.MaximumAge.Should().Be(9);
    }

    #endregion

    #region IsAppropriateFor Tests

    [Theory]
    [InlineData(6, 5, true)]   // Age group 6-9, requires min 5 - appropriate
    [InlineData(6, 6, true)]   // Age group 6-9, requires min 6 - appropriate
    [InlineData(6, 7, false)]  // Age group 6-9, requires min 7 - not appropriate
    [InlineData(13, 10, true)] // Age group 13-18, requires min 10 - appropriate
    [InlineData(3, 6, false)]  // Age group 3-5, requires min 6 - not appropriate
    public void IsAppropriateFor_WithRequiredAge_ReturnsCorrectResult(int ageGroupMin, int requiredMin, bool expected)
    {
        // Arrange
        var ageGroup = new AgeGroup(ageGroupMin, ageGroupMin + 3);

        // Act
        var result = ageGroup.IsAppropriateFor(requiredMin);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsAppropriateFor_WithTargetAgeGroup_ComparesMinimumAges()
    {
        // Arrange
        var teenAgeGroup = new AgeGroup(13, 18);
        var childAgeGroup = new AgeGroup(6, 9);

        // Act & Assert
        teenAgeGroup.IsAppropriateFor(childAgeGroup).Should().BeTrue();  // 13 >= 6
        childAgeGroup.IsAppropriateFor(teenAgeGroup).Should().BeFalse(); // 6 >= 13
    }

    #endregion

    #region IsContentAppropriate Tests

    [Theory]
    [InlineData("6-9", "13-18", true)]   // Content for 6+, viewer is 13+ - appropriate
    [InlineData("13-18", "6-9", false)]  // Content for 13+, viewer is 6-9 - not appropriate
    [InlineData("6-9", "6-9", true)]     // Same age group - appropriate
    [InlineData("3-5", "10-12", true)]   // Content for 3+, viewer is 10+ - appropriate
    public void IsContentAppropriate_ReturnsCorrectResult(string contentAgeGroup, string viewerAgeGroup, bool expected)
    {
        // Act
        var result = AgeGroup.IsContentAppropriate(contentAgeGroup, viewerAgeGroup);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "6-9")]
    [InlineData("6-9", null)]
    [InlineData("invalid", "6-9")]
    [InlineData("6-9", "invalid")]
    public void IsContentAppropriate_WithInvalidInput_ReturnsTrue(string? contentAgeGroup, string? viewerAgeGroup)
    {
        // Act
        var result = AgeGroup.IsContentAppropriate(contentAgeGroup!, viewerAgeGroup!);

        // Assert
        result.Should().BeTrue(); // Defaults to allowing content when parsing fails
    }

    #endregion

    #region All Property Tests

    [Fact]
    public void All_ContainsExpectedAgeGroups()
    {
        // Assert
        AgeGroup.All.Should().Contain("1-2");
        AgeGroup.All.Should().Contain("3-5");
        AgeGroup.All.Should().Contain("6-9");
        AgeGroup.All.Should().Contain("10-12");
        AgeGroup.All.Should().Contain("13-18");
        AgeGroup.All.Should().Contain("19-150");
        AgeGroup.All.Should().HaveCount(6);
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_WithValidString_ReturnsAgeGroup()
    {
        // Act
        var result = AgeGroup.Parse("10-12");

        // Assert
        result.Should().NotBeNull();
        result!.MinimumAge.Should().Be(10);
        result.MaximumAge.Should().Be(12);
    }

    #endregion
}
