using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Services;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountApiService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountApiService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Get account by email address
    /// </summary>
    [HttpGet("email/{email}")]
    public async Task<ActionResult<Account>> GetAccountByEmail(string email)
    {
        try
        {
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound($"Account with email {email} not found");
            }

            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by email {Email}", email);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get account by ID
    /// </summary>
    [HttpGet("{accountId}")]
    public async Task<ActionResult<Account>> GetAccountById(string accountId)
    {
        try
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null)
            {
                return NotFound($"Account with ID {accountId} not found");
            }

            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account by ID {AccountId}", accountId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new account
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Account>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required");
            }

            if (string.IsNullOrEmpty(request.Auth0UserId))
            {
                return BadRequest("Auth0 User ID is required");
            }

            var account = new Account
            {
                Auth0UserId = request.Auth0UserId,
                Email = request.Email,
                DisplayName = request.DisplayName ?? request.Email.Split('@')[0],
                UserProfileIds = request.UserProfileIds ?? new List<string>(),
                Subscription = request.Subscription ?? new SubscriptionDetails(),
                Settings = request.Settings ?? new AccountSettings()
            };

            var createdAccount = await _accountService.CreateAccountAsync(account);
            return CreatedAtAction(nameof(GetAccountById), new { accountId = createdAccount.Id }, createdAccount);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing account
    /// </summary>
    [HttpPut("{accountId}")]
    public async Task<ActionResult<Account>> UpdateAccount(string accountId, [FromBody] UpdateAccountRequest request)
    {
        try
        {
            var existingAccount = await _accountService.GetAccountByIdAsync(accountId);
            if (existingAccount == null)
            {
                return NotFound($"Account with ID {accountId} not found");
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.DisplayName))
            {
                existingAccount.DisplayName = request.DisplayName;
            }

            if (request.UserProfileIds != null)
            {
                existingAccount.UserProfileIds = request.UserProfileIds;
            }

            if (request.Subscription != null)
            {
                existingAccount.Subscription = request.Subscription;
            }

            if (request.Settings != null)
            {
                existingAccount.Settings = request.Settings;
            }

            var updatedAccount = await _accountService.UpdateAccountAsync(existingAccount);
            return Ok(updatedAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account {AccountId}", accountId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an account
    /// </summary>
    [HttpDelete("{accountId}")]
    public async Task<ActionResult> DeleteAccount(string accountId)
    {
        try
        {
            var success = await _accountService.DeleteAccountAsync(accountId);
            if (!success)
            {
                return NotFound($"Account with ID {accountId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account {AccountId}", accountId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Link user profiles to an account
    /// </summary>
    [HttpPost("{accountId}/profiles")]
    public async Task<ActionResult> LinkProfilesToAccount(string accountId, [FromBody] LinkProfilesRequest request)
    {
        try
        {
            if (request.UserProfileIds == null || !request.UserProfileIds.Any())
            {
                return BadRequest("User profile IDs are required");
            }

            var success = await _accountService.LinkUserProfilesToAccountAsync(accountId, request.UserProfileIds);
            if (!success)
            {
                return NotFound($"Account with ID {accountId} not found");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking profiles to account {AccountId}", accountId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all user profiles for an account
    /// </summary>
    [HttpGet("{accountId}/profiles")]
    public async Task<ActionResult<List<UserProfile>>> GetAccountProfiles(string accountId)
    {
        try
        {
            var profiles = await _accountService.GetUserProfilesForAccountAsync(accountId);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for account {AccountId}", accountId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate account exists
    /// </summary>
    [HttpGet("validate/{email}")]
    public async Task<ActionResult<bool>> ValidateAccount(string email)
    {
        try
        {
            var isValid = await _accountService.ValidateAccountAsync(email);
            return Ok(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating account {Email}", email);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request model for creating a new account
/// </summary>
public class CreateAccountRequest
{
    public string Auth0UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public List<string>? UserProfileIds { get; set; }
    public SubscriptionDetails? Subscription { get; set; }
    public AccountSettings? Settings { get; set; }
}

/// <summary>
/// Request model for updating an account
/// </summary>
public class UpdateAccountRequest
{
    public string? DisplayName { get; set; }
    public List<string>? UserProfileIds { get; set; }
    public SubscriptionDetails? Subscription { get; set; }
    public AccountSettings? Settings { get; set; }
}

/// <summary>
/// Request model for linking profiles to an account
/// </summary>
public class LinkProfilesRequest
{
    public List<string> UserProfileIds { get; set; } = new();
}
