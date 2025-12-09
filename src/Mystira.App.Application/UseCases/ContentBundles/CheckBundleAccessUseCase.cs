using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Application.UseCases.ContentBundles;

/// <summary>
/// Use case for checking if an account has access to a content bundle
/// </summary>
public class CheckBundleAccessUseCase
{
    private readonly IContentBundleRepository _bundleRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<CheckBundleAccessUseCase> _logger;

    public CheckBundleAccessUseCase(
        IContentBundleRepository bundleRepository,
        IAccountRepository accountRepository,
        ILogger<CheckBundleAccessUseCase> logger)
    {
        _bundleRepository = bundleRepository;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(string accountId, string bundleId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(bundleId))
        {
            throw new ArgumentException("Bundle ID cannot be null or empty", nameof(bundleId));
        }

        var bundle = await _bundleRepository.GetByIdAsync(bundleId);
        if (bundle == null)
        {
            _logger.LogWarning("Content bundle not found: {BundleId}", bundleId);
            return false;
        }

        // Free bundles are accessible to all
        if (bundle.IsFree)
        {
            _logger.LogDebug("Bundle {BundleId} is free, access granted", bundleId);
            return true;
        }

        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account == null)
        {
            _logger.LogWarning("Account not found: {AccountId}", accountId);
            return false;
        }

        // Check if account has purchased this bundle
        if (account.Subscription?.PurchasedScenarios != null &&
            account.Subscription.PurchasedScenarios.Any(s => bundle.ScenarioIds.Contains(s)))
        {
            _logger.LogDebug("Account {AccountId} has purchased access to bundle {BundleId}", accountId, bundleId);
            return true;
        }

        // Check subscription access
        if (account.Subscription?.IsActive == true)
        {
            // Subscription grants access to all bundles
            _logger.LogDebug("Account {AccountId} has active subscription, access granted to bundle {BundleId}", accountId, bundleId);
            return true;
        }

        _logger.LogDebug("Account {AccountId} does not have access to bundle {BundleId}", accountId, bundleId);
        return false;
    }
}

