using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.UseCases.GameSessions;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.UseCases;

public class CheckAchievementsUseCaseTests
{
    private readonly Mock<IGameSessionRepository> _sessionRepository;
    private readonly Mock<IRepository<BadgeConfiguration>> _badgeRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<CheckAchievementsUseCase>> _logger;
    private readonly CheckAchievementsUseCase _useCase;

    public CheckAchievementsUseCaseTests()
    {
        _sessionRepository = new Mock<IGameSessionRepository>();
        _badgeRepository = new Mock<IRepository<BadgeConfiguration>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<CheckAchievementsUseCase>>();

        _useCase = new CheckAchievementsUseCase(
            _sessionRepository.Object,
            _badgeRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOrEmptySessionId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync("   "));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentSession_ReturnsEmptyList()
    {
        // Arrange
        _sessionRepository.Setup(r => r.GetByIdAsync("non-existent"))
            .ReturnsAsync((GameSession?)null);

        // Act
        var result = await _useCase.ExecuteAsync("non-existent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithFirstChoice_AwardsFirstChoiceAchievement()
    {
        // Arrange
        var session = CreateTestSession();
        session.ChoiceHistory = new List<SessionChoice>
        {
            new SessionChoice { SceneId = "scene1", ChoiceText = "First choice" }
        };

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration>());

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().ContainSingle(a => a.Type == AchievementType.FirstChoice);
        result.First(a => a.Type == AchievementType.FirstChoice).Title.Should().Be("First Steps");

        _sessionRepository.Verify(r => r.UpdateAsync(It.IsAny<GameSession>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedSession_AwardsCompletionAchievement()
    {
        // Arrange
        var session = CreateTestSession();
        session.Status = SessionStatus.Completed;
        session.ChoiceHistory = new List<SessionChoice>
        {
            new SessionChoice { SceneId = "scene1", ChoiceText = "Choice 1" },
            new SessionChoice { SceneId = "scene2", ChoiceText = "Choice 2" }
        };

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration>());

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().ContainSingle(a => a.Type == AchievementType.SessionComplete);
        result.First(a => a.Type == AchievementType.SessionComplete).Title.Should().Be("Adventure Complete");
    }

    [Fact]
    public async Task ExecuteAsync_WithCompassThresholdReached_AwardsCompassAchievement()
    {
        // Arrange
        var session = CreateTestSession();
        session.CompassValues = new Dictionary<string, CompassTracking>
        {
            ["courage"] = new CompassTracking
            {
                Axis = "courage",
                CurrentValue = 4.0, // Above default threshold of 3.0
                StartingValue = 0.0,
                History = new List<CompassChange>()
            }
        };

        var badgeConfig = new BadgeConfiguration
        {
            Id = "courage-badge",
            Name = "Brave Heart",
            Axis = "courage",
            Threshold = 3.0f,
            Message = "You showed great courage!"
        };

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration> { badgeConfig });

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().ContainSingle(a => a.Type == AchievementType.CompassThreshold);
        var compassAchievement = result.First(a => a.Type == AchievementType.CompassThreshold);
        compassAchievement.Title.Should().Be("Brave Heart");
        compassAchievement.CompassAxis.Should().Be("courage");
    }

    [Fact]
    public async Task ExecuteAsync_WithNegativeCompassThresholdReached_AwardsCompassAchievement()
    {
        // Arrange
        var session = CreateTestSession();
        session.CompassValues = new Dictionary<string, CompassTracking>
        {
            ["caution"] = new CompassTracking
            {
                Axis = "caution",
                CurrentValue = -4.0, // Absolute value above threshold
                StartingValue = 0.0,
                History = new List<CompassChange>()
            }
        };

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration>());

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().ContainSingle(a => a.Type == AchievementType.CompassThreshold);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingAchievement_DoesNotDuplicateAchievement()
    {
        // Arrange
        var session = CreateTestSession();
        session.ChoiceHistory = new List<SessionChoice>
        {
            new SessionChoice { SceneId = "scene1", ChoiceText = "First choice" }
        };
        session.Achievements = new List<SessionAchievement>
        {
            new SessionAchievement
            {
                Id = $"{session.Id}_first_choice",
                Type = AchievementType.FirstChoice,
                Title = "First Steps"
            }
        };

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration>());

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().BeEmpty(); // No new achievements
        _sessionRepository.Verify(r => r.UpdateAsync(It.IsAny<GameSession>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompassBelowThreshold_DoesNotAwardCompassAchievement()
    {
        // Arrange
        var session = CreateTestSession();
        session.CompassValues = new Dictionary<string, CompassTracking>
        {
            ["courage"] = new CompassTracking
            {
                Axis = "courage",
                CurrentValue = 2.0, // Below default threshold of 3.0
                StartingValue = 0.0,
                History = new List<CompassChange>()
            }
        };

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration>());

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().NotContain(a => a.Type == AchievementType.CompassThreshold);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleAchievements_AwardsAllApplicable()
    {
        // Arrange
        var session = CreateTestSession();
        session.Status = SessionStatus.Completed;
        session.ChoiceHistory = new List<SessionChoice>
        {
            new SessionChoice { SceneId = "scene1", ChoiceText = "First choice" }
        };
        session.CompassValues = new Dictionary<string, CompassTracking>
        {
            ["courage"] = new CompassTracking
            {
                Axis = "courage",
                CurrentValue = 4.0,
                StartingValue = 0.0,
                History = new List<CompassChange>()
            }
        };

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration>());

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().HaveCount(3); // First choice + completion + compass
        result.Should().Contain(a => a.Type == AchievementType.FirstChoice);
        result.Should().Contain(a => a.Type == AchievementType.SessionComplete);
        result.Should().Contain(a => a.Type == AchievementType.CompassThreshold);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoNewAchievements_DoesNotSave()
    {
        // Arrange
        var session = CreateTestSession();
        session.ChoiceHistory = new List<SessionChoice>(); // No choices
        session.CompassValues = new Dictionary<string, CompassTracking>(); // No compass values

        _sessionRepository.Setup(r => r.GetByIdAsync(session.Id))
            .ReturnsAsync(session);
        _badgeRepository.Setup(r => r.ListAsync(It.IsAny<Ardalis.Specification.ISpecification<BadgeConfiguration>>()))
            .ReturnsAsync(new List<BadgeConfiguration>());

        // Act
        var result = await _useCase.ExecuteAsync(session.Id);

        // Assert
        result.Should().BeEmpty();
        _sessionRepository.Verify(r => r.UpdateAsync(It.IsAny<GameSession>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static GameSession CreateTestSession()
    {
        return new GameSession
        {
            Id = Guid.NewGuid().ToString("N"),
            ScenarioId = "test-scenario",
            AccountId = "test-account",
            ProfileId = "test-profile",
            Status = SessionStatus.InProgress,
            StartTime = DateTime.UtcNow,
            ChoiceHistory = new List<SessionChoice>(),
            EchoHistory = new List<EchoLog>(),
            Achievements = new List<SessionAchievement>(),
            CompassValues = new Dictionary<string, CompassTracking>()
        };
    }
}
