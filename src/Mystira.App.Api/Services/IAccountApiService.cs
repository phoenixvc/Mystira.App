using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing accounts in the API
/// </summary>
public interface IAccountApiService
{
    /// <summary>
    /// Get an account by email address
    /// </summary>
    Task<Account?> GetAccountByEmailAsync(string email);

    /// <summary>
    /// Get an account by ID
    /// </summary>
    Task<Account?> GetAccountByIdAsync(string accountId);

    /// <summary>
    /// Create a new account
    /// </summary>
    Task<Account> CreateAccountAsync(Account account);

    /// <summary>
    /// Update an existing account
    /// </summary>
    Task<Account> UpdateAccountAsync(Account account);

    /// <summary>
    /// Delete an account and all associated data
    /// </summary>
    Task<bool> DeleteAccountAsync(string accountId);

    /// <summary>
    /// Link user profiles to an account
    /// </summary>
    Task<bool> LinkUserProfilesToAccountAsync(string accountId, List<string> userProfileIds);

    /// <summary>
    /// Get all user profiles for an account
    /// </summary>
    Task<List<UserProfile>> GetUserProfilesForAccountAsync(string accountId);

    /// <summary>
    /// Validate that an account exists and is active
    /// </summary>
    Task<bool> ValidateAccountAsync(string email);

    /// <summary>
    /// Add a completed scenario to an account
    /// </summary>
    Task<bool> AddCompletedScenarioAsync(string accountId, string scenarioId);
}
