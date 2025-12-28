using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.UserProfiles.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.Contracts.App.Requests.UserProfiles;

namespace Mystira.App.Application.Tests.CQRS.UserProfiles;

public class CreateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger> _logger;

    public CreateUserProfileCommandHandlerTests()
    {
        _repository = new Mock<IUserProfileRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger>();
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesNewProfile()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Test Profile",
            AgeGroup = "6-9",
            AccountId = "acc-123"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Profile");
        result.AgeGroupName.Should().Be("6-9");
        result.AccountId.Should().Be("acc-123");
        result.Id.Should().NotBeNullOrEmpty();

        _repository.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "",
            AgeGroup = "6-9"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var act = () => CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*name is required*");
    }

    [Fact]
    public async Task Handle_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = null!,
            AgeGroup = "6-9"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var act = () => CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*name is required*");
    }

    [Fact]
    public async Task Handle_WithShortName_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "A",
            AgeGroup = "6-9"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var act = () => CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least 2 characters*");
    }

    [Fact]
    public async Task Handle_WithEmptyAgeGroup_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Valid Name",
            AgeGroup = ""
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var act = () => CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Age group is required*");
    }

    [Fact]
    public async Task Handle_WithInvalidAgeGroup_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Valid Name",
            AgeGroup = "invalid-group"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var act = () => CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid age group*");
    }

    [Theory]
    [InlineData("1-2")]
    [InlineData("3-5")]
    [InlineData("6-9")]
    [InlineData("10-12")]
    [InlineData("13-18")]
    [InlineData("19-150")]
    public async Task Handle_WithValidAgeGroups_CreatesProfile(string ageGroup)
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Test Profile",
            AgeGroup = ageGroup
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.AgeGroupName.Should().Be(ageGroup);
    }

    [Fact]
    public async Task Handle_WithDateOfBirth_UpdatesAgeGroupFromBirthDate()
    {
        // Arrange
        var birthDate = DateTime.Today.AddYears(-7); // 7 years old -> 6-9 age group
        var request = new CreateUserProfileRequest
        {
            Name = "Child Profile",
            AgeGroup = "1-2", // Initial age group (will be updated)
            DateOfBirth = birthDate
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.DateOfBirth.Should().Be(birthDate);
        result.AgeGroupName.Should().Be("6-9");
    }

    [Fact]
    public async Task Handle_WithGuestProfile_SetsIsGuestTrue()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Guest User",
            AgeGroup = "6-9",
            IsGuest = true
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.IsGuest.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNpcProfile_SetsIsNpcTrue()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "NPC Character",
            AgeGroup = "10-12",
            IsNpc = true
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.IsNpc.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFantasyThemes_SetsPreferredThemes()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Theme Lover",
            AgeGroup = "10-12",
            PreferredFantasyThemes = new List<string> { "Fantasy", "Adventure" }
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.PreferredFantasyThemes.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithPronouns_SetsPronouns()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Pronoun User",
            AgeGroup = "13-18",
            Pronouns = "they/them"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Pronouns.Should().Be("they/them");
    }

    [Fact]
    public async Task Handle_WithBio_SetsBio()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Bio User",
            AgeGroup = "19-150",
            Bio = "Hello, I love adventures!"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Bio.Should().Be("Hello, I love adventures!");
    }

    [Fact]
    public async Task Handle_WithAvatar_SetsAvatarMediaId()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Avatar User",
            AgeGroup = "6-9",
            SelectedAvatarMediaId = "avatar-123"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.SelectedAvatarMediaId.Should().Be("avatar-123");
        result.AvatarMediaId.Should().Be("avatar-123");
    }

    [Fact]
    public async Task Handle_WithCompletedOnboarding_SetsFlag()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Onboarded User",
            AgeGroup = "10-12",
            HasCompletedOnboarding = true
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.HasCompletedOnboarding.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetsCreatedAtAndUpdatedAt()
    {
        // Arrange
        var request = new CreateUserProfileRequest
        {
            Name = "Timestamp User",
            AgeGroup = "6-9"
        };
        var command = new CreateUserProfileCommand(request);

        // Act
        var result = await CreateUserProfileCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
