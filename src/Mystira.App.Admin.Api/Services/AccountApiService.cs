using Mystira.App.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.App.Admin.Api.Data;

namespace Mystira.App.Admin.Api.Services;

public class AccountApiService : IAccountApiService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<AccountApiService> _logger;

    public AccountApiService(MystiraAppDbContext context, ILogger<AccountApiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Account?> GetAccountByEmailAsync(string email)
    {
        try
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower());
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
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId);
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

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

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

            await _context.SaveChangesAsync();

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
            var userProfiles = await _context.UserProfiles
                .Where(up => up.AccountId == accountId)
                .ToListAsync();

            foreach (var profile in userProfiles)
            {
                profile.AccountId = null;
            }

            // Remove the account
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted account {AccountId} and unlinked {ProfileCount} profiles",
                accountId, userProfiles.Count);
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

            var profiles = await _context.UserProfiles
                .Where(up => userProfileIds.Contains(up.Id))
                .ToListAsync();

            foreach (var profile in profiles)
            {
                profile.AccountId = accountId;
            }

            // Update account's profile list
            account.UserProfileIds = userProfileIds;
            await _context.SaveChangesAsync();

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
            return await _context.UserProfiles
                .Where(up => up.AccountId == accountId)
                .ToListAsync();
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
}