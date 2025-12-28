using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.CQRS.Accounts;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger> _logger;

    public DeleteAccountCommandHandlerTests()
    {
        _repository = new Mock<IAccountRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger>();
    }

    [Fact]
    public async Task Handle_WithExistingAccount_DeletesAndReturnsTrue()
    {
        // Arrange
        var accountId = "acc-123";
        var existingAccount = new Account
        {
            Id = accountId,
            Email = "test@example.com",
            ExternalUserId = "ext-123"
        };

        _repository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(existingAccount);

        var command = new DeleteAccountCommand(accountId);

        // Act
        var result = await DeleteAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repository.Verify(r => r.DeleteAsync(accountId), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentAccount_ReturnsFalse()
    {
        // Arrange
        var accountId = "nonexistent-123";

        _repository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        var command = new DeleteAccountCommand(accountId);

        // Act
        var result = await DeleteAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repository.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingAccount_UsesCorrectAccountId()
    {
        // Arrange
        var accountId = "specific-acc-456";
        var existingAccount = new Account
        {
            Id = accountId,
            Email = "specific@example.com"
        };

        string? deletedAccountId = null;
        _repository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(existingAccount);
        _repository.Setup(r => r.DeleteAsync(It.IsAny<string>()))
            .Callback<string>(id => deletedAccountId = id)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteAccountCommand(accountId);

        // Act
        await DeleteAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        deletedAccountId.Should().Be(accountId);
    }

    [Fact]
    public async Task Handle_SavesChangesOnlyAfterDelete()
    {
        // Arrange
        var accountId = "acc-789";
        var existingAccount = new Account { Id = accountId };
        var callOrder = new List<string>();

        _repository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(existingAccount);
        _repository.Setup(r => r.DeleteAsync(It.IsAny<string>()))
            .Callback(() => callOrder.Add("delete"))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);

        var command = new DeleteAccountCommand(accountId);

        // Act
        await DeleteAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            CancellationToken.None);

        // Assert
        callOrder.Should().BeEquivalentTo(new[] { "delete", "save" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToSaveChanges()
    {
        // Arrange
        var accountId = "acc-token";
        var existingAccount = new Account { Id = accountId };
        using var cts = new CancellationTokenSource();

        CancellationToken? capturedToken = null;
        _repository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync(existingAccount);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => capturedToken = ct)
            .ReturnsAsync(1);

        var command = new DeleteAccountCommand(accountId);

        // Act
        await DeleteAccountCommandHandler.Handle(
            command,
            _repository.Object,
            _unitOfWork.Object,
            _logger.Object,
            cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }
}
