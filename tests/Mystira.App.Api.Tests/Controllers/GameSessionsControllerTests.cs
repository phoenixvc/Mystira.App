using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Api.Models;
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.CQRS.GameSessions.Queries;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.GameSessions;
using Mystira.Contracts.App.Requests.Scenarios;
using Mystira.Contracts.App.Responses.GameSessions;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class GameSessionsControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<GameSessionsController>> _mockLogger;
    private readonly GameSessionsController _controller;

    public GameSessionsControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<GameSessionsController>>();
        _controller = new GameSessionsController(_mockBus.Object, _mockLogger.Object);

        // Setup HttpContext with authenticated user
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        var claims = new List<Claim>
        {
            new Claim("sub", "test-account-id"),
            new Claim(ClaimTypes.NameIdentifier, "test-account-id")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupUnauthenticatedUser()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // No claims
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region StartSession Tests

    [Fact]
    public async Task StartSession_WithValidRequest_ReturnsCreatedWithSession()
    {
        // Arrange
        var request = new StartGameSessionRequest { ScenarioId = "scenario-1" };
        var session = new GameSession { Id = "session-1", ScenarioId = "scenario-1" };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession>(
                It.IsAny<StartGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.StartSession(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(GameSessionsController.GetSession));
    }

    [Fact]
    public async Task StartSession_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var request = new StartGameSessionRequest { ScenarioId = "scenario-1" };

        // Act
        var result = await _controller.StartSession(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task StartSession_WhenArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new StartGameSessionRequest { ScenarioId = "invalid" };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession>(
                It.IsAny<StartGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new ArgumentException("Invalid scenario"));

        // Act
        var result = await _controller.StartSession(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region PauseSession Tests

    [Fact]
    public async Task PauseSession_WhenSessionExists_ReturnsOkWithSession()
    {
        // Arrange
        var sessionId = "session-1";
        var session = new GameSession { Id = sessionId, Status = SessionStatus.Paused };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<PauseGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.PauseSession(sessionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSession = okResult.Value.Should().BeOfType<GameSession>().Subject;
        returnedSession.Status.Should().Be(SessionStatus.Paused);
    }

    [Fact]
    public async Task PauseSession_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<PauseGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _controller.PauseSession(sessionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PauseSession_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.PauseSession("session-1");

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region ResumeSession Tests

    [Fact]
    public async Task ResumeSession_WhenSessionExists_ReturnsOkWithSession()
    {
        // Arrange
        var sessionId = "session-1";
        var session = new GameSession { Id = sessionId, Status = SessionStatus.InProgress };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<ResumeGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.ResumeSession(sessionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSession = okResult.Value.Should().BeOfType<GameSession>().Subject;
        returnedSession.Status.Should().Be(SessionStatus.InProgress);
    }

    [Fact]
    public async Task ResumeSession_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<ResumeGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _controller.ResumeSession(sessionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region EndSession Tests

    [Fact]
    public async Task EndSession_WhenSessionExists_ReturnsOkWithSession()
    {
        // Arrange
        var sessionId = "session-1";
        var session = new GameSession { Id = sessionId, Status = SessionStatus.Completed };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<EndGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.EndSession(sessionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSession = okResult.Value.Should().BeOfType<GameSession>().Subject;
        returnedSession.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public async Task EndSession_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<EndGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _controller.EndSession(sessionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region FinalizeSession Tests

    [Fact]
    public async Task FinalizeSession_ReturnsOkWithResult()
    {
        // Arrange
        var sessionId = "session-1";
        var finalizationResult = new { SessionId = sessionId, Awards = new List<string>() };

        _mockBus
            .Setup(x => x.InvokeAsync<object>(
                It.IsAny<FinalizeGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(finalizationResult);

        // Act
        var result = await _controller.FinalizeSession(sessionId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task FinalizeSession_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var sessionId = "session-1";

        _mockBus
            .Setup(x => x.InvokeAsync<object>(
                It.IsAny<FinalizeGameSessionCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.FinalizeSession(sessionId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetSession Tests

    [Fact]
    public async Task GetSession_WhenSessionExists_ReturnsOkWithSession()
    {
        // Arrange
        var sessionId = "session-1";
        var session = new GameSession { Id = sessionId, ScenarioId = "scenario-1" };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<GetGameSessionQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSession = okResult.Value.Should().BeOfType<GameSession>().Subject;
        returnedSession.Id.Should().Be(sessionId);
    }

    [Fact]
    public async Task GetSession_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<GetGameSessionQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetSessionsByProfile Tests

    [Fact]
    public async Task GetSessionsByProfile_ReturnsOkWithSessions()
    {
        // Arrange
        var profileId = "profile-1";
        var sessions = new List<GameSessionResponse>
        {
            new GameSessionResponse { Id = "session-1" },
            new GameSessionResponse { Id = "session-2" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<GameSessionResponse>>(
                It.IsAny<GetSessionsByProfileQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetSessionsByProfile(profileId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeOfType<List<GameSessionResponse>>().Subject;
        returnedSessions.Should().HaveCount(2);
    }

    #endregion

    #region GetInProgressSessions Tests

    [Fact]
    public async Task GetInProgressSessions_ReturnsOkWithSessions()
    {
        // Arrange
        var accountId = "acc-1";
        var sessions = new List<GameSessionResponse>
        {
            new GameSessionResponse { Id = "session-1" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<GameSessionResponse>>(
                It.IsAny<GetInProgressSessionsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetInProgressSessions(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeOfType<List<GameSessionResponse>>().Subject;
        returnedSessions.Should().HaveCount(1);
    }

    #endregion

    #region GetSessionStats Tests

    [Fact]
    public async Task GetSessionStats_WhenSessionExists_ReturnsOkWithStats()
    {
        // Arrange
        var sessionId = "session-1";
        var stats = new SessionStatsResponse { TotalChoicesMade = 10, TotalSceneProgression = 5 };

        _mockBus
            .Setup(x => x.InvokeAsync<SessionStatsResponse?>(
                It.IsAny<GetSessionStatsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetSessionStats(sessionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeOfType<SessionStatsResponse>().Subject;
        returnedStats.TotalChoicesMade.Should().Be(10);
    }

    [Fact]
    public async Task GetSessionStats_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<SessionStatsResponse?>(
                It.IsAny<GetSessionStatsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((SessionStatsResponse?)null);

        // Act
        var result = await _controller.GetSessionStats(sessionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetAchievements Tests

    [Fact]
    public async Task GetAchievements_ReturnsOkWithAchievements()
    {
        // Arrange
        var sessionId = "session-1";
        var achievements = new List<SessionAchievement>
        {
            new SessionAchievement { Name = "First Steps", Description = "Completed first scene" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<SessionAchievement>>(
                It.IsAny<GetAchievementsQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(achievements);

        // Act
        var result = await _controller.GetAchievements(sessionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAchievements = okResult.Value.Should().BeOfType<List<SessionAchievement>>().Subject;
        returnedAchievements.Should().HaveCount(1);
    }

    #endregion

    #region MakeChoice Tests

    [Fact]
    public async Task MakeChoice_WithValidRequest_ReturnsOkWithSession()
    {
        // Arrange
        var request = new MakeChoiceRequest { SessionId = "session-1", ChoiceId = "choice-1" };
        var session = new GameSession { Id = "session-1" };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<MakeChoiceCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.MakeChoice(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<GameSession>();
    }

    [Fact]
    public async Task MakeChoice_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new MakeChoiceRequest { SessionId = "nonexistent", ChoiceId = "choice-1" };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<MakeChoiceCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _controller.MakeChoice(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MakeChoice_WhenInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var request = new MakeChoiceRequest { SessionId = "session-1", ChoiceId = "invalid" };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<MakeChoiceCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new InvalidOperationException("Invalid choice for current scene"));

        // Act
        var result = await _controller.MakeChoice(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ProgressScene Tests

    [Fact]
    public async Task ProgressScene_WithValidRequest_ReturnsOkWithSession()
    {
        // Arrange
        var sessionId = "session-1";
        var request = new ProgressSceneRequest { SessionId = sessionId, NextSceneId = "scene-2" };
        var session = new GameSession { Id = sessionId };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<ProgressSceneCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.ProgressScene(sessionId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<GameSession>();
    }

    [Fact]
    public async Task ProgressScene_WhenSessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var sessionId = "nonexistent";
        var request = new ProgressSceneRequest { NextSceneId = "scene-2" };

        _mockBus
            .Setup(x => x.InvokeAsync<GameSession?>(
                It.IsAny<ProgressSceneCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _controller.ProgressScene(sessionId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CompleteScenarioForAccount Tests

    [Fact]
    public async Task CompleteScenarioForAccount_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new CompleteScenarioRequest { AccountId = "acc-1", ScenarioId = "scenario-1" };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<AddCompletedScenarioCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CompleteScenarioForAccount(request);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task CompleteScenarioForAccount_WithMissingAccountId_ReturnsBadRequest()
    {
        // Arrange
        var request = new CompleteScenarioRequest { AccountId = "", ScenarioId = "scenario-1" };

        // Act
        var result = await _controller.CompleteScenarioForAccount(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CompleteScenarioForAccount_WhenAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CompleteScenarioRequest { AccountId = "nonexistent", ScenarioId = "scenario-1" };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<AddCompletedScenarioCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CompleteScenarioForAccount(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
