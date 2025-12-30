using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Mystira.App.PWA.Models;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

public class GameSessionApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<GameSessionApiClient>> _loggerMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly GameSessionApiClient _client;
    private readonly string _baseUrl = "http://localhost:5000/";

    public GameSessionApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_baseUrl)
        };

        _loggerMock = new Mock<ILogger<GameSessionApiClient>>();
        _tokenProviderMock = new Mock<ITokenProvider>();

        // Default behavior for TokenProvider
        _tokenProviderMock.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(true);
        _tokenProviderMock.Setup(x => x.GetCurrentTokenAsync()).ReturnsAsync("test-token");

        _client = new GameSessionApiClient(_httpClient, _loggerMock.Object, _tokenProviderMock.Object);
    }

    private void SetupResponse(HttpStatusCode statusCode, object? content = null)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode
        };

        if (content != null)
        {
            response.Content = JsonContent.Create(content);
        }
        else if (statusCode != HttpStatusCode.OK)
        {
             response.Content = new StringContent("Error message");
        }
        else
        {
             response.Content = new StringContent("");
        }

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);
    }
    
    private void SetupException(Exception exception)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(exception);
    }

    [Fact]
    public async Task StartGameSessionAsync_WhenSuccessful_ReturnsGameSession()
    {
        // Arrange
        var expectedSession = new GameSession { Id = "session-123" };
        SetupResponse(HttpStatusCode.OK, expectedSession);

        var playerNames = new List<string> { "Player1" };

        // Act
        var result = await _client.StartGameSessionAsync("scenario-1", "account-1", "profile-1", playerNames, "10-12");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("session-123");
        
        _tokenProviderMock.Verify(x => x.GetCurrentTokenAsync(), Times.Once);
    }

    [Fact]
    public async Task StartGameSessionAsync_WhenApiFails_ReturnsNullAndLogsWarning()
    {
        // Arrange
        SetupResponse(HttpStatusCode.BadRequest, "Invalid data");

        // Act
        var result = await _client.StartGameSessionAsync("scenario-1", "account-1", "profile-1", new List<string>(), "10-12");

        // Assert
        result.Should().BeNull();
        
        // Verify logger was called with warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start game session")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task StartGameSessionAsync_WhenNetworkException_ReturnsNullAndLogsError()
    {
        // Arrange
        SetupException(new HttpRequestException("Network error"));

        // Act
        var result = await _client.StartGameSessionAsync("scenario-1", "account-1", "profile-1", new List<string>(), "10-12");

        // Assert
        result.Should().BeNull();
        
        // Verify logger was called with error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Network error starting game session")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartGameSessionWithAssignmentsAsync_WhenSuccessful_ReturnsGameSession()
    {
        // Arrange
        var expectedSession = new GameSession { Id = "session-with-assignments" };
        SetupResponse(HttpStatusCode.OK, expectedSession);
        
        var request = new StartGameSessionRequest 
        { 
            ScenarioId = "scen-1",
            AccountId = "acc-1",
            ProfileId = "prof-1"
        };

        // Act
        var result = await _client.StartGameSessionWithAssignmentsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("session-with-assignments");
    }
    
    [Fact]
    public async Task StartGameSessionWithAssignmentsAsync_WhenApiFails_ReturnsNull()
    {
        // Arrange
        SetupResponse(HttpStatusCode.InternalServerError);
        var request = new StartGameSessionRequest { ScenarioId = "scen-1" };

        // Act
        var result = await _client.StartGameSessionWithAssignmentsAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EndGameSessionAsync_WhenSuccessful_ReturnsGameSession()
    {
        // Arrange
        var expectedSession = new GameSession { Id = "session-ended", Status = Mystira.Domain.Models.SessionStatus.Completed };
        SetupResponse(HttpStatusCode.OK, expectedSession);

        // Act
        var result = await _client.EndGameSessionAsync("session-123");

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(Mystira.Domain.Models.SessionStatus.Completed);
    }
    
    [Fact]
    public async Task EndGameSessionAsync_WhenApiFails_ReturnsNull()
    {
        // Arrange
        SetupResponse(HttpStatusCode.NotFound);

        // Act
        var result = await _client.EndGameSessionAsync("session-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FinalizeGameSessionAsync_WhenSuccessful_ReturnsResponse()
    {
        // Arrange
        var expectedResponse = new FinalizeSessionResponse { SessionId = "finalized-123" };
        SetupResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.FinalizeGameSessionAsync("session-123");

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be("finalized-123");
    }

    [Fact]
    public async Task PauseGameSessionAsync_WhenSuccessful_ReturnsGameSession()
    {
        // Arrange
        var expectedSession = new GameSession { Id = "paused-123", Status = Mystira.Domain.Models.SessionStatus.Paused };
        SetupResponse(HttpStatusCode.OK, expectedSession);

        // Act
        var result = await _client.PauseGameSessionAsync("session-123");

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(Mystira.Domain.Models.SessionStatus.Paused);
    }

    [Fact]
    public async Task ResumeGameSessionAsync_WhenSuccessful_ReturnsGameSession()
    {
        // Arrange
        var expectedSession = new GameSession { Id = "resumed-123", Status = Mystira.Domain.Models.SessionStatus.InProgress };
        SetupResponse(HttpStatusCode.OK, expectedSession);

        // Act
        var result = await _client.ResumeGameSessionAsync("session-123");

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(Mystira.Domain.Models.SessionStatus.InProgress);
    }

    [Fact]
    public async Task ProgressSessionSceneAsync_WhenSuccessful_ReturnsGameSession()
    {
        // Arrange
        var expectedSession = new GameSession { Id = "session-123", CurrentSceneId = "scene-2" };
        SetupResponse(HttpStatusCode.OK, expectedSession);

        // Act
        var result = await _client.ProgressSessionSceneAsync("session-123", "scene-2");

        // Assert
        result.Should().NotBeNull();
        result!.CurrentSceneId.Should().Be("scene-2");
    }

    [Fact]
    public async Task MakeChoiceAsync_WhenSuccessful_ReturnsGameSession()
    {
        // Arrange
        var expectedSession = new GameSession { Id = "session-123", CurrentSceneId = "next-scene" };
        SetupResponse(HttpStatusCode.OK, expectedSession);

        // Act
        var result = await _client.MakeChoiceAsync("session-123", "scene-1", "Choice A", "next-scene");

        // Assert
        result.Should().NotBeNull();
        result!.CurrentSceneId.Should().Be("next-scene");
    }
    
    [Fact]
    public async Task GetSessionsByAccountAsync_WhenSuccessful_ReturnsList()
    {
        // Arrange
        var expectedSessions = new List<GameSession> 
        { 
            new GameSession { Id = "s1" },
            new GameSession { Id = "s2" }
        };
        SetupResponse(HttpStatusCode.OK, expectedSessions);

        // Act
        var result = await _client.GetSessionsByAccountAsync("account-123");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSessionsByProfileAsync_WhenSuccessful_ReturnsList()
    {
        // Arrange
        var expectedSessions = new List<GameSession> 
        { 
            new GameSession { Id = "s1" }
        };
        SetupResponse(HttpStatusCode.OK, expectedSessions);

        // Act
        var result = await _client.GetSessionsByProfileAsync("profile-123");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task GetInProgressSessionsAsync_WhenSuccessful_ReturnsList()
    {
        // Arrange
        var expectedSessions = new List<GameSession> 
        { 
            new GameSession { Id = "s1", Status = Mystira.Domain.Models.SessionStatus.InProgress }
        };
        SetupResponse(HttpStatusCode.OK, expectedSessions);

        // Act
        var result = await _client.GetInProgressSessionsAsync("account-123");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Status.Should().Be(Mystira.Domain.Models.SessionStatus.InProgress);
    }

    [Fact]
    public async Task GetSessionsByAccountAsync_WhenNetworkError_ReturnsNull()
    {
        // Arrange
        SetupException(new HttpRequestException("Fetch error"));

        // Act
        var result = await _client.GetSessionsByAccountAsync("account-123");

        // Assert
        result.Should().BeNull();
    }
}
