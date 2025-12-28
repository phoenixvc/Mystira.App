using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Wolverine handler for DeleteAccountCommand.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class DeleteAccountCommandHandler
{
    /// <summary>
    /// Handles the DeleteAccountCommand by deleting an account from the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        DeleteAccountCommand command,
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        var account = await repository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            logger.LogWarning("Account not found for deletion request");
            return false;
        }

        await repository.DeleteAsync(account.Id);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Account deleted successfully");
        return true;
    }
}
