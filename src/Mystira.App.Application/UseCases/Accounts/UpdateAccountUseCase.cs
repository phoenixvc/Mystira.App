using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.Accounts;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for updating account details
/// </summary>
public class UpdateAccountUseCase
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAccountUseCase> _logger;

    public UpdateAccountUseCase(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAccountUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> ExecuteAsync(string accountId, UpdateAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var account = await _repository.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new ArgumentException($"Account not found: {accountId}", nameof(accountId));
        }

        // Update properties if provided
        if (request.DisplayName != null)
        {
            account.DisplayName = request.DisplayName;
        }

        if (request.Settings != null)
        {
            // Map from Contracts AccountSettings to Domain AccountSettings
            // Only update properties that exist in the Contracts type
            account.Settings ??= new AccountSettings();
            account.Settings.PreferredLanguage = request.Settings.PreferredLanguage ?? account.Settings.PreferredLanguage;
            account.Settings.NotificationsEnabled = request.Settings.NotificationsEnabled ?? account.Settings.NotificationsEnabled;
            account.Settings.Theme = request.Settings.Theme ?? account.Settings.Theme;
        }

        account.LastLoginAt = DateTime.UtcNow;

        await _repository.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated account: {AccountId}", accountId);
        return account;
    }
}

