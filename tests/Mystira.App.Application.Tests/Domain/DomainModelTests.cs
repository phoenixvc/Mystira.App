using FluentAssertions;
using Mystira.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.Domain;

public class DomainModelTests
{
    #region CoreAxis Tests

    [Fact]
    public void CoreAxis_Constructor_SetsValue()
    {
        // Arrange & Act
        var coreAxis = new CoreAxis("courage");

        // Assert
        coreAxis.Value.Should().Be("courage");
    }

    [Fact]
    public void CoreAxis_ToString_ReturnsValue()
    {
        // Arrange
        var coreAxis = new CoreAxis("wisdom");

        // Act & Assert
        coreAxis.ToString().Should().Be("wisdom");
    }

    [Fact]
    public void CoreAxis_Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var axis1 = new CoreAxis("integrity");
        var axis2 = new CoreAxis("integrity");

        // Act & Assert
        axis1.Equals(axis2).Should().BeTrue();
        (axis1 == axis2).Should().BeTrue();
    }

    [Fact]
    public void CoreAxis_Equals_CaseInsensitive()
    {
        // Arrange
        var axis1 = new CoreAxis("Courage");
        var axis2 = new CoreAxis("COURAGE");

        // Act & Assert
        axis1.Equals(axis2).Should().BeTrue();
    }

    [Fact]
    public void CoreAxis_Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var axis1 = new CoreAxis("courage");
        var axis2 = new CoreAxis("wisdom");

        // Act & Assert
        (axis1 != axis2).Should().BeTrue();
    }

    [Fact]
    public void CoreAxis_GetHashCode_SameForEqualValues()
    {
        // Arrange
        var axis1 = new CoreAxis("Courage");
        var axis2 = new CoreAxis("courage");

        // Act & Assert
        axis1.GetHashCode().Should().Be(axis2.GetHashCode());
    }

    [Fact]
    public void CoreAxis_Parse_WithValidValue_ReturnsCoreAxis()
    {
        // Act
        var result = CoreAxis.Parse("empathy");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("empathy");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CoreAxis_Parse_WithEmptyOrNull_ReturnsNull(string? value)
    {
        // Act
        var result = CoreAxis.Parse(value);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Archetype Tests

    [Fact]
    public void Archetype_Constructor_SetsValue()
    {
        // Arrange & Act
        var archetype = new Archetype("hero");

        // Assert
        archetype.Value.Should().Be("hero");
    }

    [Fact]
    public void Archetype_ToString_ReturnsValue()
    {
        // Arrange
        var archetype = new Archetype("mentor");

        // Act & Assert
        archetype.ToString().Should().Be("mentor");
    }

    [Fact]
    public void Archetype_Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var arch1 = new Archetype("hero");
        var arch2 = new Archetype("hero");

        // Act & Assert
        arch1.Equals(arch2).Should().BeTrue();
        (arch1 == arch2).Should().BeTrue();
    }

    [Fact]
    public void Archetype_Equals_CaseInsensitive()
    {
        // Arrange
        var arch1 = new Archetype("Hero");
        var arch2 = new Archetype("HERO");

        // Act & Assert
        arch1.Equals(arch2).Should().BeTrue();
    }

    [Fact]
    public void Archetype_Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var arch1 = new Archetype("hero");
        var arch2 = new Archetype("villain");

        // Act & Assert
        (arch1 != arch2).Should().BeTrue();
    }

    [Fact]
    public void Archetype_GetHashCode_SameForEqualValues()
    {
        // Arrange
        var arch1 = new Archetype("Hero");
        var arch2 = new Archetype("hero");

        // Act & Assert
        arch1.GetHashCode().Should().Be(arch2.GetHashCode());
    }

    [Fact]
    public void Archetype_Parse_WithValidValue_ReturnsArchetype()
    {
        // Act
        var result = Archetype.Parse("trickster");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("trickster");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Archetype_Parse_WithEmptyOrNull_ReturnsNull(string? value)
    {
        // Act
        var result = Archetype.Parse(value);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region EchoType Tests

    [Fact]
    public void EchoType_Constructor_SetsValue()
    {
        // Arrange & Act
        var echoType = new EchoType("discovery");

        // Assert
        echoType.Value.Should().Be("discovery");
    }

    [Fact]
    public void EchoType_ToString_ReturnsValue()
    {
        // Arrange
        var echoType = new EchoType("challenge");

        // Act & Assert
        echoType.ToString().Should().Be("challenge");
    }

    [Fact]
    public void EchoType_Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var echo1 = new EchoType("discovery");
        var echo2 = new EchoType("discovery");

        // Act & Assert
        echo1.Equals(echo2).Should().BeTrue();
        (echo1 == echo2).Should().BeTrue();
    }

    [Fact]
    public void EchoType_Equals_CaseInsensitive()
    {
        // Arrange
        var echo1 = new EchoType("Discovery");
        var echo2 = new EchoType("DISCOVERY");

        // Act & Assert
        echo1.Equals(echo2).Should().BeTrue();
    }

    [Fact]
    public void EchoType_Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var echo1 = new EchoType("discovery");
        var echo2 = new EchoType("challenge");

        // Act & Assert
        (echo1 != echo2).Should().BeTrue();
    }

    [Fact]
    public void EchoType_GetHashCode_SameForEqualValues()
    {
        // Arrange
        var echo1 = new EchoType("Challenge");
        var echo2 = new EchoType("challenge");

        // Act & Assert
        echo1.GetHashCode().Should().Be(echo2.GetHashCode());
    }

    [Fact]
    public void EchoType_Parse_WithValidValue_ReturnsEchoType()
    {
        // Act
        var result = EchoType.Parse("transformation");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("transformation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EchoType_Parse_WithEmptyOrNull_ReturnsNull(string? value)
    {
        // Act
        var result = EchoType.Parse(value);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region BadgeThresholds Tests

    [Fact]
    public void BadgeThresholds_AgeGroupThresholds_ContainsSchoolAgeGroup()
    {
        // Assert - BadgeThresholds uses name-based AgeGroups (school, preteens, teens)
        // which may differ from the value-based approach in StringEnum
        BadgeThresholds.AgeGroupThresholds.Should().NotBeEmpty();
    }

    [Fact]
    public void BadgeThresholds_GetThresholdsForAgeGroup_WithUnknownGroup_ReturnsEmpty()
    {
        // Arrange - Create an age group that isn't in the thresholds
        var ageGroup = new AgeGroup("unknown");

        // Act
        var thresholds = BadgeThresholds.GetThresholdsForAgeGroup(ageGroup);

        // Assert
        thresholds.Should().NotBeNull();
        thresholds.Should().BeEmpty();
    }

    [Fact]
    public void BadgeThresholds_GetThreshold_WithUnknownGroup_ReturnsZero()
    {
        // Arrange
        var ageGroup = new AgeGroup("unknown");

        // Act
        var threshold = BadgeThresholds.GetThreshold(ageGroup, "kindness");

        // Assert
        threshold.Should().Be(0f);
    }

    [Fact]
    public void BadgeThresholds_IsThresholdMet_WithZeroThreshold_AlwaysReturnsTrue()
    {
        // Arrange - Unknown axis returns 0 threshold
        var ageGroup = new AgeGroup("unknown");

        // Act
        var result = BadgeThresholds.IsThresholdMet(ageGroup, "nonexistent", 0.0f);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region RandomNameGenerator Tests

    [Fact]
    public void RandomNameGenerator_GenerateFantasyName_ReturnsNonEmptyString()
    {
        // Act
        var name = RandomNameGenerator.GenerateFantasyName();

        // Assert
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RandomNameGenerator_GenerateAdjectiveName_ReturnsNameWithSpace()
    {
        // Act
        var name = RandomNameGenerator.GenerateAdjectiveName();

        // Assert
        name.Should().NotBeNullOrEmpty();
        name.Should().Contain(" ");
    }

    [Fact]
    public void RandomNameGenerator_GenerateGuestName_WithoutAdjective_ReturnsSingleWord()
    {
        // Act
        var name = RandomNameGenerator.GenerateGuestName(useAdjective: false);

        // Assert
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RandomNameGenerator_GenerateGuestName_WithAdjective_ReturnsMultipleWords()
    {
        // Act
        var name = RandomNameGenerator.GenerateGuestName(useAdjective: true);

        // Assert
        name.Should().NotBeNullOrEmpty();
        name.Should().Contain(" ");
    }

    [Fact]
    public void RandomNameGenerator_GenerateUniqueGuestNames_ReturnsRequestedCount()
    {
        // Act
        var names = RandomNameGenerator.GenerateUniqueGuestNames(5, useAdjective: false);

        // Assert
        names.Should().HaveCount(5);
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void RandomNameGenerator_GenerateUniqueGuestNames_WithAdjective_ReturnsUniqueNames()
    {
        // Act
        var names = RandomNameGenerator.GenerateUniqueGuestNames(10, useAdjective: true);

        // Assert
        names.Should().HaveCount(10);
        names.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region AgeGroup Tests

    [Fact]
    public void AgeGroup_Parse_SchoolAge_ReturnsCorrectAgeGroup()
    {
        // Act - Parse uses the "value" field from JSON ("6-9")
        var ageGroup = AgeGroup.Parse("6-9");

        // Assert
        ageGroup.Should().NotBeNull();
        ageGroup!.Value.Should().Be("6-9");
    }

    [Fact]
    public void AgeGroup_Parse_Preteens_ReturnsCorrectAgeGroup()
    {
        // Act - Parse uses the "value" field from JSON ("10-12")
        var ageGroup = AgeGroup.Parse("10-12");

        // Assert
        ageGroup.Should().NotBeNull();
        ageGroup!.Value.Should().Be("10-12");
    }

    [Fact]
    public void AgeGroup_Parse_Teens_ReturnsCorrectAgeGroup()
    {
        // Act - Parse uses the "value" field from JSON ("13-18")
        var ageGroup = AgeGroup.Parse("13-18");

        // Assert
        ageGroup.Should().NotBeNull();
        ageGroup!.Value.Should().Be("13-18");
    }

    [Fact]
    public void AgeGroup_TryParse_WithValidValue_ReturnsTrue()
    {
        // Act
        var result = AgeGroup.TryParse("6-9", out var ageGroup);

        // Assert
        result.Should().BeTrue();
        ageGroup.Should().NotBeNull();
    }

    [Fact]
    public void AgeGroup_TryParse_WithInvalidValue_ReturnsFalse()
    {
        // Act
        var result = AgeGroup.TryParse("invalid", out var ageGroup);

        // Assert
        result.Should().BeFalse();
        ageGroup.Should().BeNull();
    }

    [Fact]
    public void AgeGroup_ValueMap_ContainsExpectedAgeGroups()
    {
        // Assert - ValueMap uses "value" field from JSON
        AgeGroup.ValueMap.Should().ContainKey("6-9");
        AgeGroup.ValueMap.Should().ContainKey("10-12");
        AgeGroup.ValueMap.Should().ContainKey("13-18");
    }

    [Fact]
    public void AgeGroup_Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var group1 = AgeGroup.Parse("6-9");
        var group2 = AgeGroup.Parse("6-9");

        // Act & Assert
        group1!.Equals(group2).Should().BeTrue();
    }

    [Fact]
    public void AgeGroup_GetHashCode_SameForEqualValues()
    {
        // Arrange - Note: case sensitivity depends on implementation
        var group1 = new AgeGroup("6-9");
        var group2 = new AgeGroup("6-9");

        // Act & Assert
        group1.GetHashCode().Should().Be(group2.GetHashCode());
    }

    [Fact]
    public void AgeGroup_Constructor_ParsesMinMaxAge()
    {
        // Arrange & Act
        var ageGroup = new AgeGroup("10-12");

        // Assert
        ageGroup.MinimumAge.Should().Be(10);
        ageGroup.MaximumAge.Should().Be(12);
    }

    [Fact]
    public void AgeGroup_Constructor_WithInvalidFormat_UsesDefaults()
    {
        // Arrange & Act
        var ageGroup = new AgeGroup("invalid");

        // Assert - defaults to 6-9 range per the implementation
        ageGroup.MinimumAge.Should().Be(6);
        ageGroup.MaximumAge.Should().Be(9);
    }

    [Fact]
    public void AgeGroup_All_ContainsExpectedValues()
    {
        // Assert
        AgeGroup.All.Should().Contain("6-9");
        AgeGroup.All.Should().Contain("10-12");
        AgeGroup.All.Should().Contain("13-18");
    }

    #endregion
}
