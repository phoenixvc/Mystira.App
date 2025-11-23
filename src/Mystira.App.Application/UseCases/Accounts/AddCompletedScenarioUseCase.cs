using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for marking a scenario as completed for an account
/// </summary>
public class AddCompletedScenarioUseCase
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddCompletedScenarioUseCase> _logger;

    public AddCompletedScenarioUseCase(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AddCompletedScenarioUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> ExecuteAsync(string accountId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(scenarioId));
        }

        var account = await _repository.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new ArgumentException($"Account not found: {accountId}", nameof(accountId));
        }

        if (account.CompletedScenarioIds == null)
        {
            account.CompletedScenarioIds = new List<string>();
        }

        if (!account.CompletedScenarioIds.Contains(scenarioId))
        {
            account.CompletedScenarioIds.Add(scenarioId);
            await _repository.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added completed scenario {ScenarioId} to account {AccountId}", scenarioId, accountId);
        }
        else
        {
            _logger.LogDebug("Scenario {ScenarioId} already marked as completed for account {AccountId}", scenarioId, accountId);
        }

        return account;
    }
}

