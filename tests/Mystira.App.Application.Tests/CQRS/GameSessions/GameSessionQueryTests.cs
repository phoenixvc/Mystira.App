using FluentAssertions;
using Mystira.App.Application.CQRS.GameSessions.Queries;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class GameSessionQueryTests : CqrsIntegrationTestBase
{
    protected override async Task SeedTestDataAsync()
    {
        var sessions = new List<GameSession>
        {
            new()
            {
                Id = "session-1",
                ScenarioId = "scenario-1",
                AccountId = "account-1",
                ProfileId = "profile-1",
                PlayerNames = new List<string> { "Player1" },
                Status = SessionStatus.Completed,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1),
                SceneCount = 5
            },
            new()
            {
                Id = "session-2",
                ScenarioId = "scenario-2",
                AccountId = "account-1",
                ProfileId = "profile-1",
                PlayerNames = new List<string> { "Player1", "Player2" },
                Status = SessionStatus.InProgress,
                StartTime = DateTime.UtcNow.AddMinutes(-30),
                CurrentSceneId = "scene-2-1",
                SceneCount = 3
            },
            new()
            {
                Id = "session-3",
                ScenarioId = "scenario-1",
                AccountId = "account-1",
                ProfileId = "profile-2",
                PlayerNames = new List<string> { "Player3" },
                Status = SessionStatus.Paused,
                StartTime = DateTime.UtcNow.AddMinutes(-45),
                CurrentSceneId = "scene-1-1",
                IsPaused = true,
                PausedAt = DateTime.UtcNow.AddMinutes(-15),
                SceneCount = 2
            },
            new()
            {
                Id = "session-4",
                ScenarioId = "scenario-3",
                AccountId = "account-2",
                ProfileId = "profile-3",
                PlayerNames = new List<string> { "OtherPlayer" },
                Status = SessionStatus.InProgress,
                StartTime = DateTime.UtcNow.AddMinutes(-20),
                CurrentSceneId = "scene-3-1",
                SceneCount = 1
            }
        };

        DbContext.GameSessions.AddRange(sessions);
        await DbContext.SaveChangesAsync();
    }

    #region GetGameSessionQuery Tests

    [Fact]
    public async Task GetGameSessionQuery_WithExistingSession_ReturnsSession()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetGameSessionQuery("session-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("session-1");
        result.ScenarioId.Should().Be("scenario-1");
        result.AccountId.Should().Be("account-1");
        result.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task GetGameSessionQuery_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetGameSessionQuery("non-existent");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGameSessionQuery_WithInProgressSession_ReturnsCorrectStatus()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetGameSessionQuery("session-2");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SessionStatus.InProgress);
        result.EndTime.Should().BeNull();
    }

    [Fact]
    public async Task GetGameSessionQuery_WithPausedSession_ReturnsPausedStatus()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetGameSessionQuery("session-3");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SessionStatus.Paused);
        result.IsPaused.Should().BeTrue();
        result.PausedAt.Should().NotBeNull();
    }

    #endregion

    #region GetSessionsByAccountQuery Tests

    [Fact]
    public async Task GetSessionsByAccountQuery_WithExistingSessions_ReturnsAllSessionsForAccount()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetSessionsByAccountQuery("account-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(s => s.AccountId.Should().Be("account-1"));
    }

    [Fact]
    public async Task GetSessionsByAccountQuery_WithNoSessions_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetSessionsByAccountQuery("non-existent-account");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSessionsByAccountQuery_ReturnsSessionsWithCorrectData()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetSessionsByAccountQuery("account-2");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().HaveCount(1);
        var session = result.First();
        session.Id.Should().Be("session-4");
        session.ScenarioId.Should().Be("scenario-3");
        session.ProfileId.Should().Be("profile-3");
        session.Status.Should().Be(SessionStatus.InProgress);
    }

    #endregion

    #region GetSessionsByProfileQuery Tests

    [Fact]
    public async Task GetSessionsByProfileQuery_WithExistingSessions_ReturnsAllSessionsForProfile()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetSessionsByProfileQuery("profile-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.ProfileId.Should().Be("profile-1"));
    }

    [Fact]
    public async Task GetSessionsByProfileQuery_WithNoSessions_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetSessionsByProfileQuery("non-existent-profile");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSessionsByProfileQuery_ReturnsMixedStatusSessions()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetSessionsByProfileQuery("profile-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Status == SessionStatus.Completed);
        result.Should().Contain(s => s.Status == SessionStatus.InProgress);
    }

    #endregion

    #region GetInProgressSessionsQuery Tests

    [Fact]
    public async Task GetInProgressSessionsQuery_ReturnsInProgressAndPausedSessions()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = new GetInProgressSessionsQuery("account-1");

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        // Should return session-2 (InProgress) and session-3 (Paused) but not session-1 (Completed)
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Paused);
    }

    #endregion
}
