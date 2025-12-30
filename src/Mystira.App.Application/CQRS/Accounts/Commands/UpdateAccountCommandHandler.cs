using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Wolverine handler for UpdateAccountCommand.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class UpdateAccountCommandHandler
{
    /// <summary>
    /// Handles the UpdateAccountCommand by updating an existing account in the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Account?> Handle(
        UpdateAccountCommand command,
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        var account = await repository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            logger.LogWarning("Account not found: {AccountId}", command.AccountId);
            return null;
        }

        if (!string.IsNullOrEmpty(command.DisplayName))
        {
            account.DisplayName = command.DisplayName;
        }

        if (command.UserProfileIds != null)
        {
            account.UserProfileIds = command.UserProfileIds;
        }

        if (command.Subscription != null)
        {
            account.Subscription = command.Subscription;
        }

        if (command.Settings != null)
        {
            account.Settings = command.Settings;
        }

        await repository.UpdateAsync(account);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Updated account {AccountId}", account.Id);
        return account;
    }
}
