using DMfinity.Domain.Models;
using FluentAssertions;
using Xunit;
using AutoFixture.Xunit2;

namespace DMfinity.Domain.Tests.Models;

public class GameSessionTests
{
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
        // Act
        var gameSession = new GameSession
        {
            ScenarioId = scenarioId,
            AccountId = accountId,
            PlayerNames = playerNames,
            CurrentSceneId = currentSceneId,
            Status = SessionStatus.InProgress
        };

        // Assert
        gameSession.ScenarioId.Should().Be(scenarioId);
        gameSession.AccountId.Should().Be(accountId);
        gameSession.PlayerNames.Should().BeEquivalentTo(playerNames);
        gameSession.CurrentSceneId.Should().Be(currentSceneId);
        gameSession.Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public void GetTotalElapsedTime_ReturnsCorrectTime_WhenInProgress()
    {
        // Arrange
        var session = new GameSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            Status = SessionStatus.InProgress
        };

        // Act
        var elapsedTime = session.GetTotalElapsedTime();

        // Assert
        elapsedTime.Should().BeCloseTo(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetTotalElapsedTime_ReturnsCorrectTime_WhenPaused()
    {
        // Arrange
        var session = new GameSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            Status = SessionStatus.InProgress,
            IsPaused = true,
            PausedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        var elapsedTime = session.GetTotalElapsedTime();

        // Assert
        elapsedTime.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetTotalElapsedTime_ReturnsCorrectTime_WhenFinished()
    {
        // Arrange
        var session = new GameSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            Status = SessionStatus.Completed,
            ElapsedTime = TimeSpan.FromMinutes(10)
        };

        // Act
        var elapsedTime = session.GetTotalElapsedTime();

        // Assert
        elapsedTime.Should().Be(TimeSpan.FromMinutes(10));
    }
}