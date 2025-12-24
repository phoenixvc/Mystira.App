using FluentAssertions;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.Contracts.App.Models.GameSessions;
using Mystira.Contracts.App.Requests.GameSessions;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class GameSessionCommandTests : CqrsIntegrationTestBase
{
    #region StartGameSessionCommand Tests

    [Fact]
    public async Task StartGameSessionCommand_WithValidRequest_CreatesSession()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            PlayerNames = new List<string> { "Player1", "Player2" },
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.ScenarioId.Should().Be("scenario-1");
        result.AccountId.Should().Be("account-1");
        result.ProfileId.Should().Be("profile-1");
        result.PlayerNames.Should().BeEquivalentTo(new[] { "Player1", "Player2" });
        result.Status.Should().Be(SessionStatus.InProgress);
        result.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persistence
        var savedSession = await DbContext.GameSessions.FindAsync(result.Id);
        savedSession.Should().NotBeNull();
        savedSession!.ScenarioId.Should().Be("scenario-1");
    }

    [Fact]
    public async Task StartGameSessionCommand_WithCharacterAssignments_CreatesSessionWithAssignments()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            CharacterAssignments = new List<CharacterAssignmentDto>
            {
                new()
                {
                    CharacterId = "char-1",
                    CharacterName = "Hero",
                    Role = "protagonist",
                    Archetype = "courage",
                    PlayerAssignment = new PlayerAssignmentDto
                    {
                        Type = "Player",
                        ProfileId = "profile-1",
                        ProfileName = "TestPlayer"
                    }
                }
            },
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.CharacterAssignments.Should().HaveCount(1);
        result.CharacterAssignments[0].CharacterId.Should().Be("char-1");
        result.CharacterAssignments[0].CharacterName.Should().Be("Hero");
        result.CharacterAssignments[0].PlayerAssignment.Should().NotBeNull();
        result.CharacterAssignments[0].PlayerAssignment!.ProfileName.Should().Be("TestPlayer");
        result.PlayerNames.Should().Contain("TestPlayer");
    }

    [Fact]
    public async Task StartGameSessionCommand_WithMissingScenarioId_ThrowsArgumentException()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "",
            AccountId = "account-1",
            ProfileId = "profile-1",
            PlayerNames = new List<string> { "Player1" },
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task StartGameSessionCommand_WithMissingAccountId_ThrowsArgumentException()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "",
            ProfileId = "profile-1",
            PlayerNames = new List<string> { "Player1" },
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task StartGameSessionCommand_WithMissingProfileId_ThrowsArgumentException()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "",
            PlayerNames = new List<string> { "Player1" },
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task StartGameSessionCommand_WithNoPlayersOrAssignments_ThrowsArgumentException()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            PlayerNames = new List<string>(),
            CharacterAssignments = new List<CharacterAssignmentDto>(),
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await Mediator.Send(command);
        });
    }

    [Fact]
    public async Task StartGameSessionCommand_InitializesEmptyCollections()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            PlayerNames = new List<string> { "Player1" },
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.ChoiceHistory.Should().NotBeNull().And.BeEmpty();
        result.EchoHistory.Should().NotBeNull().And.BeEmpty();
        result.Achievements.Should().NotBeNull().And.BeEmpty();
        result.CompassValues.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task StartGameSessionCommand_SetsCorrectTargetAgeGroup()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            PlayerNames = new List<string> { "Player1" },
            TargetAgeGroup = "10-12"
        };
        var command = new StartGameSessionCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.TargetAgeGroup.Should().NotBeNull();
        result.TargetAgeGroup.MinimumAge.Should().Be(10);
        result.TargetAgeGroup.MaximumAge.Should().Be(12);
    }

    [Fact]
    public async Task StartGameSessionCommand_WithGuestPlayer_CreatesSessionWithGuestAssignment()
    {
        // Arrange
        var request = new StartGameSessionRequest
        {
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            CharacterAssignments = new List<CharacterAssignmentDto>
            {
                new()
                {
                    CharacterId = "char-1",
                    CharacterName = "Hero",
                    Role = "protagonist",
                    Archetype = "courage",
                    PlayerAssignment = new PlayerAssignmentDto
                    {
                        Type = "Guest",
                        GuestName = "GuestPlayer",
                        GuestAgeRange = "6-9"
                    }
                }
            },
            TargetAgeGroup = "6-9"
        };
        var command = new StartGameSessionCommand(request);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.CharacterAssignments.Should().HaveCount(1);
        result.CharacterAssignments[0].PlayerAssignment!.Type.Should().Be("Guest");
        result.CharacterAssignments[0].PlayerAssignment!.GuestName.Should().Be("GuestPlayer");
        result.PlayerNames.Should().Contain("GuestPlayer");
    }

    #endregion

    #region PauseGameSessionCommand Tests

    [Fact]
    public async Task PauseGameSessionCommand_WithActiveSession_PausesSession()
    {
        // Arrange
        var session = new GameSession
        {
            Id = "session-to-pause",
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            PlayerNames = new List<string> { "Player1" }
        };
        DbContext.GameSessions.Add(session);
        await DbContext.SaveChangesAsync();

        var command = new PauseGameSessionCommand("session-to-pause");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.IsPaused.Should().BeTrue();
        result.Status.Should().Be(SessionStatus.Paused);
        result.PausedAt.Should().NotBeNull();
        result.PausedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PauseGameSessionCommand_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var command = new PauseGameSessionCommand("non-existent-session");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PauseGameSessionCommand_WithCompletedSession_ReturnsNull()
    {
        // Arrange
        var session = new GameSession
        {
            Id = "completed-session",
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            Status = SessionStatus.Completed,
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow,
            PlayerNames = new List<string> { "Player1" }
        };
        DbContext.GameSessions.Add(session);
        await DbContext.SaveChangesAsync();

        var command = new PauseGameSessionCommand("completed-session");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ResumeGameSessionCommand Tests

    [Fact]
    public async Task ResumeGameSessionCommand_WithPausedSession_ResumesSession()
    {
        // Arrange
        var session = new GameSession
        {
            Id = "session-to-resume",
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            Status = SessionStatus.Paused,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            IsPaused = true,
            PausedAt = DateTime.UtcNow.AddMinutes(-5),
            PlayerNames = new List<string> { "Player1" }
        };
        DbContext.GameSessions.Add(session);
        await DbContext.SaveChangesAsync();

        var command = new ResumeGameSessionCommand("session-to-resume");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.IsPaused.Should().BeFalse();
        result.Status.Should().Be(SessionStatus.InProgress);
        result.PausedAt.Should().BeNull();
    }

    [Fact]
    public async Task ResumeGameSessionCommand_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var command = new ResumeGameSessionCommand("non-existent-session");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region EndGameSessionCommand Tests

    [Fact]
    public async Task EndGameSessionCommand_WithActiveSession_EndsSession()
    {
        // Arrange
        var session = new GameSession
        {
            Id = "session-to-end",
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            PlayerNames = new List<string> { "Player1" }
        };
        DbContext.GameSessions.Add(session);
        await DbContext.SaveChangesAsync();

        var command = new EndGameSessionCommand("session-to-end");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SessionStatus.Completed);
        result.EndTime.Should().NotBeNull();
        result.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EndGameSessionCommand_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var command = new EndGameSessionCommand("non-existent-session");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EndGameSessionCommand_WithPausedSession_EndsSession()
    {
        // Arrange
        var session = new GameSession
        {
            Id = "paused-session-to-end",
            ScenarioId = "scenario-1",
            AccountId = "account-1",
            ProfileId = "profile-1",
            Status = SessionStatus.Paused,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            IsPaused = true,
            PausedAt = DateTime.UtcNow.AddMinutes(-5),
            PlayerNames = new List<string> { "Player1" }
        };
        DbContext.GameSessions.Add(session);
        await DbContext.SaveChangesAsync();

        var command = new EndGameSessionCommand("paused-session-to-end");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SessionStatus.Completed);
        result.EndTime.Should().NotBeNull();
    }

    #endregion
}
