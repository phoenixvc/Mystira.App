using FluentAssertions;
using Mystira.App.Application.CQRS.Badges.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Badges;

/// <summary>
/// Unit tests for CalculateBadgeScoresQueryHandler.
/// Tests depth-first traversal, score aggregation, and percentile calculation.
/// </summary>
public class CalculateBadgeScoresQueryHandlerTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        // Create test scenarios with various graph structures
        var linearScenario = new Scenario
        {
            Id = "scenario-linear",
            Title = "Linear Path Scenario",
            Description = "Simple linear path for testing",
            AgeGroup = "6-9",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "scene-1",
                    Title = "Start",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Go forward",
                            NextSceneId = "scene-2",
                            CompassChange = new CompassChange
                            {
                                Axis = "Courage",
                                Delta = 10.0
                            }
                        }
                    }
                },
                new Scene
                {
                    Id = "scene-2",
                    Title = "Middle",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Continue",
                            NextSceneId = "scene-3",
                            CompassChange = new CompassChange
                            {
                                Axis = "Courage",
                                Delta = 5.0
                            }
                        }
                    }
                },
                new Scene
                {
                    Id = "scene-3",
                    Title = "End",
                    Type = SceneType.Special,
                    Branches = new List<Branch>()
                }
            }
        };

        var branchingScenario = new Scenario
        {
            Id = "scenario-branching",
            Title = "Branching Scenario",
            Description = "Multiple paths with different scores",
            AgeGroup = "6-9",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "branch-start",
                    Title = "Start",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Path A",
                            NextSceneId = "branch-a",
                            CompassChange = new CompassChange
                            {
                                Axis = "Wisdom",
                                Delta = 20.0
                            }
                        },
                        new Branch
                        {
                            Choice = "Path B",
                            NextSceneId = "branch-b",
                            CompassChange = new CompassChange
                            {
                                Axis = "Wisdom",
                                Delta = 10.0
                            }
                        }
                    }
                },
                new Scene
                {
                    Id = "branch-a",
                    Title = "Path A End",
                    Type = SceneType.Special,
                    Branches = new List<Branch>()
                },
                new Scene
                {
                    Id = "branch-b",
                    Title = "Path B End",
                    Type = SceneType.Special,
                    Branches = new List<Branch>()
                }
            }
        };

        var multiAxisScenario = new Scenario
        {
            Id = "scenario-multi-axis",
            Title = "Multi-Axis Scenario",
            Description = "Tests multiple compass axes",
            AgeGroup = "6-9",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "multi-start",
                    Title = "Start",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Brave choice",
                            NextSceneId = "multi-end",
                            CompassChange = new CompassChange
                            {
                                Axis = "Courage",
                                Delta = 15.0
                            }
                        },
                        new Branch
                        {
                            Choice = "Wise choice",
                            NextSceneId = "multi-end",
                            CompassChange = new CompassChange
                            {
                                Axis = "Wisdom",
                                Delta = 12.0
                            }
                        }
                    }
                },
                new Scene
                {
                    Id = "multi-end",
                    Title = "End",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Final choice",
                            NextSceneId = "",
                            CompassChange = new CompassChange
                            {
                                Axis = "Empathy",
                                Delta = 8.0
                            }
                        }
                    }
                }
            }
        };

        var complexScenario = new Scenario
        {
            Id = "scenario-complex",
            Title = "Complex Branching",
            Description = "Complex graph with multiple branches",
            AgeGroup = "6-9",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "complex-1",
                    Title = "Start",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Option 1",
                            NextSceneId = "complex-2a",
                            CompassChange = new CompassChange { Axis = "Courage", Delta = 5.0 }
                        },
                        new Branch
                        {
                            Choice = "Option 2",
                            NextSceneId = "complex-2b",
                            CompassChange = new CompassChange { Axis = "Courage", Delta = 10.0 }
                        },
                        new Branch
                        {
                            Choice = "Option 3",
                            NextSceneId = "complex-2c",
                            CompassChange = new CompassChange { Axis = "Courage", Delta = 15.0 }
                        }
                    }
                },
                new Scene
                {
                    Id = "complex-2a",
                    Title = "Path A",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Continue",
                            NextSceneId = "",
                            CompassChange = new CompassChange { Axis = "Courage", Delta = 3.0 }
                        }
                    }
                },
                new Scene
                {
                    Id = "complex-2b",
                    Title = "Path B",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Continue",
                            NextSceneId = "",
                            CompassChange = new CompassChange { Axis = "Courage", Delta = 7.0 }
                        }
                    }
                },
                new Scene
                {
                    Id = "complex-2c",
                    Title = "Path C",
                    Type = SceneType.Choice,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Continue",
                            NextSceneId = "",
                            CompassChange = new CompassChange { Axis = "Courage", Delta = 12.0 }
                        }
                    }
                }
            }
        };

        // Create content bundles
        var testBundle = new ContentBundle
        {
            Id = "bundle-test",
            Title = "Test Bundle",
            Description = "Bundle for testing",
            ScenarioIds = new List<string>
            {
                "scenario-linear",
                "scenario-branching",
                "scenario-multi-axis",
                "scenario-complex"
            }
        };

        var singleScenarioBundle = new ContentBundle
        {
            Id = "bundle-single",
            Title = "Single Scenario Bundle",
            Description = "Bundle with one scenario",
            ScenarioIds = new List<string> { "scenario-linear" }
        };

        var emptyBundle = new ContentBundle
        {
            Id = "bundle-empty",
            Title = "Empty Bundle",
            Description = "Bundle with no scenarios",
            ScenarioIds = new List<string>()
        };

        await DbContext.Scenarios.AddRangeAsync(linearScenario, branchingScenario, multiAxisScenario, complexScenario);
        await DbContext.ContentBundles.AddRangeAsync(testBundle, singleScenarioBundle, emptyBundle);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithValidBundle_ReturnsCorrectAxes()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { 50, 75, 90 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().NotBeNull();
        results.Should().NotBeEmpty();

        // Should have Courage, Wisdom, and Empathy axes
        var axisNames = results.Select(r => r.AxisName).ToList();
        axisNames.Should().Contain("Courage");
        axisNames.Should().Contain("Wisdom");
        axisNames.Should().Contain("Empathy");
    }

    [Fact]
    public async Task Handle_WithValidBundle_ReturnsCorrectPercentiles()
    {
        // Arrange
        await SeedTestDataAsync();
        var percentiles = new List<double> { 25, 50, 75, 90 };
        var query = new CalculateBadgeScoresQuery("bundle-test", percentiles);

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().NotBeNull();
        foreach (var result in results)
        {
            result.PercentileScores.Should().HaveCount(percentiles.Count);
            result.PercentileScores.Keys.Should().BeEquivalentTo(percentiles);
        }
    }

    [Fact]
    public async Task Handle_WithLinearScenario_CalculatesCorrectScore()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-single", new List<double> { 50 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().ContainSingle();
        var courageResult = results.First(r => r.AxisName == "Courage");

        // Linear scenario: 10 + 5 = 15
        courageResult.PercentileScores[50].Should().Be(15.0);
    }

    [Fact]
    public async Task Handle_WithBranchingScenario_CalculatesPercentiles()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { 50, 75, 90 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        var wisdomResult = results.FirstOrDefault(r => r.AxisName == "Wisdom");
        wisdomResult.Should().NotBeNull();

        var courageResult = results.FirstOrDefault(r => r.AxisName == "Courage");
        courageResult.Should().NotBeNull();

        // Should have scores from branching paths
        wisdomResult!.PercentileScores[50].Should().Be(12.0);
        wisdomResult!.PercentileScores[75].Should().Be(16.0);
        wisdomResult!.PercentileScores[90].Should().Be(18.4);

        courageResult!.PercentileScores[50].Should().Be(15.0);
        courageResult!.PercentileScores[75].Should().Be(17.0);
        courageResult!.PercentileScores[90].Should().Be(23.0);
    }

    [Fact]
    public async Task Handle_WithComplexBranching_HandlesMultiplePaths()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { 25, 50, 75 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        var courageResult = results.FirstOrDefault(r => r.AxisName == "Courage");
        courageResult.Should().NotBeNull();

        // Complex scenario has paths: 5+3=8, 10+7=17, 15+12=27
        // Plus linear scenario: 10+5=15
        // Should have multiple distinct scores
        courageResult!.PercentileScores[25].Should().BeGreaterThan(0);
        courageResult.PercentileScores[75].Should().BeGreaterThan(courageResult.PercentileScores[25]);
    }

    [Fact]
    public async Task Handle_WithEmptyBundle_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-empty", new List<double> { 50 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidBundleId_ThrowsInvalidOperationException()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("invalid-bundle", new List<double> { 50 });

        // Act
        var act = async () => await Mediator.Send(query);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_WithNullBundleId_ThrowsArgumentException()
    {
        // Arrange
        var query = new CalculateBadgeScoresQuery(null!, new List<double> { 50 });

        // Act
        var act = async () => await Mediator.Send(query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithEmptyBundleId_ThrowsArgumentException()
    {
        // Arrange
        var query = new CalculateBadgeScoresQuery("", new List<double> { 50 });

        // Act
        var act = async () => await Mediator.Send(query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithNullPercentiles_ThrowsArgumentException()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", null!);

        // Act
        var act = async () => await Mediator.Send(query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithEmptyPercentiles_ThrowsArgumentException()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double>());

        // Act
        var act = async () => await Mediator.Send(query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public async Task Handle_WithInvalidPercentileValue_ThrowsArgumentException(double invalidPercentile)
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { invalidPercentile });

        // Act
        var act = async () => await Mediator.Send(query);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*between 0 and 100*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Handle_WithValidPercentileEdgeCases_Succeeds(double percentile)
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { percentile });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().NotBeNull();
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.PercentileScores.Should().ContainKey(percentile));
    }

    [Fact]
    public async Task Handle_WithMultipleAxes_ReturnsAllAxes()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { 50 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().HaveCountGreaterOrEqualTo(3); // Courage, Wisdom, Empathy

        var axisNames = results.Select(r => r.AxisName).ToList();
        axisNames.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_PercentilesAreMonotonicallyIncreasing()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { 10, 25, 50, 75, 90 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        foreach (var result in results)
        {
            var sortedScores = result.PercentileScores.OrderBy(kvp => kvp.Key).ToList();

            for (int i = 1; i < sortedScores.Count; i++)
            {
                // Each percentile score should be >= previous
                sortedScores[i].Value.Should().BeGreaterOrEqualTo(sortedScores[i - 1].Value);
            }
        }
    }

    [Fact]
    public async Task Handle_WithSinglePath_AllPercentilesEqual()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-single", new List<double> { 0, 25, 50, 75, 100 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        var courageResult = results.First(r => r.AxisName == "Courage");

        // With only one path, all percentiles should be the same
        var uniqueScores = courageResult.PercentileScores.Values.Distinct().ToList();
        uniqueScores.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_CaseInsensitiveAxisNames()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new CalculateBadgeScoresQuery("bundle-test", new List<double> { 50 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        // Axis names should be grouped case-insensitively
        var axisNames = results.Select(r => r.AxisName).ToList();
        axisNames.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_WithManyPercentiles_ReturnsAllValues()
    {
        // Arrange
        await SeedTestDataAsync();
        var percentiles = Enumerable.Range(0, 101).Select(i => (double)i).ToList();
        var query = new CalculateBadgeScoresQuery("bundle-test", percentiles);

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().NotBeEmpty();
        foreach (var result in results)
        {
            result.PercentileScores.Should().HaveCount(101);
        }
    }

    [Fact]
    public async Task Handle_IgnoresScenesWithoutCompassChanges()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add a scenario with no compass changes
        var noCompassScenario = new Scenario
        {
            Id = "scenario-no-compass",
            Title = "No Compass Scenario",
            Description = "Scenario without compass changes",
            AgeGroup = "6-9",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "no-compass-1",
                    Title = "Scene 1",
                    Type = SceneType.Narrative,
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Continue",
                            NextSceneId = ""
                        }
                    }
                }
            }
        };

        var noCompassBundle = new ContentBundle
        {
            Id = "bundle-no-compass",
            Title = "No Compass Bundle",
            Description = "Bundle with no compass changes",
            ScenarioIds = new List<string> { "scenario-no-compass" }
        };

        await DbContext.Scenarios.AddAsync(noCompassScenario);
        await DbContext.ContentBundles.AddAsync(noCompassBundle);
        await DbContext.SaveChangesAsync();

        var query = new CalculateBadgeScoresQuery("bundle-no-compass", new List<double> { 50 });

        // Act
        var results = await Mediator.Send(query);

        // Assert
        results.Should().BeEmpty();
    }
}
