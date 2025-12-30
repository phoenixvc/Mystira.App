using FluentAssertions;
using Mystira.Domain.Models;

namespace Mystira.App.Application.Tests.Models;

public class ScenarioTests
{
    [Fact]
    public void Validate_ReturnsTrue_WhenScenarioIsValid()
    {
        // Arrange
        var scenario = new Scenario
        {
            Title = "Test Scenario",
            Scenes = new List<Scene>
            {
                new() { Id = "1", Title = "Scene 1", NextSceneId = "2" },
                new() { Id = "2", Title = "Scene 2" }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenTitleIsEmpty()
    {
        // Arrange
        var scenario = new Scenario
        {
            Title = "",
            Scenes = new List<Scene>
            {
                new() { Id = "1", Title = "Scene 1" }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Scenario title cannot be empty.");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenThereAreNoScenes()
    {
        // Arrange
        var scenario = new Scenario
        {
            Title = "Test Scenario",
            Scenes = new List<Scene>()
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Scenario must have at least one scene.");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenNextSceneIdIsInvalid()
    {
        // Arrange
        var scenario = new Scenario
        {
            Title = "Test Scenario",
            Scenes = new List<Scene>
            {
                new() { Id = "1", Title = "Scene 1", NextSceneId = "3" }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Scene 'Scene 1' has an invalid NextSceneId: 3");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenBranchNextSceneIdIsInvalid()
    {
        // Arrange
        var scenario = new Scenario
        {
            Title = "Test Scenario",
            Scenes = new List<Scene>
            {
                new()
                {
                    Id = "1",
                    Title = "Scene 1",
                    Branches = new List<Branch>
                    {
                        new() { NextSceneId = "3" }
                    }
                }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Scene 'Scene 1' has a branch with an invalid NextSceneId: 3");
    }
}
