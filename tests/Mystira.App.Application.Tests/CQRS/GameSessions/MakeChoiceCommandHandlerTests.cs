using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Models;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class MakeChoiceCommandHandlerTests
{
    private readonly Mock<IGameSessionRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger> _logger;

    public MakeChoiceCommandHandlerTests()
    {
        _repository = new Mock<IGameSessionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger>();
    }

    [Fact]
    public async Task Handle_WithValidRequest_RecordsChoice()
    {
        // Arrange
        var session = CreateActiveSession();
        var request = CreateValidRequest(session.Id);

        _repository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);

        var command = new MakeChoiceCommand(request);

        // Act
        var result = await MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ChoiceHistory.Should().HaveCount(1);
        result.ChoiceHistory[0].SceneId.Should().Be(request.SceneId);
        result.ChoiceHistory[0].ChoiceText.Should().Be(request.ChoiceText);
        result.CurrentSceneId.Should().Be(request.NextSceneId);

        _repository.Verify(r => r.UpdateAsync(It.IsAny<GameSession>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCompassChange_UpdatesCompassTracking()
    {
        // Arrange
        var session = CreateActiveSession();
        session.CompassValues["courage"] = new CompassTracking
        {
            Axis = "courage",
            CurrentValue = 0.0,
            StartingValue = 0.0,
            History = new List<CompassChange>()
        };

        var request = CreateValidRequest(session.Id);
        request.CompassAxis = "courage";
        request.CompassDirection = "positive";
        request.CompassDelta = 1.5;

        _repository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);

        var command = new MakeChoiceCommand(request);

        // Act
        var result = await MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ChoiceHistory[0].CompassAxis.Should().Be("courage");
        result.ChoiceHistory[0].CompassDelta.Should().Be(1.5);
        result.ChoiceHistory[0].CompassChange.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithMissingSessionId_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest("test-session");
        request.SessionId = "";
        var command = new MakeChoiceCommand(request);

        // Act
        var act = () => MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SessionId*");
    }

    [Fact]
    public async Task Handle_WithMissingSceneId_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest("test-session");
        request.SceneId = "";
        var command = new MakeChoiceCommand(request);

        // Act
        var act = () => MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SceneId*");
    }

    [Fact]
    public async Task Handle_WithMissingChoiceText_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest("test-session");
        request.ChoiceText = "";
        var command = new MakeChoiceCommand(request);

        // Act
        var act = () => MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ChoiceText*");
    }

    [Fact]
    public async Task Handle_WithMissingNextSceneId_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest("test-session");
        request.NextSceneId = "";
        var command = new MakeChoiceCommand(request);

        // Act
        var act = () => MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*NextSceneId*");
    }

    [Fact]
    public async Task Handle_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var request = CreateValidRequest("non-existent-session");
        _repository.Setup(r => r.GetByIdAsync(request.SessionId))
            .ReturnsAsync((GameSession?)null);

        var command = new MakeChoiceCommand(request);

        // Act
        var result = await MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<GameSession>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCompletedSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = CreateActiveSession();
        session.Status = SessionStatus.Completed;

        var request = CreateValidRequest(session.Id);
        _repository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);

        var command = new MakeChoiceCommand(request);

        // Act
        var act = () => MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status*Completed*");
    }

    [Fact]
    public async Task Handle_WithPausedSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = CreateActiveSession();
        session.Status = SessionStatus.Paused;

        var request = CreateValidRequest(session.Id);
        _repository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);

        var command = new MakeChoiceCommand(request);

        // Act
        var act = () => MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status*Paused*");
    }

    [Fact]
    public async Task Handle_UpdatesElapsedTime()
    {
        // Arrange
        var session = CreateActiveSession();
        session.StartTime = DateTime.UtcNow.AddMinutes(-5);

        var request = CreateValidRequest(session.Id);
        _repository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);

        var command = new MakeChoiceCommand(request);

        // Act
        var result = await MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.ElapsedTime.TotalMinutes.Should().BeGreaterOrEqualTo(4.9);
    }

    [Fact]
    public async Task Handle_WithProvidedPlayerId_UsesProvidedPlayerId()
    {
        // Arrange
        var session = CreateActiveSession();
        var request = CreateValidRequest(session.Id);
        request.PlayerId = "specific-player-id";

        _repository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);

        var command = new MakeChoiceCommand(request);

        // Act
        var result = await MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ChoiceHistory[0].PlayerId.Should().Be("specific-player-id");
    }

    [Fact]
    public async Task Handle_WithoutPlayerId_UsesProfileId()
    {
        // Arrange
        var session = CreateActiveSession();
        session.ProfileId = "session-profile-id";
        var request = CreateValidRequest(session.Id);
        request.PlayerId = null;

        _repository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);

        var command = new MakeChoiceCommand(request);

        // Act
        var result = await MakeChoiceCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ChoiceHistory[0].PlayerId.Should().Be("session-profile-id");
    }

    private static GameSession CreateActiveSession()
    {
        return new GameSession
        {
            Id = Guid.NewGuid().ToString("N"),
            ScenarioId = "test-scenario",
            AccountId = "test-account",
            ProfileId = "test-profile",
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow,
            CurrentSceneId = "scene1",
            ChoiceHistory = new List<SessionChoice>(),
            EchoHistory = new List<EchoLog>(),
            Achievements = new List<SessionAchievement>(),
            CompassValues = new Dictionary<string, CompassTracking>()
        };
    }

    private static MakeChoiceRequest CreateValidRequest(string sessionId)
    {
        return new MakeChoiceRequest
        {
            SessionId = sessionId,
            SceneId = "scene1",
            ChoiceText = "Go left",
            NextSceneId = "scene2"
        };
    }
}
