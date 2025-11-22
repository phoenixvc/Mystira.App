using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.Authentication;

/// <summary>
/// Use case for completing a pending signup and creating an account
/// </summary>
public class CompletePendingSignupUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPendingSignupRepository _pendingSignupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompletePendingSignupUseCase> _logger;

    public CompletePendingSignupUseCase(
        IAccountRepository accountRepository,
        IPendingSignupRepository pendingSignupRepository,
        IUnitOfWork unitOfWork,
        ILogger<CompletePendingSignupUseCase> logger)
    {
        _accountRepository = accountRepository;
        _pendingSignupRepository = pendingSignupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> ExecuteAsync(PendingSignup pendingSignup, string? auth0UserId = null)
    {
        if (pendingSignup == null)
        {
            throw new ArgumentNullException(nameof(pendingSignup));
        }

        if (pendingSignup.IsUsed)
        {
            throw new InvalidOperationException("Pending signup has already been used");
        }

        if (pendingSignup.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Pending signup has expired");
        }

        // Create account
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Auth0UserId = auth0UserId ?? $"auth0|{Guid.NewGuid():N}",
            Email = pendingSignup.Email,
            DisplayName = pendingSignup.DisplayName,
            Role = "Guest",
            UserProfileIds = new List<string>(),
            CompletedScenarioIds = new List<string>(),
            Subscription = new SubscriptionDetails(),
            Settings = new AccountSettings(),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _accountRepository.AddAsync(account);

        // Mark pending signup as used
        pendingSignup.IsUsed = true;
        await _pendingSignupRepository.UpdateAsync(pendingSignup);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Completed signup and created account: {AccountId} for email: {Email}",
            account.Id, account.Email);
        return account;
    }
}

