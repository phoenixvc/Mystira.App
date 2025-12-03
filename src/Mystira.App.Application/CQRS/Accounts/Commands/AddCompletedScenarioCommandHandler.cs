using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.CQRS.Accounts.Commands;

/// <summary>
/// Handler for marking a scenario as completed for an account.
/// Adds the scenario ID to the account's completed scenarios list if not already present.
/// </summary>
public class AddCompletedScenarioCommandHandler : ICommandHandler<AddCompletedScenarioCommand, bool>
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddCompletedScenarioCommandHandler> _logger;

    public AddCompletedScenarioCommandHandler(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AddCompletedScenarioCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        AddCompletedScenarioCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.AccountId))
        {
            _logger.LogWarning("Cannot add completed scenario: Account ID is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.ScenarioId))
        {
            _logger.LogWarning("Cannot add completed scenario: Scenario ID is null or empty");
            return false;
        }

        var account = await _repository.GetByIdAsync(command.AccountId);
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", command.AccountId);
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
            await _repository.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added completed scenario {ScenarioId} to account {AccountId}",
                command.ScenarioId, command.AccountId);
        }
        else
        {
            _logger.LogDebug("Scenario {ScenarioId} already marked as completed for account {AccountId}",
                command.ScenarioId, command.AccountId);
        }

        return true;
    }
}
