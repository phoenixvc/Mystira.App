using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.CQRS.Accounts.Queries;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for account management.
/// Follows hexagonal architecture - uses only IMediator (CQRS pattern).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IMediator mediator, ILogger<AccountsController> logger)
    {
        _mediator = mediator;
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
            var query = new GetAccountByEmailQuery(email);
            var account = await _mediator.Send(query);
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
            var query = new GetAccountQuery(accountId);
            var account = await _mediator.Send(query);
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

            var command = new CreateAccountCommand(
                request.Auth0UserId,
                request.Email,
                request.DisplayName,
                request.UserProfileIds,
                request.Subscription,
                request.Settings);

            var createdAccount = await _mediator.Send(command);
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
            var command = new UpdateAccountCommand(
                accountId,
                request.DisplayName,
                request.UserProfileIds,
                request.Subscription,
                request.Settings);

            var updatedAccount = await _mediator.Send(command);
            if (updatedAccount == null)
            {
                return NotFound($"Account with ID {accountId} not found");
            }

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
            var command = new DeleteAccountCommand(accountId);
            var success = await _mediator.Send(command);
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

            var command = new LinkProfilesToAccountCommand(accountId, request.UserProfileIds);
            var success = await _mediator.Send(command);
            if (!success)
            {
                return NotFound($"Account with ID {accountId} not found or no profiles were linked");
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
            var query = new GetProfilesByAccountQuery(accountId);
            var profiles = await _mediator.Send(query);
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
            var query = new ValidateAccountQuery(email);
            var isValid = await _mediator.Send(query);
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
