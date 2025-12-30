using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.UserBadges.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;
using Mystira.Contracts.App.Requests.Badges;

namespace Mystira.App.Application.Tests.CQRS.UserBadges;

public class AwardBadgeCommandHandlerTests
{
    private readonly Mock<IUserBadgeRepository> _badgeRepository;
    private readonly Mock<IRepository<BadgeConfiguration>> _badgeConfigRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger> _logger;

    public AwardBadgeCommandHandlerTests()
    {
        _badgeRepository = new Mock<IUserBadgeRepository>();
        _badgeConfigRepository = new Mock<IRepository<BadgeConfiguration>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger>();
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesUserBadge()
    {
        // Arrange
        var badgeConfig = new BadgeConfiguration
        {
            Id = "badge-config-123",
            Name = "Honesty Champion",
            Message = "You showed great honesty!",
            Axis = "honesty",
            Threshold = 10,
            ImageId = "img-123"
        };

        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-123",
            BadgeConfigurationId = "badge-config-123",
            TriggerValue = 15,
            GameSessionId = "session-456",
            ScenarioId = "scenario-789"
        };

        _badgeConfigRepository.Setup(r => r.GetByIdAsync("badge-config-123"))
            .ReturnsAsync(badgeConfig);

        var command = new AwardBadgeCommand(request);

        // Act
        var result = await AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserProfileId.Should().Be("profile-123");
        result.BadgeConfigurationId.Should().Be("badge-config-123");
        result.BadgeId.Should().Be("badge-config-123");
        result.BadgeName.Should().Be("Honesty Champion");
        result.BadgeMessage.Should().Be("You showed great honesty!");
        result.Axis.Should().Be("honesty");
        result.TriggerValue.Should().Be(15);
        result.Threshold.Should().Be(10);
        result.GameSessionId.Should().Be("session-456");
        result.ScenarioId.Should().Be("scenario-789");
        result.ImageId.Should().Be("img-123");
        result.EarnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _badgeRepository.Verify(r => r.AddAsync(It.IsAny<UserBadge>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyUserProfileId_ThrowsArgumentException()
    {
        // Arrange
        var request = new AwardBadgeRequest
        {
            UserProfileId = "",
            BadgeConfigurationId = "badge-config-123"
        };

        var command = new AwardBadgeCommand(request);

        // Act
        var act = () => AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UserProfileId*required*");
    }

    [Fact]
    public async Task Handle_WithNullUserProfileId_ThrowsArgumentException()
    {
        // Arrange
        var request = new AwardBadgeRequest
        {
            UserProfileId = null!,
            BadgeConfigurationId = "badge-config-123"
        };

        var command = new AwardBadgeCommand(request);

        // Act
        var act = () => AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*UserProfileId*required*");
    }

    [Fact]
    public async Task Handle_WithEmptyBadgeConfigurationId_ThrowsArgumentException()
    {
        // Arrange
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-123",
            BadgeConfigurationId = ""
        };

        var command = new AwardBadgeCommand(request);

        // Act
        var act = () => AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*BadgeConfigurationId*required*");
    }

    [Fact]
    public async Task Handle_WithNonexistentBadgeConfig_ThrowsArgumentException()
    {
        // Arrange
        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-123",
            BadgeConfigurationId = "nonexistent-badge"
        };

        _badgeConfigRepository.Setup(r => r.GetByIdAsync("nonexistent-badge"))
            .ReturnsAsync(default(BadgeConfiguration));

        var command = new AwardBadgeCommand(request);

        // Act
        var act = () => AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Badge not found*");
    }

    [Fact]
    public async Task Handle_GeneratesUniqueId()
    {
        // Arrange
        var badgeConfig = new BadgeConfiguration
        {
            Id = "badge-config-456",
            Name = "Courage Badge",
            Axis = "courage"
        };

        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-456",
            BadgeConfigurationId = "badge-config-456"
        };

        _badgeConfigRepository.Setup(r => r.GetByIdAsync("badge-config-456"))
            .ReturnsAsync(badgeConfig);

        var command = new AwardBadgeCommand(request);

        // Act
        var result = await AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithoutOptionalFields_CreatesValidBadge()
    {
        // Arrange
        var badgeConfig = new BadgeConfiguration
        {
            Id = "badge-config-789",
            Name = "Kindness Badge",
            Axis = "kindness"
        };

        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-789",
            BadgeConfigurationId = "badge-config-789"
            // No optional fields: TriggerValue, GameSessionId, ScenarioId
        };

        _badgeConfigRepository.Setup(r => r.GetByIdAsync("badge-config-789"))
            .ReturnsAsync(badgeConfig);

        var command = new AwardBadgeCommand(request);

        // Act
        var result = await AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BadgeName.Should().Be("Kindness Badge");
        result.Axis.Should().Be("kindness");
    }

    [Theory]
    [InlineData("honesty")]
    [InlineData("courage")]
    [InlineData("kindness")]
    [InlineData("compassion")]
    [InlineData("wisdom")]
    public async Task Handle_CopiesAxisFromBadgeConfig(string axis)
    {
        // Arrange
        var badgeConfig = new BadgeConfiguration
        {
            Id = $"badge-{axis}",
            Name = $"{axis} Badge",
            Axis = axis
        };

        var request = new AwardBadgeRequest
        {
            UserProfileId = "profile-test",
            BadgeConfigurationId = $"badge-{axis}"
        };

        _badgeConfigRepository.Setup(r => r.GetByIdAsync($"badge-{axis}"))
            .ReturnsAsync(badgeConfig);

        var command = new AwardBadgeCommand(request);

        // Act
        var result = await AwardBadgeCommandHandler.Handle(
            command,
            _badgeRepository.Object,
            _badgeConfigRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Axis.Should().Be(axis);
    }
}
