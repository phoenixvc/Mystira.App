using FluentAssertions;
using Mystira.App.Application.Specifications;
using Mystira.Domain.Models;

namespace Mystira.App.Application.Tests.Specifications;

/// <summary>
/// Unit tests for GameSession specifications.
/// </summary>
public class GameSessionSpecificationsTests
{
    private readonly List<GameSession> _sessions;

    public GameSessionSpecificationsTests()
    {
        _sessions = new List<GameSession>
        {
            CreateSession("1", "account1", "profile1", "scenario1", SessionStatus.InProgress),
            CreateSession("2", "account1", "profile1", "scenario2", SessionStatus.Completed),
            CreateSession("3", "account1", "profile2", "scenario1", SessionStatus.Paused),
            CreateSession("4", "account2", "profile3", "scenario1", SessionStatus.InProgress),
            CreateSession("5", "account2", "profile3", "scenario3", SessionStatus.Abandoned),
        };
    }

    [Fact]
    public void SessionsByAccountSpec_ShouldFilterByAccountId()
    {
        // Arrange
        var spec = new SessionsByAccountSpec("account1");

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(s => s.AccountId == "account1");
    }

    [Fact]
    public void SessionsByProfileSpec_ShouldFilterByProfileId()
    {
        // Arrange
        var spec = new SessionsByProfileSpec("profile1");

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.ProfileId == "profile1");
    }

    [Fact]
    public void InProgressSessionsSpec_ShouldFilterActiveAndPausedForAccount()
    {
        // Arrange
        var spec = new InProgressSessionsSpec("account1");

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s =>
            s.AccountId == "account1" &&
            (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused));
    }

    [Fact]
    public void SessionsByScenarioSpec_ShouldFilterByScenarioId()
    {
        // Arrange
        var spec = new SessionsByScenarioSpec("scenario1");

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(s => s.ScenarioId == "scenario1");
    }

    [Fact]
    public void ActiveSessionsSpec_ShouldFilterInProgressAndPaused()
    {
        // Arrange
        var spec = new ActiveSessionsSpec();

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(s =>
            s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused);
    }

    [Fact]
    public void CompletedSessionsSpec_ShouldFilterCompletedOnly()
    {
        // Arrange
        var spec = new CompletedSessionsSpec();

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public void SessionsByStatusSpec_ShouldFilterByStatus()
    {
        // Arrange
        var spec = new SessionsByStatusSpec(SessionStatus.Abandoned);

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(SessionStatus.Abandoned);
    }

    [Fact]
    public void SessionsByAccountAndScenarioSpec_ShouldFilterByBoth()
    {
        // Arrange
        var spec = new SessionsByAccountAndScenarioSpec("account1", "scenario1");

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s =>
            s.AccountId == "account1" && s.ScenarioId == "scenario1");
    }

    [Fact]
    public void GameSessionByIdSpec_ShouldMatchById()
    {
        // Arrange
        var spec = new GameSessionByIdSpec("3");

        // Act
        var result = _sessions.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("3");
    }

    private static GameSession CreateSession(
        string id,
        string accountId,
        string profileId,
        string scenarioId,
        SessionStatus status)
    {
        return new GameSession
        {
            Id = id,
            AccountId = accountId,
            ProfileId = profileId,
            ScenarioId = scenarioId,
            Status = status,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = status == SessionStatus.Completed ? DateTime.UtcNow : null
        };
    }
}
