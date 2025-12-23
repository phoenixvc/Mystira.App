using FluentAssertions;
using Mystira.App.Application.Specifications;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.Specifications;

/// <summary>
/// Unit tests for Scenario specifications.
/// Tests verify that specifications correctly filter entities.
/// </summary>
public class ScenarioSpecificationsTests
{
    private readonly List<Scenario> _scenarios;

    public ScenarioSpecificationsTests()
    {
        _scenarios = new List<Scenario>
        {
            CreateScenario("1", "Dragon Quest", "children", DifficultyLevel.Easy, new[] { "fantasy", "featured" }, true),
            CreateScenario("2", "Space Adventure", "teens", DifficultyLevel.Medium, new[] { "scifi" }, true),
            CreateScenario("3", "Mystery Manor", "adults", DifficultyLevel.Hard, new[] { "mystery", "featured" }, true),
            CreateScenario("4", "Pirate Treasure", "children", DifficultyLevel.Easy, new[] { "adventure" }, true),
            CreateScenario("5", "Zombie Apocalypse", "teens", DifficultyLevel.Hard, new[] { "horror" }, false),
            CreateScenario("6", "Dragon's Lair", "children", DifficultyLevel.Medium, new[] { "fantasy" }, true),
        };
    }

    [Fact]
    public void ScenarioByIdSpec_ShouldMatchExactId()
    {
        // Arrange
        var spec = new ScenarioByIdSpec("1");

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("1");
        result.First().Title.Should().Be("Dragon Quest");
    }

    [Fact]
    public void ScenarioByIdSpec_ShouldReturnEmpty_WhenNoMatch()
    {
        // Arrange
        var spec = new ScenarioByIdSpec("nonexistent");

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ScenariosByAgeGroupSpec_ShouldFilterByAgeGroup()
    {
        // Arrange
        var spec = new ScenariosByAgeGroupSpec("children");

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(s => s.AgeGroup.Should().Be("children"));
    }

    [Fact]
    public void ScenariosByTagSpec_ShouldFilterByTag()
    {
        // Arrange
        var spec = new ScenariosByTagSpec("fantasy");

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.Tags.Should().Contain("fantasy"));
    }

    [Fact]
    public void ScenariosByDifficultySpec_ShouldFilterByDifficulty()
    {
        // Arrange
        var spec = new ScenariosByDifficultySpec(DifficultyLevel.Hard);

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.Difficulty.Should().Be(DifficultyLevel.Hard));
    }

    [Fact]
    public void FeaturedScenariosSpec_ShouldFilterFeaturedOnly()
    {
        // Arrange
        var spec = new FeaturedScenariosSpec();

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.Tags.Should().Contain("featured"));
    }

    [Fact]
    public void ScenariosByTitlePatternSpec_ShouldMatchPartialTitle()
    {
        // Arrange
        var spec = new ScenariosByTitlePatternSpec("dragon");

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.Title.ToLower().Should().Contain("dragon"));
    }

    [Fact]
    public void ScenariosByTitlePatternSpec_ShouldBeCaseInsensitive()
    {
        // Arrange
        var spec = new ScenariosByTitlePatternSpec("DRAGON");

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void PublishedScenariosSpec_ShouldFilterActiveOnly()
    {
        // Arrange
        var spec = new PublishedScenariosSpec();

        // Act
        var result = _scenarios.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
    }

    [Fact]
    public void ScenariosByArchetypeSpec_ShouldFilterByArchetype()
    {
        // Arrange
        var scenariosWithArchetypes = new List<Scenario>
        {
            CreateScenarioWithArchetypes("1", "Quest", new[] { "warrior", "mage" }),
            CreateScenarioWithArchetypes("2", "Adventure", new[] { "rogue" }),
            CreateScenarioWithArchetypes("3", "Battle", new[] { "warrior" }),
        };
        var spec = new ScenariosByArchetypeSpec("warrior");

        // Act
        var result = scenariosWithArchetypes.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    private static Scenario CreateScenario(
        string id,
        string title,
        string ageGroup,
        DifficultyLevel difficulty,
        string[] tags,
        bool isActive)
    {
        return new Scenario
        {
            Id = id,
            Title = title,
            AgeGroup = ageGroup,
            Difficulty = difficulty,
            Tags = tags.ToList(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Scenario CreateScenarioWithArchetypes(string id, string title, string[] archetypes)
    {
        return new Scenario
        {
            Id = id,
            Title = title,
            AgeGroup = "children",
            Difficulty = DifficultyLevel.Easy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Archetypes = archetypes.Select((a, i) => new KeyValuePair<int, string>(i, a)).ToDictionary(x => x.Key, x => x.Value)
        };
    }
}
