using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Mystira.App.Domain.Models;
using Xunit;

namespace DMfinity.Domain.Tests.Models;

public class ScenarioTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Scenario_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var scenario = new Scenario();

        // Assert
        scenario.Id.Should().BeEmpty();
        scenario.Title.Should().BeEmpty();
        scenario.Description.Should().BeEmpty();
        scenario.Tags.Should().NotBeNull().And.BeEmpty();
        scenario.Difficulty.Should().Be(default(DifficultyLevel));
        scenario.SessionLength.Should().Be(default(SessionLength));
        scenario.Archetypes.Should().NotBeNull().And.BeEmpty();
        scenario.AgeGroup.Should().BeEmpty();
        scenario.MinimumAge.Should().Be(0);
        scenario.CoreAxes.Should().NotBeNull().And.BeEmpty();
        scenario.Characters.Should().NotBeNull().And.BeEmpty();
        scenario.Scenes.Should().NotBeNull().And.BeEmpty();
        scenario.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [AutoData]
    public void Scenario_SetProperties_SetsValuesCorrectly(
        string id,
        string title,
        string description,
        List<string> tags,
        List<string> archetypes,
        string ageGroup,
        int minimumAge,
        List<string> coreAxes)
    {
        // Arrange
        var scenario = new Scenario();

        // Act
        scenario.Id = id;
        scenario.Title = title;
        scenario.Description = description;
        scenario.Tags = tags;
        scenario.Difficulty = DifficultyLevel.Medium;
        scenario.SessionLength = SessionLength.Medium;
        scenario.Archetypes = archetypes;
        scenario.AgeGroup = ageGroup;
        scenario.MinimumAge = minimumAge;
        scenario.CoreAxes = coreAxes;

        // Assert
        scenario.Id.Should().Be(id);
        scenario.Title.Should().Be(title);
        scenario.Description.Should().Be(description);
        scenario.Tags.Should().BeEquivalentTo(tags);
        scenario.Difficulty.Should().Be(DifficultyLevel.Medium);
        scenario.SessionLength.Should().Be(SessionLength.Medium);
        scenario.Archetypes.Should().BeEquivalentTo(archetypes);
        scenario.AgeGroup.Should().Be(ageGroup);
        scenario.MinimumAge.Should().Be(minimumAge);
        scenario.CoreAxes.Should().BeEquivalentTo(coreAxes);
    }

    [Theory]
    [InlineData(DifficultyLevel.Easy)]
    [InlineData(DifficultyLevel.Medium)]
    [InlineData(DifficultyLevel.Hard)]
    public void Scenario_SetDifficulty_SetsCorrectly(DifficultyLevel difficulty)
    {
        // Arrange
        var scenario = new Scenario();

        // Act
        scenario.Difficulty = difficulty;

        // Assert
        scenario.Difficulty.Should().Be(difficulty);
    }

    [Theory]
    [InlineData(SessionLength.Short)]
    [InlineData(SessionLength.Medium)]
    [InlineData(SessionLength.Long)]
    public void Scenario_SetSessionLength_SetsCorrectly(SessionLength sessionLength)
    {
        // Arrange
        var scenario = new Scenario();

        // Act
        scenario.SessionLength = sessionLength;

        // Assert
        scenario.SessionLength.Should().Be(sessionLength);
    }

    [Fact]
    public void Scenario_AddScene_AddsToScenes()
    {
        // Arrange
        var scenario = new Scenario();
        var scene = _fixture.Create<Scene>();

        // Act
        scenario.Scenes.Add(scene);

        // Assert
        scenario.Scenes.Should().HaveCount(1);
        scenario.Scenes.First().Should().Be(scene);
    }

    [Fact]
    public void Scenario_CoreAxes_ShouldNotExceedFour()
    {
        // Arrange
        var scenario = new Scenario();
        var coreAxes = new List<string> { "honesty", "bravery", "kindness", "justice" };

        // Act
        scenario.CoreAxes = coreAxes;

        // Assert
        scenario.CoreAxes.Should().HaveCount(4);
        scenario.CoreAxes.Should().BeEquivalentTo(coreAxes);
    }
}

public class SceneTests
{
    [Fact]
    public void Scene_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var scene = new Scene();

        // Assert
        scene.Id.Should().BeEmpty();
        scene.Title.Should().BeEmpty();
        scene.Type.Should().Be(default(SceneType));
        scene.Description.Should().BeEmpty();
        scene.Media.Should().BeNull();
        scene.Branches.Should().NotBeNull().And.BeEmpty();
        scene.EchoReveals.Should().NotBeNull().And.BeEmpty();
        scene.Difficulty.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void Scene_SetProperties_SetsValuesCorrectly(
        string id,
        string title,
        string description,
        string nextScene)
    {
        // Arrange
        var scene = new Scene();

        // Act
        scene.Id = id;
        scene.Title = title;
        scene.Type = SceneType.Choice;
        scene.Description = description;
        scene.NextScene = nextScene;
        scene.Difficulty = 10;

        // Assert
        scene.Id.Should().Be(id);
        scene.Title.Should().Be(title);
        scene.Type.Should().Be(SceneType.Choice);
        scene.Description.Should().Be(description);
        scene.NextScene.Should().Be(nextScene);
        scene.Difficulty.Should().Be(10);
    }

    [Theory]
    [InlineData(SceneType.Narrative)]
    [InlineData(SceneType.Choice)]
    [InlineData(SceneType.Roll)]
    [InlineData(SceneType.Special)]
    public void Scene_SetType_SetsCorrectly(SceneType sceneType)
    {
        // Arrange
        var scene = new Scene();

        // Act
        scene.Type = sceneType;

        // Assert
        scene.Type.Should().Be(sceneType);
    }
}

public class BranchTests
{
    [Fact]
    public void Branch_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var branch = new Branch();

        // Assert
        branch.Choice.Should().BeEmpty();
        branch.NextScene.Should().BeEmpty();
        branch.EchoLog.Should().BeNull();
        branch.CompassChange.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void Branch_SetProperties_SetsValuesCorrectly(
        string choice,
        string nextScene)
    {
        // Arrange
        var branch = new Branch();

        // Act
        branch.Choice = choice;
        branch.NextScene = nextScene;

        // Assert
        branch.Choice.Should().Be(choice);
        branch.NextScene.Should().Be(nextScene);
    }
}

public class EchoLogTests
{
    [Fact]
    public void EchoLog_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var echoLog = new EchoLog();

        // Assert
        echoLog.EchoType.Should().BeEmpty();
        echoLog.Description.Should().BeEmpty();
        echoLog.Strength.Should().Be(0);
    }

    [Theory]
    [AutoData]
    public void EchoLog_SetProperties_SetsValuesCorrectly(
        string echoType,
        string description,
        double strength)
    {
        // Arrange
        var echoLog = new EchoLog();

        // Act
        echoLog.EchoType = echoType;
        echoLog.Description = description;
        echoLog.Strength = strength;

        // Assert
        echoLog.EchoType.Should().Be(echoType);
        echoLog.Description.Should().Be(description);
        echoLog.Strength.Should().Be(strength);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void EchoLog_SetStrength_WithinValidRange_SetsCorrectly(double strength)
    {
        // Arrange
        var echoLog = new EchoLog();

        // Act
        echoLog.Strength = strength;

        // Assert
        echoLog.Strength.Should().Be(strength);
        echoLog.Strength.Should().BeInRange(-1.0, 1.0);
    }
}
