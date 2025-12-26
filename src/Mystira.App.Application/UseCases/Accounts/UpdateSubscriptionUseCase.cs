using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.Accounts;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for updating subscription details
/// </summary>
public class UpdateSubscriptionUseCase
{
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateSubscriptionUseCase> _logger;

    public UpdateSubscriptionUseCase(
        IAccountRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSubscriptionUseCase> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> ExecuteAsync(string accountId, UpdateSubscriptionRequest request)
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

        // Update subscription properties - convert from Contracts enum to Domain enum
        account.Subscription.Type = (SubscriptionType)(int)request.Type;

        if (request.ProductId != null)
        {
            account.Subscription.ProductId = request.ProductId;
        }

        if (request.ValidUntil.HasValue)
        {
            account.Subscription.ValidUntil = request.ValidUntil;
        }

        if (request.PurchaseToken != null)
        {
            account.Subscription.PurchaseToken = request.PurchaseToken;
        }

        if (request.PurchasedScenarios != null)
        {
            account.Subscription.PurchasedScenarios = request.PurchasedScenarios;
        }

        account.Subscription.LastVerified = DateTime.UtcNow;

        await _repository.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated subscription for account: {AccountId}", accountId);
        return account;
    }
}

