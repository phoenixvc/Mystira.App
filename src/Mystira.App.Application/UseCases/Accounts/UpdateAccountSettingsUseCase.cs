using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for updating account settings
/// </summary>
public class UpdateAccountSettingsUseCase
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAccountSettingsUseCase> _logger;

    public UpdateAccountSettingsUseCase(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAccountSettingsUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> ExecuteAsync(string accountId, AccountSettings settings)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var account = await _repository.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new ArgumentException($"Account not found: {accountId}", nameof(accountId));
        }

        account.Settings = settings;
        await _repository.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated settings for account: {AccountId}", accountId);
        return account;
    }
}

