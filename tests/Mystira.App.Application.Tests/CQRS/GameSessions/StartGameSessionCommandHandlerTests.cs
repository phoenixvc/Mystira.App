using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.GameSessions.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Models;
using Mystira.Contracts.App.Requests.GameSessions;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Tests.CQRS.GameSessions;

public class StartGameSessionCommandHandlerTests
{
    private readonly Mock<IGameSessionRepository> _sessionRepository;
    private readonly Mock<IScenarioRepository> _scenarioRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger> _logger;

    public StartGameSessionCommandHandlerTests()
    {
        _sessionRepository = new Mock<IGameSessionRepository>();
        _scenarioRepository = new Mock<IScenarioRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger>();
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesNewGameSession()
    {
        // Arrange
        var request = CreateValidRequest();
        var scenario = CreateTestScenario();

        _sessionRepository.Setup(r => r.GetActiveSessionsByScenarioAndAccountAsync(request.ScenarioId, request.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());
        _scenarioRepository.Setup(r => r.GetByIdAsync(request.ScenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        var command = new StartGameSessionCommand(request);

        // Act
        var result = await StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ScenarioId.Should().Be(request.ScenarioId);
        result.AccountId.Should().Be(request.AccountId);
        result.ProfileId.Should().Be(request.ProfileId);
        result.Status.Should().Be(SessionStatus.InProgress);
        result.PlayerNames.Should().Contain("Player1");
        result.CompassValues.Should().NotBeEmpty();

        _sessionRepository.Verify(r => r.AddAsync(It.IsAny<GameSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingActiveSession_ReusesExistingSession()
    {
        // Arrange
        var request = CreateValidRequest();
        var existingSession = new GameSession
        {
            Id = "existing-session-id",
            ScenarioId = request.ScenarioId,
            AccountId = request.AccountId,
            ProfileId = request.ProfileId,
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            PlayerNames = new List<string> { "ExistingPlayer" },
            ChoiceHistory = new List<SessionChoice>(),
            CompassValues = new Dictionary<string, CompassTracking>()
        };

        _sessionRepository.Setup(r => r.GetActiveSessionsByScenarioAndAccountAsync(request.ScenarioId, request.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession> { existingSession });
        _scenarioRepository.Setup(r => r.GetByIdAsync(request.ScenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestScenario());

        var command = new StartGameSessionCommand(request);

        // Act
        var result = await StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingSession.Id);
        _sessionRepository.Verify(r => r.AddAsync(It.IsAny<GameSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingScenarioId_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ScenarioId = "";
        var command = new StartGameSessionCommand(request);

        // Act
        var act = () => StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ScenarioId*");
    }

    [Fact]
    public async Task Handle_WithMissingAccountId_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.AccountId = "";
        var command = new StartGameSessionCommand(request);

        // Act
        var act = () => StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*AccountId*");
    }

    [Fact]
    public async Task Handle_WithMissingProfileId_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ProfileId = "";
        var command = new StartGameSessionCommand(request);

        // Act
        var act = () => StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ProfileId*");
    }

    [Fact]
    public async Task Handle_WithNoPlayersOrAssignments_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.PlayerNames = new List<string>();
        request.CharacterAssignments = new List<CharacterAssignmentDto>();
        var command = new StartGameSessionCommand(request);

        // Act
        var act = () => StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*player*character assignment*");
    }

    [Fact]
    public async Task Handle_WithScenarioAgeRestriction_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.TargetAgeGroup = "6-9";
        var scenario = CreateTestScenario();
        scenario.MinimumAge = 12; // Higher than target age group

        _sessionRepository.Setup(r => r.GetActiveSessionsByScenarioAndAccountAsync(request.ScenarioId, request.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());
        _scenarioRepository.Setup(r => r.GetByIdAsync(request.ScenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        var command = new StartGameSessionCommand(request);

        // Act
        var act = () => StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*minimum age*");
    }

    [Fact]
    public async Task Handle_WithCharacterAssignments_SetsPlayerNamesFromAssignments()
    {
        // Arrange
        var request = CreateValidRequest();
        request.PlayerNames = new List<string>(); // Empty
        request.CharacterAssignments = new List<CharacterAssignmentDto>
        {
            new CharacterAssignmentDto
            {
                CharacterId = "char1",
                CharacterName = "Hero",
                PlayerAssignment = new PlayerAssignmentDto
                {
                    Type = "profile",
                    ProfileName = "TestPlayer"
                }
            }
        };

        _sessionRepository.Setup(r => r.GetActiveSessionsByScenarioAndAccountAsync(request.ScenarioId, request.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());
        _scenarioRepository.Setup(r => r.GetByIdAsync(request.ScenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestScenario());

        var command = new StartGameSessionCommand(request);

        // Act
        var result = await StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.PlayerNames.Should().Contain("TestPlayer");
        result.CharacterAssignments.Should().HaveCount(1);
        result.CharacterAssignments[0].CharacterName.Should().Be("Hero");
    }

    [Fact]
    public async Task Handle_WithScenarioAxes_InitializesCompassTracking()
    {
        // Arrange
        var request = CreateValidRequest();
        var scenario = CreateTestScenario();
        scenario.CoreAxes = new List<CoreAxis>
        {
            CoreAxis.Parse("courage")!,
            CoreAxis.Parse("wisdom")!
        };

        _sessionRepository.Setup(r => r.GetActiveSessionsByScenarioAndAccountAsync(request.ScenarioId, request.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());
        _scenarioRepository.Setup(r => r.GetByIdAsync(request.ScenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        var command = new StartGameSessionCommand(request);

        // Act
        var result = await StartGameSessionCommandHandler.Handle(
            command,
            _sessionRepository.Object,
            _scenarioRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.CompassValues.Should().ContainKey("courage");
        result.CompassValues.Should().ContainKey("wisdom");
        result.CompassValues["courage"].CurrentValue.Should().Be(0.0);
    }

    private static StartGameSessionRequest CreateValidRequest()
    {
        return new StartGameSessionRequest
        {
            ScenarioId = "test-scenario-id",
            AccountId = "test-account-id",
            ProfileId = "test-profile-id",
            PlayerNames = new List<string> { "Player1" },
            TargetAgeGroup = "6-9"
        };
    }

    private static Scenario CreateTestScenario()
    {
        return new Scenario
        {
            Id = "test-scenario-id",
            Title = "Test Scenario",
            MinimumAge = 6,
            CoreAxes = new List<CoreAxis> { CoreAxis.Parse("courage") },
            Scenes = new List<Scene>
            {
                new Scene { Id = "scene1", Title = "Opening Scene" }
            }
        };
    }
}
