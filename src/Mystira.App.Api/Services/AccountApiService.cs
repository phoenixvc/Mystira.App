using Mystira.App.Application.UseCases.Accounts;
using Mystira.App.Contracts.Requests.Accounts;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// API service for account operations - delegates to use cases
/// </summary>
public class AccountApiService : IAccountApiService
{
    private readonly GetAccountByEmailUseCase _getAccountByEmailUseCase;
    private readonly GetAccountUseCase _getAccountUseCase;
    private readonly CreateAccountUseCase _createAccountUseCase;
    private readonly UpdateAccountUseCase _updateAccountUseCase;
    private readonly AddUserProfileToAccountUseCase _addUserProfileUseCase;
    private readonly RemoveUserProfileFromAccountUseCase _removeUserProfileUseCase;
    private readonly AddCompletedScenarioUseCase _addCompletedScenarioUseCase;
    private readonly ILogger<AccountApiService> _logger;

    public AccountApiService(
        GetAccountByEmailUseCase getAccountByEmailUseCase,
        GetAccountUseCase getAccountUseCase,
        CreateAccountUseCase createAccountUseCase,
        UpdateAccountUseCase updateAccountUseCase,
        AddUserProfileToAccountUseCase addUserProfileUseCase,
        RemoveUserProfileFromAccountUseCase removeUserProfileUseCase,
        AddCompletedScenarioUseCase addCompletedScenarioUseCase,
        ILogger<AccountApiService> logger)
    {
        _getAccountByEmailUseCase = getAccountByEmailUseCase;
        _getAccountUseCase = getAccountUseCase;
        _createAccountUseCase = createAccountUseCase;
        _updateAccountUseCase = updateAccountUseCase;
        _addUserProfileUseCase = addUserProfileUseCase;
        _removeUserProfileUseCase = removeUserProfileUseCase;
        _addCompletedScenarioUseCase = addCompletedScenarioUseCase;
        _logger = logger;
    }

    public async Task<Account?> GetAccountByEmailAsync(string email)
    {
        try
        {
            return await _getAccountByEmailUseCase.ExecuteAsync(email);
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
            return await _getAccountUseCase.ExecuteAsync(accountId);
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
            var request = new CreateAccountRequest
            {
                Email = account.Email,
                DisplayName = account.DisplayName,
                Subscription = account.Subscription,
                Settings = account.Settings
            };

            return await _createAccountUseCase.ExecuteAsync(request);
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
            var request = new UpdateAccountRequest
            {
                Id = account.Id,
                DisplayName = account.DisplayName,
                UserProfileIds = account.UserProfileIds,
                Subscription = account.Subscription,
                Settings = account.Settings
            };

            return await _updateAccountUseCase.ExecuteAsync(request);
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
            // Note: DeleteAccountUseCase doesn't exist yet - would need to be created
            // For now, this method should be deprecated or throw NotImplementedException
            throw new NotImplementedException("DeleteAccountUseCase needs to be created");
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
            foreach (var profileId in userProfileIds)
            {
                await _addUserProfileUseCase.ExecuteAsync(accountId, profileId);
            }

            _logger.LogInformation("Linked {ProfileCount} profiles to account {AccountId}",
                userProfileIds.Count, accountId);
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
            // Note: This would need a GetUserProfilesByAccountUseCase
            // For now, this should be deprecated or throw NotImplementedException
            throw new NotImplementedException("GetUserProfilesByAccountUseCase needs to be created");
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
            return await _addCompletedScenarioUseCase.ExecuteAsync(accountId, scenarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding completed scenario {ScenarioId} for account {AccountId}",
                scenarioId, accountId);
            return false;
        }
    }
}
