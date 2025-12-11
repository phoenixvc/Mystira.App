using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.Auth.Commands;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Auth;

public class AuthCommandTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IPendingSignupRepository> _pendingSignupRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<RequestPasswordlessSigninCommandHandler>> _signinLoggerMock;
    private readonly Mock<ILogger<VerifyPasswordlessSigninCommandHandler>> _verifyLoggerMock;

    public AuthCommandTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _pendingSignupRepositoryMock = new Mock<IPendingSignupRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _emailServiceMock = new Mock<IEmailService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _signinLoggerMock = new Mock<ILogger<RequestPasswordlessSigninCommandHandler>>();
        _verifyLoggerMock = new Mock<ILogger<VerifyPasswordlessSigninCommandHandler>>();
    }

    #region RequestPasswordlessSigninCommand Tests

    [Fact]
    public async Task RequestPasswordlessSignin_WithExistingAccount_SendsEmailAndReturnsSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var account = new Account
        {
            Id = "account-1",
            Email = email,
            DisplayName = "Test User"
        };

        _accountRepositoryMock.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(account);
        _pendingSignupRepositoryMock.Setup(x => x.GetActiveByEmailAsync(email))
            .ReturnsAsync((PendingSignup?)null);
        _pendingSignupRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PendingSignup>()))
            .Returns(Task.CompletedTask);
        _emailServiceMock.Setup(x => x.SendSigninCodeAsync(email, account.DisplayName, It.IsAny<string>()))
            .ReturnsAsync((true, null));
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RequestPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object,
            _signinLoggerMock.Object);

        var command = new RequestPasswordlessSigninCommand(email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Check your email");
        result.Code.Should().NotBeNullOrEmpty();
        result.Code.Should().HaveLength(6);
        _emailServiceMock.Verify(x => x.SendSigninCodeAsync(email, account.DisplayName, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordlessSignin_WithNonExistentAccount_ReturnsFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _accountRepositoryMock.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Account?)null);

        var handler = new RequestPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object,
            _signinLoggerMock.Object);

        var command = new RequestPasswordlessSigninCommand(email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No account found");
        _emailServiceMock.Verify(x => x.SendSigninCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordlessSignin_WithExistingPendingSignin_ReusesCode()
    {
        // Arrange
        var email = "test@example.com";
        var existingCode = "123456";
        var account = new Account { Id = "account-1", Email = email, DisplayName = "Test User" };
        var existingPending = new PendingSignup
        {
            Email = email,
            Code = existingCode,
            IsSignin = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _accountRepositoryMock.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(account);
        _pendingSignupRepositoryMock.Setup(x => x.GetActiveByEmailAsync(email))
            .ReturnsAsync(existingPending);

        var handler = new RequestPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object,
            _signinLoggerMock.Object);

        var command = new RequestPasswordlessSigninCommand(email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Code.Should().Be(existingCode);
        _pendingSignupRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PendingSignup>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordlessSignin_WithEmailFailure_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var account = new Account { Id = "account-1", Email = email, DisplayName = "Test User" };

        _accountRepositoryMock.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(account);
        _pendingSignupRepositoryMock.Setup(x => x.GetActiveByEmailAsync(email))
            .ReturnsAsync((PendingSignup?)null);
        _pendingSignupRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PendingSignup>()))
            .Returns(Task.CompletedTask);
        _emailServiceMock.Setup(x => x.SendSigninCodeAsync(email, account.DisplayName, It.IsAny<string>()))
            .ReturnsAsync((false, "SMTP Error"));
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RequestPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object,
            _signinLoggerMock.Object);

        var command = new RequestPasswordlessSigninCommand(email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Failed to send");
    }

    #endregion

    #region VerifyPasswordlessSigninCommand Tests

    [Fact]
    public async Task VerifyPasswordlessSignin_WithValidCode_ReturnsAccountAndTokens()
    {
        // Arrange
        var email = "test@example.com";
        var code = "123456";
        var account = new Account
        {
            Id = "account-1",
            Auth0UserId = "auth0|123",
            Email = email,
            DisplayName = "Test User",
            Role = "user"
        };
        var pendingSignin = new PendingSignup
        {
            Email = email,
            Code = code,
            IsSignin = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            FailedAttempts = 0
        };

        _pendingSignupRepositoryMock.Setup(x => x.GetByEmailAndCodeAsync(email, code))
            .ReturnsAsync(pendingSignin);
        _accountRepositoryMock.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(account);
        _accountRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Account>()))
            .Returns(Task.CompletedTask);
        _pendingSignupRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PendingSignup>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(account.Auth0UserId, email, account.DisplayName, account.Role))
            .Returns("access-token-123");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token-456");

        var handler = new VerifyPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object,
            _verifyLoggerMock.Object);

        var command = new VerifyPasswordlessSigninCommand(email, code);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successful");
        result.Account.Should().NotBeNull();
        result.Account!.Id.Should().Be("account-1");
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("refresh-token-456");
    }

    [Fact]
    public async Task VerifyPasswordlessSignin_WithInvalidCode_IncrementsFailedAttempts()
    {
        // Arrange
        var email = "test@example.com";
        var wrongCode = "000000";
        var pendingSignin = new PendingSignup
        {
            Email = email,
            Code = "123456",
            IsSignin = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            FailedAttempts = 0
        };

        _pendingSignupRepositoryMock.Setup(x => x.GetByEmailAndCodeAsync(email, wrongCode))
            .ReturnsAsync((PendingSignup?)null);
        _pendingSignupRepositoryMock.Setup(x => x.GetActiveByEmailAsync(email))
            .ReturnsAsync(pendingSignin);
        _pendingSignupRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PendingSignup>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new VerifyPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object,
            _verifyLoggerMock.Object);

        var command = new VerifyPasswordlessSigninCommand(email, wrongCode);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
        pendingSignin.FailedAttempts.Should().Be(1);
        _pendingSignupRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<PendingSignup>()), Times.Once);
    }

    [Fact]
    public async Task VerifyPasswordlessSignin_WithExpiredCode_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var code = "123456";
        var expiredPending = new PendingSignup
        {
            Email = email,
            Code = code,
            IsSignin = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired
            IsUsed = false,
            FailedAttempts = 0
        };

        _pendingSignupRepositoryMock.Setup(x => x.GetByEmailAndCodeAsync(email, code))
            .ReturnsAsync(expiredPending);

        var handler = new VerifyPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object,
            _verifyLoggerMock.Object);

        var command = new VerifyPasswordlessSigninCommand(email, code);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expired");
        result.Account.Should().BeNull();
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task VerifyPasswordlessSignin_WithTooManyFailedAttempts_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var code = "123456";
        var pendingSignin = new PendingSignup
        {
            Email = email,
            Code = code,
            IsSignin = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            FailedAttempts = 5 // Max attempts reached
        };

        _pendingSignupRepositoryMock.Setup(x => x.GetByEmailAndCodeAsync(email, code))
            .ReturnsAsync(pendingSignin);

        var handler = new VerifyPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object,
            _verifyLoggerMock.Object);

        var command = new VerifyPasswordlessSigninCommand(email, code);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Too many failed attempts");
    }

    [Fact]
    public async Task VerifyPasswordlessSignin_WithNoPendingSignin_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var code = "123456";

        _pendingSignupRepositoryMock.Setup(x => x.GetByEmailAndCodeAsync(email, code))
            .ReturnsAsync((PendingSignup?)null);
        _pendingSignupRepositoryMock.Setup(x => x.GetActiveByEmailAsync(email))
            .ReturnsAsync((PendingSignup?)null);

        var handler = new VerifyPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object,
            _verifyLoggerMock.Object);

        var command = new VerifyPasswordlessSigninCommand(email, code);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid or expired");
    }

    [Fact]
    public async Task VerifyPasswordlessSignin_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var email = "test@example.com";
        var code = "123456";
        var account = new Account
        {
            Id = "account-1",
            Auth0UserId = "auth0|123",
            Email = email,
            DisplayName = "Test User",
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };
        var pendingSignin = new PendingSignup
        {
            Email = email,
            Code = code,
            IsSignin = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        Account? capturedAccount = null;
        _pendingSignupRepositoryMock.Setup(x => x.GetByEmailAndCodeAsync(email, code))
            .ReturnsAsync(pendingSignin);
        _accountRepositoryMock.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(account);
        _accountRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Account>()))
            .Callback<Account>(a => capturedAccount = a)
            .Returns(Task.CompletedTask);
        _pendingSignupRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<PendingSignup>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh");

        var handler = new VerifyPasswordlessSigninCommandHandler(
            _accountRepositoryMock.Object,
            _pendingSignupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object,
            _verifyLoggerMock.Object);

        var command = new VerifyPasswordlessSigninCommand(email, code);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedAccount.Should().NotBeNull();
        capturedAccount!.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion
}
