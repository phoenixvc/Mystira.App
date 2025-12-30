using System.Collections.Generic;
using FluentAssertions;
using Mystira.Domain.Models;
using Xunit;

namespace Mystira.App.Api.Tests.Models;

public class ScenarioValidationTests
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
                        new() { Choice = "Go somewhere", NextSceneId = "nonexistent" }
                    }
                }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Scene 'Scene 1' has a branch with an invalid NextSceneId: nonexistent");
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenBranchNextSceneIdIsEmpty_StoryEnding()
    {
        // Arrange - a branch with empty NextSceneId (story ending)
        var scenario = new Scenario
        {
            Title = "Test Scenario",
            Scenes = new List<Scene>
            {
                new()
                {
                    Id = "1",
                    Title = "Scene 1",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new() { Choice = "Continue", NextSceneId = "2" },
                        new() { Choice = "End the story", NextSceneId = "" } // Story ending
                    }
                },
                new()
                {
                    Id = "2",
                    Title = "Scene 2",
                    Type = SceneType.Special,
                    NextSceneId = null // No next scene (also an ending)
                }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenSpecialSceneHasNoNextScene()
    {
        // Arrange - a special scene with no next scene (story ending)
        var scenario = new Scenario
        {
            Title = "Test Scenario",
            Scenes = new List<Scene>
            {
                new()
                {
                    Id = "1",
                    Title = "Opening Scene",
                    Type = SceneType.Narrative,
                    NextSceneId = "2"
                },
                new()
                {
                    Id = "2",
                    Title = "The End",
                    Type = SceneType.Special,
                    NextSceneId = null // No next scene - this is the ending
                }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }
}
