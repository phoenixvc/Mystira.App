using Microsoft.Extensions.Logging;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Application.UseCases.Accounts;

/// <summary>
/// Use case for unlinking a user profile from an account
/// </summary>
public class RemoveUserProfileFromAccountUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveUserProfileFromAccountUseCase> _logger;

    public RemoveUserProfileFromAccountUseCase(
        IAccountRepository accountRepository,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveUserProfileFromAccountUseCase> logger)
    {
        _accountRepository = accountRepository;
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account> ExecuteAsync(string accountId, string profileId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile ID cannot be null or empty", nameof(profileId));
        }

        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account == null)
        {
            throw new ArgumentException($"Account not found: {accountId}", nameof(accountId));
        }

        var profile = await _userProfileRepository.GetByIdAsync(profileId);
        if (profile == null)
        {
            throw new ArgumentException($"User profile not found: {profileId}", nameof(profileId));
        }

        // Unlink profile from account
        if (profile.AccountId == accountId)
        {
            profile.AccountId = null;
            await _userProfileRepository.UpdateAsync(profile);
        }

        // Remove profile ID from account's profile list
        if (account.UserProfileIds.Contains(profileId))
        {
            account.UserProfileIds.Remove(profileId);
            await _accountRepository.UpdateAsync(account);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Unlinked profile {ProfileId} from account {AccountId}", profileId, accountId);
        return account;
    }
}

