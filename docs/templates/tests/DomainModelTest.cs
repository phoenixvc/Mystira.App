// Example Domain Model Test
// File: tests/Mystira.Domain.Tests/Models/ScenarioTests.cs

using FluentAssertions;
using Mystira.Domain.Models;
using Xunit;

namespace Mystira.Domain.Tests.Models;

/// <summary>
/// Example tests for a domain model (Scenario entity)
/// Demonstrates testing business logic, validation, and invariants
/// </summary>
public class ScenarioTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var scenario = new Scenario();

        // Assert
        scenario.Id.Should().NotBeEmpty("ID should be auto-generated");
        scenario.Title.Should().BeEmpty("Title should default to empty string");
        scenario.Scenes.Should().NotBeNull().And.BeEmpty("Scenes collection should be initialized but empty");
        scenario.IsActive.Should().BeTrue("Scenarios should be active by default");
        scenario.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetTitle_WithInvalidValue_ThrowsArgumentException(string invalidTitle)
    {
        // Arrange
        var scenario = new Scenario();

        // Act
        var action = () => scenario.Title = invalidTitle;

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot be empty*");
    }

    [Fact]
    public void AddScene_WithValidScene_AddsToCollection()
    {
        // Arrange
        var scenario = new Scenario { Id = "scenario-1" };
        var scene = new Scene
        {
            Id = "scene-1",
            ScenarioId = "scenario-1",
            Narrative = "You enter a dark cave..."
        };

        // Act
        scenario.Scenes.Add(scene);

        // Assert
        scenario.Scenes.Should().ContainSingle()
            .Which.Should().Be(scene);
    }

    [Fact]
    public void GetTotalScenes_ReturnsCorrectCount()
    {
        // Arrange
        var scenario = new Scenario { Id = "scenario-1" };
        scenario.Scenes.Add(new Scene { Id = "scene-1", ScenarioId = "scenario-1" });
        scenario.Scenes.Add(new Scene { Id = "scene-2", ScenarioId = "scenario-1" });
        scenario.Scenes.Add(new Scene { Id = "scene-3", ScenarioId = "scenario-1" });

        // Act
        var count = scenario.GetTotalScenes();

        // Assert
        count.Should().Be(3);
    }

    [Theory]
    [InlineData(ScenarioGenre.Fantasy, "5-7")]
    [InlineData(ScenarioGenre.SciFi, "8-10")]
    [InlineData(ScenarioGenre.Mystery, "10-12")]
    public void Scenario_WithGenreAndAgeGroup_SetsCorrectly(ScenarioGenre genre, string ageGroup)
    {
        // Act
        var scenario = new Scenario
        {
            Genre = genre,
            AgeGroup = ageGroup
        };

        // Assert
        scenario.Genre.Should().Be(genre);
        scenario.AgeGroup.Should().Be(ageGroup);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var scenario = new Scenario { IsActive = true };

        // Act
        scenario.Deactivate();

        // Assert
        scenario.IsActive.Should().BeFalse();
        scenario.DeactivatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
