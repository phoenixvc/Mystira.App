using AutoFixture;
using AutoFixture.Xunit2;
using DMfinity.Domain.Models;
using FluentAssertions;
using Xunit;

namespace DMfinity.Domain.Tests.Models;

public class GameSessionTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void GameSession_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var gameSession = new GameSession();

        // Assert
        gameSession.Id.Should().NotBeEmpty();
        gameSession.ScenarioId.Should().BeEmpty();
        gameSession.AccountId.Should().BeEmpty();
        gameSession.PlayerNames.Should().NotBeNull().And.BeEmpty();
        gameSession.Status.Should().Be(SessionStatus.NotStarted);
        gameSession.CurrentSceneId.Should().BeEmpty();
        gameSession.ChoiceHistory.Should().NotBeNull().And.BeEmpty();
        gameSession.EchoHistory.Should().NotBeNull().And.BeEmpty();
        gameSession.CompassValues.Should().NotBeNull().And.BeEmpty();
        gameSession.Achievements.Should().NotBeNull().And.BeEmpty();
        gameSession.IsPaused.Should().BeFalse();
        gameSession.SceneCount.Should().Be(0);
    }

    [Theory]
    [AutoData]
    public void GameSession_SetProperties_SetsValuesCorrectly(
        string scenarioId, 
        string accountId, 
        List<string> playerNames, 
        string currentSceneId)
    {
        // Arrange
        var gameSession = new GameSession();

        // Act
        gameSession.ScenarioId = scenarioId;
        gameSession.AccountId = accountId;
        gameSession.PlayerNames = playerNames;
        gameSession.CurrentSceneId = currentSceneId;
        gameSession.Status = SessionStatus.InProgress;

        // Assert
        gameSession.ScenarioId.Should().Be(scenarioId);
        gameSession.AccountId.Should().Be(accountId);
        gameSession.PlayerNames.Should().BeEquivalentTo(playerNames);
        gameSession.CurrentSceneId.Should().Be(currentSceneId);
        gameSession.Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public void GameSession_AddChoice_AddsToChoiceHistory()
    {
        // Arrange
        var gameSession = new GameSession();
        var choice = _fixture.Create<SessionChoice>();

        // Act
        gameSession.ChoiceHistory.Add(choice);

        // Assert
        gameSession.ChoiceHistory.Should().HaveCount(1);
        gameSession.ChoiceHistory.First().Should().Be(choice);
    }

    [Fact]
    public void GameSession_AddEcho_AddsToEchoHistory()
    {
        // Arrange
        var gameSession = new GameSession();
        var echo = _fixture.Create<EchoLog>();

        // Act
        gameSession.EchoHistory.Add(echo);

        // Assert
        gameSession.EchoHistory.Should().HaveCount(1);
        gameSession.EchoHistory.First().Should().Be(echo);
    }

    [Fact]
    public void GameSession_AddAchievement_AddsToAchievements()
    {
        // Arrange
        var gameSession = new GameSession();
        var achievement = _fixture.Create<SessionAchievement>();

        // Act
        gameSession.Achievements.Add(achievement);

        // Assert
        gameSession.Achievements.Should().HaveCount(1);
        gameSession.Achievements.First().Should().Be(achievement);
    }

    [Theory]
    [InlineData(SessionStatus.NotStarted)]
    [InlineData(SessionStatus.InProgress)]
    [InlineData(SessionStatus.Paused)]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Abandoned)]
    public void GameSession_SetStatus_SetsStatusCorrectly(SessionStatus status)
    {
        // Arrange
        var gameSession = new GameSession();

        // Act
        gameSession.Status = status;

        // Assert
        gameSession.Status.Should().Be(status);
    }
}

public class SessionChoiceTests
{
    [Fact]
    public void SessionChoice_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var choice = new SessionChoice();

        // Assert
        choice.SceneId.Should().BeEmpty();
        choice.SceneTitle.Should().BeEmpty();
        choice.ChoiceText.Should().BeEmpty();
        choice.NextScene.Should().BeEmpty();
        choice.ChosenAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        choice.EchoGenerated.Should().BeNull();
        choice.CompassChange.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void SessionChoice_SetProperties_SetsValuesCorrectly(
        string sceneId,
        string sceneTitle,
        string choiceText,
        string nextScene)
    {
        // Arrange
        var choice = new SessionChoice();
        var testTime = DateTime.UtcNow;

        // Act
        choice.SceneId = sceneId;
        choice.SceneTitle = sceneTitle;
        choice.ChoiceText = choiceText;
        choice.NextScene = nextScene;
        choice.ChosenAt = testTime;

        // Assert
        choice.SceneId.Should().Be(sceneId);
        choice.SceneTitle.Should().Be(sceneTitle);
        choice.ChoiceText.Should().Be(choiceText);
        choice.NextScene.Should().Be(nextScene);
        choice.ChosenAt.Should().Be(testTime);
    }
}

public class SessionAchievementTests
{
    [Fact]
    public void SessionAchievement_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var achievement = new SessionAchievement();

        // Assert
        achievement.Id.Should().NotBeEmpty();
        achievement.Title.Should().BeEmpty();
        achievement.Description.Should().BeEmpty();
        achievement.IconName.Should().BeEmpty();
        achievement.Type.Should().Be(default(AchievementType));
        achievement.CompassAxis.Should().BeEmpty();
        achievement.ThresholdValue.Should().Be(0);
        achievement.EarnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        achievement.IsVisible.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void SessionAchievement_SetProperties_SetsValuesCorrectly(
        string title,
        string description,
        string iconName,
        string compassAxis,
        float thresholdValue)
    {
        // Arrange
        var achievement = new SessionAchievement();
        var testTime = DateTime.UtcNow;

        // Act
        achievement.Title = title;
        achievement.Description = description;
        achievement.IconName = iconName;
        achievement.Type = AchievementType.CompassThreshold;
        achievement.CompassAxis = compassAxis;
        achievement.ThresholdValue = thresholdValue;
        achievement.EarnedAt = testTime;
        achievement.IsVisible = false;

        // Assert
        achievement.Title.Should().Be(title);
        achievement.Description.Should().Be(description);
        achievement.IconName.Should().Be(iconName);
        achievement.Type.Should().Be(AchievementType.CompassThreshold);
        achievement.CompassAxis.Should().Be(compassAxis);
        achievement.ThresholdValue.Should().Be(thresholdValue);
        achievement.EarnedAt.Should().Be(testTime);
        achievement.IsVisible.Should().BeFalse();
    }

    [Theory]
    [InlineData(AchievementType.CompassThreshold)]
    [InlineData(AchievementType.FirstChoice)]
    [InlineData(AchievementType.SessionComplete)]
    [InlineData(AchievementType.EchoRevealed)]
    [InlineData(AchievementType.ConsistentChoice)]
    [InlineData(AchievementType.MoralGrowth)]
    public void SessionAchievement_SetType_SetsTypeCorrectly(AchievementType type)
    {
        // Arrange
        var achievement = new SessionAchievement();

        // Act
        achievement.Type = type;

        // Assert
        achievement.Type.Should().Be(type);
    }
}