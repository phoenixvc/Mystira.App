using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

public class AccountApiService : IAccountApiService
{
    private readonly IAccountRepository _repository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountApiService> _logger;

    public AccountApiService(
        IAccountRepository repository,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILogger<AccountApiService> logger)
    {
        _repository = repository;
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Account?> GetAccountByEmailAsync(string email)
    {
        try
        {
            return await _repository.GetByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by email {Email}", email);
            return null;
        }
    }

    public async Task<Account?> GetAccountByIdAsync(string accountId)
    {
        try
        {
            return await _repository.GetByIdAsync(accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by ID {AccountId}", accountId);
            return null;
        }
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
        try
        {
            // Check if account with email already exists
            var existingAccount = await GetAccountByEmailAsync(account.Email);
            if (existingAccount != null)
            {
                throw new InvalidOperationException($"Account with email {account.Email} already exists");
            }

            // Ensure ID is set
            if (string.IsNullOrEmpty(account.Id))
            {
                account.Id = Guid.NewGuid().ToString();
            }

            account.CreatedAt = DateTime.UtcNow;
            account.LastLoginAt = DateTime.UtcNow;

            await _repository.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created new account for {Email}", account.Email);
            return account;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for {Email}", account.Email);
            throw;
        }
    }

    public async Task<Account> UpdateAccountAsync(Account account)
    {
        try
        {
            var existingAccount = await GetAccountByIdAsync(account.Id);
            if (existingAccount == null)
            {
                throw new InvalidOperationException($"Account with ID {account.Id} not found");
            }

            // Update properties
            existingAccount.DisplayName = account.DisplayName;
            existingAccount.UserProfileIds = account.UserProfileIds;
            existingAccount.Subscription = account.Subscription;
            existingAccount.Settings = account.Settings;
            existingAccount.LastLoginAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existingAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated account {AccountId}", account.Id);
            return existingAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAccountAsync(string accountId)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
            {
                return false;
            }

            // Unlink all user profiles from this account
            var userProfiles = await _userProfileRepository.GetByAccountIdAsync(accountId);
            foreach (var profile in userProfiles)
            {
                profile.AccountId = null;
                await _userProfileRepository.UpdateAsync(profile);
            }

            // Remove the account
            await _repository.DeleteAsync(accountId);
            await _unitOfWork.SaveChangesAsync();

            var profileList = userProfiles.ToList();
            _logger.LogInformation("Deleted account {AccountId} and unlinked {ProfileCount} profiles",
                accountId, profileList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account {AccountId}", accountId);
            return false;
        }
    }

    public async Task<bool> LinkUserProfilesToAccountAsync(string accountId, List<string> userProfileIds)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
            {
                return false;
            }

            var profiles = new List<UserProfile>();
            foreach (var profileId in userProfileIds)
            {
                var profile = await _userProfileRepository.GetByIdAsync(profileId);
                if (profile != null)
                {
                    profile.AccountId = accountId;
                    await _userProfileRepository.UpdateAsync(profile);
                    profiles.Add(profile);
                }
            }

            // Update account's profile list
            account.UserProfileIds = userProfileIds;
            await _repository.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Linked {ProfileCount} profiles to account {AccountId}",
                profiles.Count, accountId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking profiles to account {AccountId}", accountId);
            return false;
        }
    }

    public async Task<List<UserProfile>> GetUserProfilesForAccountAsync(string accountId)
    {
        try
        {
            var profiles = await _userProfileRepository.GetByAccountIdAsync(accountId);
            return profiles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for account {AccountId}", accountId);
            return new List<UserProfile>();
        }
    }

    public async Task<bool> ValidateAccountAsync(string email)
    {
        try
        {
            var account = await GetAccountByEmailAsync(email);
            return account != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating account {Email}", email);
            return false;
        }
    }

    public async Task<bool> AddCompletedScenarioAsync(string accountId, string scenarioId)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
            {
                _logger.LogWarning("Account not found: {AccountId}", accountId);
                return false;
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
                _logger.LogInformation("Added completed scenario {ScenarioId} to account {AccountId}",
                    scenarioId, accountId);
            }
            else
            {
                _logger.LogInformation("Scenario {ScenarioId} already marked as completed for account {AccountId}",
                    scenarioId, accountId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding completed scenario {ScenarioId} for account {AccountId}",
                scenarioId, accountId);
            return false;
        }
    }
}
