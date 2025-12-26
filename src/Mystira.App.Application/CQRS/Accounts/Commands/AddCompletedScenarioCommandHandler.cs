using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Wolverine handler for AddCompletedScenarioCommand.
/// Marks a scenario as completed for an account.
/// Adds the scenario ID to the account's completed scenarios list if not already present.
/// Uses static method convention for cleaner, more testable code.
/// </summary>
public static class AddCompletedScenarioCommandHandler
{
    /// <summary>
    /// Handles the AddCompletedScenarioCommand by adding a scenario to the account's completed list.
    /// Wolverine injects dependencies as method parameters.
    /// </summary>
    public static async Task<bool> Handle(
        AddCompletedScenarioCommand command,
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
        {
            logger.LogWarning("Cannot add completed scenario: Account ID is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.ScenarioId))
        {
            logger.LogWarning("Cannot add completed scenario: Scenario ID is null or empty");
            return false;
        }

        var account = await repository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            logger.LogWarning("Account not found: {AccountId}", command.AccountId);
            return false;
        }

        // Initialize list if null
        if (account.CompletedScenarioIds == null)
        {
            account.CompletedScenarioIds = new List<string>();
        }

        // Add scenario if not already completed
        if (!account.CompletedScenarioIds.Contains(command.ScenarioId))
        {
            account.CompletedScenarioIds.Add(command.ScenarioId);
            await repository.UpdateAsync(account);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation("Added completed scenario {ScenarioId} to account {AccountId}",
                command.ScenarioId, command.AccountId);
        }
        else
        {
            logger.LogDebug("Scenario {ScenarioId} already marked as completed for account {AccountId}",
                command.ScenarioId, command.AccountId);
        }

        return true;
    }
}
