using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Api.Controllers;
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.CQRS.Accounts.Queries;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.App.Domain.Models;
using Wolverine;
using Xunit;

namespace Mystira.App.Api.Tests.Controllers;

public class AccountsControllerTests
{
    private readonly Mock<IMessageBus> _mockBus;
    private readonly Mock<ILogger<AccountsController>> _mockLogger;
    private readonly AccountsController _controller;

    public AccountsControllerTests()
    {
        _mockBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<AccountsController>>();
        _controller = new AccountsController(_mockBus.Object, _mockLogger.Object);
    }

    #region GetAccountByEmail Tests

    [Fact]
    public async Task GetAccountByEmail_WhenAccountExists_ReturnsOkWithAccount()
    {
        // Arrange
        var email = "test@example.com";
        var account = new Account { Id = "acc-1", Email = email };

        _mockBus
            .Setup(x => x.InvokeAsync<Account?>(
                It.IsAny<GetAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(account);

        // Act
        var result = await _controller.GetAccountByEmail(email);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAccount = okResult.Value.Should().BeOfType<Account>().Subject;
        returnedAccount.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetAccountByEmail_WhenAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var email = "notfound@example.com";

        _mockBus
            .Setup(x => x.InvokeAsync<Account?>(
                It.IsAny<GetAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(Account));

        // Act
        var result = await _controller.GetAccountByEmail(email);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAccountByEmail_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var email = "test@example.com";

        _mockBus
            .Setup(x => x.InvokeAsync<Account?>(
                It.IsAny<GetAccountByEmailQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAccountByEmail(email);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAccountById Tests

    [Fact]
    public async Task GetAccountById_WhenAccountExists_ReturnsOkWithAccount()
    {
        // Arrange
        var accountId = "acc-1";
        var account = new Account { Id = accountId, Email = "test@example.com" };

        _mockBus
            .Setup(x => x.InvokeAsync<Account?>(
                It.IsAny<GetAccountQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(account);

        // Act
        var result = await _controller.GetAccountById(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAccount = okResult.Value.Should().BeOfType<Account>().Subject;
        returnedAccount.Id.Should().Be(accountId);
    }

    [Fact]
    public async Task GetAccountById_WhenAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<Account?>(
                It.IsAny<GetAccountQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(Account));

        // Act
        var result = await _controller.GetAccountById(accountId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateAccount Tests

    [Fact]
    public async Task CreateAccount_WithValidRequest_ReturnsCreatedWithAccount()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Auth0UserId = "auth0|123",
            Email = "new@example.com",
            DisplayName = "New User"
        };
        var createdAccount = new Account { Id = "acc-new", Email = request.Email };

        _mockBus
            .Setup(x => x.InvokeAsync<Account>(
                It.IsAny<CreateAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(createdAccount);

        // Act
        var result = await _controller.CreateAccount(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AccountsController.GetAccountById));
        var returnedAccount = createdResult.Value.Should().BeOfType<Account>().Subject;
        returnedAccount.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task CreateAccount_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Auth0UserId = "auth0|123",
            Email = "",
            DisplayName = "New User"
        };

        // Act
        var result = await _controller.CreateAccount(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateAccount_WithMissingAuth0UserId_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Auth0UserId = "",
            Email = "test@example.com",
            DisplayName = "New User"
        };

        // Act
        var result = await _controller.CreateAccount(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateAccount_WhenDuplicateAccount_ReturnsConflict()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Auth0UserId = "auth0|123",
            Email = "existing@example.com",
            DisplayName = "New User"
        };

        _mockBus
            .Setup(x => x.InvokeAsync<Account>(
                It.IsAny<CreateAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new InvalidOperationException("Account already exists"));

        // Act
        var result = await _controller.CreateAccount(request);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    #endregion

    #region UpdateAccount Tests

    [Fact]
    public async Task UpdateAccount_WhenAccountExists_ReturnsOkWithUpdatedAccount()
    {
        // Arrange
        var accountId = "acc-1";
        var request = new UpdateAccountRequest { DisplayName = "Updated Name" };
        var updatedAccount = new Account { Id = accountId, DisplayName = "Updated Name" };

        _mockBus
            .Setup(x => x.InvokeAsync<Account?>(
                It.IsAny<UpdateAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(updatedAccount);

        // Act
        var result = await _controller.UpdateAccount(accountId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAccount = okResult.Value.Should().BeOfType<Account>().Subject;
        returnedAccount.DisplayName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAccount_WhenAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = "nonexistent";
        var request = new UpdateAccountRequest { DisplayName = "Updated Name" };

        _mockBus
            .Setup(x => x.InvokeAsync<Account?>(
                It.IsAny<UpdateAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(default(Account));

        // Act
        var result = await _controller.UpdateAccount(accountId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteAccount Tests

    [Fact]
    public async Task DeleteAccount_WhenAccountExists_ReturnsNoContent()
    {
        // Arrange
        var accountId = "acc-1";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<DeleteAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteAccount(accountId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteAccount_WhenAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = "nonexistent";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<DeleteAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteAccount(accountId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region LinkProfilesToAccount Tests

    [Fact]
    public async Task LinkProfilesToAccount_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var accountId = "acc-1";
        var request = new LinkProfilesRequest { UserProfileIds = new List<string> { "profile-1", "profile-2" } };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<LinkProfilesToAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.LinkProfilesToAccount(accountId, request);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task LinkProfilesToAccount_WithEmptyProfileIds_ReturnsBadRequest()
    {
        // Arrange
        var accountId = "acc-1";
        var request = new LinkProfilesRequest { UserProfileIds = new List<string>() };

        // Act
        var result = await _controller.LinkProfilesToAccount(accountId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task LinkProfilesToAccount_WhenAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = "nonexistent";
        var request = new LinkProfilesRequest { UserProfileIds = new List<string> { "profile-1" } };

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<LinkProfilesToAccountCommand>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.LinkProfilesToAccount(accountId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetAccountProfiles Tests

    [Fact]
    public async Task GetAccountProfiles_ReturnsOkWithProfiles()
    {
        // Arrange
        var accountId = "acc-1";
        var profiles = new List<UserProfile>
        {
            new UserProfile { Id = "profile-1", Name = "Player 1" },
            new UserProfile { Id = "profile-2", Name = "Player 2" }
        };

        _mockBus
            .Setup(x => x.InvokeAsync<List<UserProfile>>(
                It.IsAny<GetProfilesByAccountQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(profiles);

        // Act
        var result = await _controller.GetAccountProfiles(accountId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfiles = okResult.Value.Should().BeOfType<List<UserProfile>>().Subject;
        returnedProfiles.Should().HaveCount(2);
    }

    #endregion

    #region ValidateAccount Tests

    [Fact]
    public async Task ValidateAccount_WhenAccountExists_ReturnsTrue()
    {
        // Arrange
        var email = "existing@example.com";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<ValidateAccountQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateAccount(email);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(true);
    }

    [Fact]
    public async Task ValidateAccount_WhenAccountNotFound_ReturnsFalse()
    {
        // Arrange
        var email = "notfound@example.com";

        _mockBus
            .Setup(x => x.InvokeAsync<bool>(
                It.IsAny<ValidateAccountQuery>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateAccount(email);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(false);
    }

    #endregion
}
