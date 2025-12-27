using FluentAssertions;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.Domain;

public class ScenarioGraphValidatorTests
{
    private readonly ScenarioGraphValidator _validator = new();

    #region Empty/Null Scenarios

    [Fact]
    public void ValidateGraph_WithNullScenes_ReturnsTrue()
    {
        // Arrange
        var scenario = new Scenario { Id = "test", Scenes = null! };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateGraph_WithEmptyScenes_ReturnsTrue()
    {
        // Arrange
        var scenario = new Scenario { Id = "test", Scenes = new List<Scene>() };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Linear Scenario Tests

    [Fact]
    public void ValidateGraph_WithLinearScenario_ReturnsTrue()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Start", NextSceneId = "scene-2" },
                new Scene { Id = "scene-2", Title = "Middle", NextSceneId = "scene-3" },
                new Scene { Id = "scene-3", Title = "End", NextSceneId = null }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateGraph_WithSingleScene_ReturnsTrue()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Only Scene", NextSceneId = null }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Branching Scenario Tests

    [Fact]
    public void ValidateGraph_WithBranchingScenario_ReturnsTrue()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "scene-1",
                    Title = "Start",
                    Branches = new List<Branch>
                    {
                        new Branch { Text = "Go left", NextSceneId = "scene-2" },
                        new Branch { Text = "Go right", NextSceneId = "scene-3" }
                    }
                },
                new Scene { Id = "scene-2", Title = "Left Path", NextSceneId = "scene-4" },
                new Scene { Id = "scene-3", Title = "Right Path", NextSceneId = "scene-4" },
                new Scene { Id = "scene-4", Title = "End", NextSceneId = null }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Cycle Detection Tests

    [Fact]
    public void ValidateGraph_WithDirectCycle_ReturnsFalseWithError()
    {
        // Arrange - Scene-1 -> Scene-2 -> Scene-1 (cycle)
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Start", NextSceneId = "scene-2" },
                new Scene { Id = "scene-2", Title = "Loop Back", NextSceneId = "scene-1" }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Infinite loop"));
    }

    [Fact]
    public void ValidateGraph_WithSelfLoop_ReturnsFalseWithError()
    {
        // Arrange - Scene points to itself
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Self Loop", NextSceneId = "scene-1" }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Infinite loop"));
    }

    [Fact]
    public void ValidateGraph_WithCycleInBranch_ReturnsFalseWithError()
    {
        // Arrange - Branch creates a cycle
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "scene-1",
                    Title = "Start",
                    Branches = new List<Branch>
                    {
                        new Branch { Text = "Continue", NextSceneId = "scene-2" }
                    }
                },
                new Scene
                {
                    Id = "scene-2",
                    Title = "Middle",
                    Branches = new List<Branch>
                    {
                        new Branch { Text = "Loop back", NextSceneId = "scene-1" }
                    }
                }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Infinite loop"));
    }

    [Fact]
    public void ValidateGraph_WithLongerCycle_ReturnsFalseWithError()
    {
        // Arrange - Scene-1 -> Scene-2 -> Scene-3 -> Scene-1 (cycle)
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Start", NextSceneId = "scene-2" },
                new Scene { Id = "scene-2", Title = "Middle", NextSceneId = "scene-3" },
                new Scene { Id = "scene-3", Title = "Loop Back", NextSceneId = "scene-1" }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Infinite loop"));
    }

    #endregion

    #region Unreachable Scene Tests

    [Fact]
    public void ValidateGraph_WithUnreachableScene_ReturnsFalseWithError()
    {
        // Arrange - Scene-3 is not reachable from Scene-1
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Start", NextSceneId = "scene-2" },
                new Scene { Id = "scene-2", Title = "End", NextSceneId = null },
                new Scene { Id = "scene-3", Title = "Orphan", NextSceneId = null }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("unreachable") && e.Contains("Orphan"));
    }

    [Fact]
    public void ValidateGraph_WithMultipleUnreachableScenes_ReportsAll()
    {
        // Arrange - Scene-3 and Scene-4 are not reachable
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene-1", Title = "Start", NextSceneId = "scene-2" },
                new Scene { Id = "scene-2", Title = "End", NextSceneId = null },
                new Scene { Id = "scene-3", Title = "Orphan 1", NextSceneId = null },
                new Scene { Id = "scene-4", Title = "Orphan 2", NextSceneId = null }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.Contains("Orphan 1"));
        errors.Should().Contain(e => e.Contains("Orphan 2"));
    }

    #endregion

    #region Diamond Pattern Tests

    [Fact]
    public void ValidateGraph_WithDiamondPattern_ReturnsTrue()
    {
        // Arrange - Branches converge (diamond pattern - not a cycle)
        //      Scene-1
        //      /     \
        //  Scene-2  Scene-3
        //      \     /
        //      Scene-4
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "scene-1",
                    Title = "Start",
                    Branches = new List<Branch>
                    {
                        new Branch { Text = "Left", NextSceneId = "scene-2" },
                        new Branch { Text = "Right", NextSceneId = "scene-3" }
                    }
                },
                new Scene { Id = "scene-2", Title = "Left", NextSceneId = "scene-4" },
                new Scene { Id = "scene-3", Title = "Right", NextSceneId = "scene-4" },
                new Scene { Id = "scene-4", Title = "Converge", NextSceneId = null }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion

    #region Mixed NextSceneId and Branches

    [Fact]
    public void ValidateGraph_WithMixedNavigation_ReturnsTrue()
    {
        // Arrange - Scene has both NextSceneId and Branches
        var scenario = new Scenario
        {
            Id = "test",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "scene-1",
                    Title = "Start",
                    NextSceneId = "scene-2",
                    Branches = new List<Branch>
                    {
                        new Branch { Text = "Alternative", NextSceneId = "scene-3" }
                    }
                },
                new Scene { Id = "scene-2", Title = "Default Path", NextSceneId = null },
                new Scene { Id = "scene-3", Title = "Alternative Path", NextSceneId = null }
            }
        };

        // Act
        var result = _validator.ValidateGraph(scenario, out var errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion
}
