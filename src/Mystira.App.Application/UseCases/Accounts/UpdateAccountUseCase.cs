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
            account.Settings = new AccountSettings
            {
                Id = account.Settings?.Id ?? Guid.NewGuid().ToString(),
                CacheCredentials = request.Settings.CacheCredentials,
                RequireAuthOnStartup = request.Settings.RequireAuthOnStartup,
                PreferredLanguage = request.Settings.PreferredLanguage,
                NotificationsEnabled = request.Settings.NotificationsEnabled,
                Theme = request.Settings.Theme
            };
        }

        account.LastLoginAt = DateTime.UtcNow;

        await _repository.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated account: {AccountId}", accountId);
        return account;
    }
}

