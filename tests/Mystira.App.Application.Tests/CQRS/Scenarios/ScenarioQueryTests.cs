using FluentAssertions;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Scenarios;

public class ScenarioQueryTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var scenarios = new List<Scenario>
        {
            new()
            {
                Id = "scenario-1",
                Title = "The Dragon's Quest",
                Description = "An adventure about bravery",
                AgeGroup = "6-9",
                MinimumAge = 6,
                IsActive = true,
                IsFeatured = true,
                Difficulty = DifficultyLevel.Easy,
                SessionLength = SessionLength.Short,
                Tags = new List<string> { "adventure", "fantasy" },
                Scenes = new List<Scene>
                {
                    new() { Id = "scene-1", Title = "The Beginning", Type = SceneType.Narrative }
                }
            },
            new()
            {
                Id = "scenario-2",
                Title = "Ocean Mysteries",
                Description = "Discover underwater secrets",
                AgeGroup = "6-9",
                MinimumAge = 6,
                IsActive = true,
                IsFeatured = false,
                Difficulty = DifficultyLevel.Medium,
                SessionLength = SessionLength.Medium,
                Tags = new List<string> { "exploration", "ocean" },
                Scenes = new List<Scene>
                {
                    new() { Id = "scene-1", Title = "The Dive", Type = SceneType.Narrative }
                }
            },
            new()
            {
                Id = "scenario-3",
                Title = "Space Explorers",
                Description = "Journey through the stars",
                AgeGroup = "10-12",
                MinimumAge = 10,
                IsActive = true,
                IsFeatured = true,
                Difficulty = DifficultyLevel.Hard,
                SessionLength = SessionLength.Long,
                Tags = new List<string> { "science", "space" },
                Scenes = new List<Scene>
                {
                    new() { Id = "scene-1", Title = "Launch", Type = SceneType.Narrative }
                }
            },
            new()
            {
                Id = "scenario-inactive",
                Title = "Inactive Story",
                Description = "This story is not active",
                AgeGroup = "6-9",
                MinimumAge = 6,
                IsActive = false,
                IsFeatured = false,
                Difficulty = DifficultyLevel.Easy,
                SessionLength = SessionLength.Short,
                Scenes = new List<Scene>
                {
                    new() { Id = "scene-1", Title = "Hidden", Type = SceneType.Narrative }
                }
            }
        };

        DbContext.Scenarios.AddRange(scenarios);
        await DbContext.SaveChangesAsync();
    }

    #region GetScenarioQuery Tests

    [Fact]
    public async Task GetScenarioQuery_WithExistingId_ReturnsScenario()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenarioQuery("scenario-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("scenario-1");
        result.Title.Should().Be("The Dragon's Quest");
        result.Description.Should().Contain("bravery");
        result.AgeGroup.Should().Be("6-9");
        result.IsActive.Should().BeTrue();
        result.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public async Task GetScenarioQuery_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenarioQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetScenarioQuery_ReturnsScenarioWithScenes()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenarioQuery("scenario-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Scenes.Should().NotBeEmpty();
        result.Scenes.Should().HaveCount(1);
        result.Scenes[0].Title.Should().Be("The Beginning");
    }

    [Fact]
    public Task GetScenarioQuery_HasCorrectCacheKey()
    {
        // Arrange
        var query = new GetScenarioQuery("scenario-123");

        // Assert
        query.CacheKey.Should().Be("Scenario:scenario-123");
        query.CacheDurationSeconds.Should().Be(300);
        return Task.CompletedTask;
    }

    #endregion

    #region GetScenariosQuery Tests

    [Fact]
    public async Task GetScenariosQuery_ReturnsAllScenarios()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenariosQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4); // All scenarios including inactive
    }

    #endregion

    #region GetFeaturedScenariosQuery Tests

    [Fact]
    public async Task GetFeaturedScenariosQuery_ReturnsOnlyFeaturedScenarios()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetFeaturedScenariosQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.IsFeatured);
    }

    #endregion

    #region GetScenariosByAgeGroupQuery Tests

    [Fact]
    public async Task GetScenariosByAgeGroupQuery_ReturnsMatchingScenarios()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenariosByAgeGroupQuery("6-9");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().OnlyContain(s => s.AgeGroup == "6-9");
    }

    [Fact]
    public async Task GetScenariosByAgeGroupQuery_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetScenariosByAgeGroupQuery("1-2");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Scenario Validation Tests

    [Fact]
    public void Scenario_Validate_WithValidScenario_ReturnsTrue()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "valid-scenario",
            Title = "Valid Story",
            Scenes = new List<Scene>
            {
                new() { Id = "scene-1", Title = "Scene 1", Type = SceneType.Narrative }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Scenario_Validate_WithEmptyTitle_ReturnsFalse()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "invalid-scenario",
            Title = "",
            Scenes = new List<Scene>
            {
                new() { Id = "scene-1", Title = "Scene 1" }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("title"));
    }

    [Fact]
    public void Scenario_Validate_WithNoScenes_ReturnsFalse()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "no-scenes",
            Title = "Story Without Scenes",
            Scenes = new List<Scene>()
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("at least one scene"));
    }

    [Fact]
    public void Scenario_Validate_WithInvalidNextSceneId_ReturnsFalse()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "invalid-links",
            Title = "Story With Bad Links",
            Scenes = new List<Scene>
            {
                new()
                {
                    Id = "scene-1",
                    Title = "Scene 1",
                    NextSceneId = "non-existent-scene"
                }
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("invalid NextSceneId"));
    }

    [Fact]
    public void Scenario_Validate_WithValidSceneChain_ReturnsTrue()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "valid-chain",
            Title = "Story With Chain",
            Scenes = new List<Scene>
            {
                new() { Id = "scene-1", Title = "Scene 1", NextSceneId = "scene-2" },
                new() { Id = "scene-2", Title = "Scene 2", NextSceneId = "scene-3" },
                new() { Id = "scene-3", Title = "Scene 3", NextSceneId = null } // Ending
            }
        };

        // Act
        var isValid = scenario.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    #endregion
}
