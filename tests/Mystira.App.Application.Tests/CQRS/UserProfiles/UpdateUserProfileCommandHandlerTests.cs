using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.UserProfiles.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.UserProfiles;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Tests.CQRS.UserProfiles;

public class UpdateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger> _logger;

    public UpdateUserProfileCommandHandlerTests()
    {
        _repository = new Mock<IUserProfileRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger>();
    }

    [Fact]
    public async Task Handle_WithExistingProfile_UpdatesAndReturnsProfile()
    {
        // Arrange
        var profileId = "profile-123";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            Name = "Original Name",
            AgeGroupName = "6-9"
        };

        var request = new UpdateUserProfileRequest
        {
            Bio = "Updated bio"
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Bio.Should().Be("Updated bio");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _repository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentProfile_ReturnsNull()
    {
        // Arrange
        var profileId = "nonexistent-123";
        var request = new UpdateUserProfileRequest();

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(UserProfile));

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdatesAgeGroup()
    {
        // Arrange
        var profileId = "profile-age";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            AgeGroupName = "6-9"
        };

        var request = new UpdateUserProfileRequest
        {
            AgeGroup = "10-12"
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.AgeGroupName.Should().Be("10-12");
    }

    [Fact]
    public async Task Handle_WithInvalidAgeGroup_ThrowsArgumentException()
    {
        // Arrange
        var profileId = "profile-invalid-age";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            AgeGroupName = "6-9"
        };

        var request = new UpdateUserProfileRequest
        {
            AgeGroup = "invalid-age-group"
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var act = () => UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid age group*");
    }

    [Fact]
    public async Task Handle_UpdatesDateOfBirthAndRefreshesAgeGroup()
    {
        // Arrange
        var profileId = "profile-dob";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            AgeGroupName = "6-9"
        };

        var newBirthDate = DateTime.Today.AddYears(-15); // 15 years old -> 13-18

        var request = new UpdateUserProfileRequest
        {
            DateOfBirth = newBirthDate
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.DateOfBirth.Should().Be(newBirthDate);
        result.AgeGroupName.Should().Be("13-18");
    }

    [Fact]
    public async Task Handle_UpdatesAvatar()
    {
        // Arrange
        var profileId = "profile-avatar";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            SelectedAvatarMediaId = "old-avatar"
        };

        var request = new UpdateUserProfileRequest
        {
            SelectedAvatarMediaId = "new-avatar-123"
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.SelectedAvatarMediaId.Should().Be("new-avatar-123");
    }

    [Fact]
    public async Task Handle_UpdatesPronouns()
    {
        // Arrange
        var profileId = "profile-pronouns";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            Pronouns = "he/him"
        };

        var request = new UpdateUserProfileRequest
        {
            Pronouns = "they/them"
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.Pronouns.Should().Be("they/them");
    }

    [Fact]
    public async Task Handle_UpdatesAccountId()
    {
        // Arrange
        var profileId = "profile-account";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            AccountId = "old-account"
        };

        var request = new UpdateUserProfileRequest
        {
            AccountId = "new-account-456"
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.AccountId.Should().Be("new-account-456");
    }

    [Fact]
    public async Task Handle_UpdatesOnboardingStatus()
    {
        // Arrange
        var profileId = "profile-onboarding";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            HasCompletedOnboarding = false
        };

        var request = new UpdateUserProfileRequest
        {
            HasCompletedOnboarding = true
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.HasCompletedOnboarding.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdatesIsGuest()
    {
        // Arrange
        var profileId = "profile-guest";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            IsGuest = true
        };

        var request = new UpdateUserProfileRequest
        {
            IsGuest = false
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.IsGuest.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UpdatesIsNpc()
    {
        // Arrange
        var profileId = "profile-npc";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            IsNpc = false
        };

        var request = new UpdateUserProfileRequest
        {
            IsNpc = true
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.IsNpc.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdatesFantasyThemes()
    {
        // Arrange
        var profileId = "profile-themes";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            PreferredFantasyThemes = new List<FantasyTheme> { new("Fantasy") }
        };

        var request = new UpdateUserProfileRequest
        {
            PreferredFantasyThemes = new List<string> { "Adventure", "Mystery" }
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.PreferredFantasyThemes.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNullFields_DoesNotUpdateThoseFields()
    {
        // Arrange
        var profileId = "profile-null";
        var existingProfile = new UserProfile
        {
            Id = profileId,
            Bio = "Original Bio",
            Pronouns = "she/her",
            SelectedAvatarMediaId = "original-avatar"
        };

        var request = new UpdateUserProfileRequest
        {
            Bio = null, // Should not update
            Pronouns = null, // Should not update
            SelectedAvatarMediaId = null // Should not update
        };

        _repository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var command = new UpdateUserProfileCommand(profileId, request);

        // Act
        var result = await UpdateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result!.Bio.Should().Be("Original Bio");
        result.Pronouns.Should().Be("she/her");
        result.SelectedAvatarMediaId.Should().Be("original-avatar");
    }
}
