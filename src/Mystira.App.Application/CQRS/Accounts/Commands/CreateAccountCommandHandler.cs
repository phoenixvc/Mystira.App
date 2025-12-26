using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Wolverine handler for CreateAccountCommand.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class CreateAccountCommandHandler
{
    /// <summary>
    /// Handles the CreateAccountCommand by creating a new account in the repository.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<Account> Handle(
        CreateAccountCommand command,
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        // Check if account already exists
        var existing = await repository.GetByEmailAsync(command.Email);
        if (existing != null)
        {
            throw new InvalidOperationException($"Account with email {command.Email} already exists");
        }

        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Auth0UserId = command.Auth0UserId,
            Email = command.Email,
            DisplayName = command.DisplayName ?? command.Email.Split('@')[0],
            UserProfileIds = command.UserProfileIds ?? new List<string>(),
            Subscription = command.Subscription ?? new SubscriptionDetails(),
            Settings = command.Settings ?? new AccountSettings(),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await repository.AddAsync(account);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Created account {AccountId} for email {Email}", account.Id, account.Email);
        return account;
    }
}
