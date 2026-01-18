using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Tests.CQRS.Accounts;

public class CreateAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger> _logger;

    public CreateAccountCommandHandlerTests()
    {
        _repository = new Mock<IAccountRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger>();
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesNewAccount()
    {
        // Arrange
        var command = new CreateAccountCommand(
            ExternalUserId: "ext-123",
            Email: "test@example.com",
            DisplayName: "Test User",
            UserProfileIds: null,
            Subscription: null,
            Settings: null
        );

        _repository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(default(Account));

        // Act
        var result = await CreateAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExternalUserId.Should().Be("ext-123");
        result.Email.Should().Be("test@example.com");
        result.DisplayName.Should().Be("Test User");
        result.Id.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _repository.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutDisplayName_UsesEmailPrefix()
    {
        // Arrange
        var command = new CreateAccountCommand(
            ExternalUserId: "ext-456",
            Email: "johndoe@example.com",
            DisplayName: null,
            UserProfileIds: null,
            Subscription: null,
            Settings: null
        );

        _repository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(default(Account));

        // Act
        var result = await CreateAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.DisplayName.Should().Be("johndoe");
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingAccount = new Account
        {
            Id = "existing-123",
            Email = "existing@example.com",
            ExternalUserId = "ext-existing"
        };

        var command = new CreateAccountCommand(
            ExternalUserId: "ext-new",
            Email: "existing@example.com",
            DisplayName: "New User",
            UserProfileIds: null,
            Subscription: null,
            Settings: null
        );

        _repository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(existingAccount);

        // Act
        var act = () => CreateAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        _repository.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithProfileIds_SetsUserProfileIds()
    {
        // Arrange
        var profileIds = new List<string> { "profile-1", "profile-2" };
        var command = new CreateAccountCommand(
            ExternalUserId: "ext-789",
            Email: "profiles@example.com",
            DisplayName: "User With Profiles",
            UserProfileIds: profileIds,
            Subscription: null,
            Settings: null
        );

        _repository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(default(Account));

        // Act
        var result = await CreateAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.UserProfileIds.Should().BeEquivalentTo(profileIds);
    }

    [Fact]
    public async Task Handle_WithSubscription_SetsSubscriptionDetails()
    {
        // Arrange
        var subscription = new SubscriptionDetails
        {
            Tier = "premium",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        var command = new CreateAccountCommand(
            ExternalUserId: "ext-sub",
            Email: "subscriber@example.com",
            DisplayName: "Premium User",
            UserProfileIds: null,
            Subscription: subscription,
            Settings: null
        );

        _repository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(default(Account));

        // Act
        var result = await CreateAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Subscription.Should().NotBeNull();
        result.Subscription.Tier.Should().Be("premium");
    }

    [Fact]
    public async Task Handle_WithSettings_SetsAccountSettings()
    {
        // Arrange
        var settings = new AccountSettings
        {
            NotificationsEnabled = true,
            PreferredLanguage = "en"
        };

        var command = new CreateAccountCommand(
            ExternalUserId: "ext-settings",
            Email: "settings@example.com",
            DisplayName: "Settings User",
            UserProfileIds: null,
            Subscription: null,
            Settings: settings
        );

        _repository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(default(Account));

        // Act
        var result = await CreateAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Settings.Should().NotBeNull();
        result.Settings.NotificationsEnabled.Should().BeTrue();
        result.Settings.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public async Task Handle_SetsLastLoginAtToCurrentTime()
    {
        // Arrange
        var command = new CreateAccountCommand(
            ExternalUserId: "ext-login",
            Email: "login@example.com",
            DisplayName: "Login User",
            UserProfileIds: null,
            Subscription: null,
            Settings: null
        );

        _repository.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(default(Account));

        // Act
        var result = await CreateAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
