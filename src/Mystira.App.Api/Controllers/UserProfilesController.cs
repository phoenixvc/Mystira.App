using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.UserProfiles.Commands;
using Mystira.App.Application.CQRS.UserProfiles.Queries;
using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for user profile management.
/// Follows hexagonal architecture - uses only IMediator (CQRS pattern).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserProfilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserProfilesController> _logger;

    public UserProfilesController(
        IMediator mediator,
        ILogger<UserProfilesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new User profile
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserProfile>> CreateProfile([FromBody] CreateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var command = new CreateUserProfileCommand(request);
            var profile = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetProfileById), new { id = profile.Id }, profile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating profile");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while creating profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all profiles for an account
    /// </summary>
    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<List<UserProfile>>> GetProfilesByAccount(string accountId)
    {
        try
        {
            _logger.LogInformation("Getting profiles for account {AccountId}", accountId);

            var query = new GetProfilesByAccountQuery(accountId);
            var profiles = await _mediator.Send(query);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for account {AccountId}", accountId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching profiles",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get a User profile by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfile>> GetProfileById(string id)
    {
        try
        {
            var query = new GetUserProfileQuery(id);
            var profile = await _mediator.Send(query);
            if (profile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update a User profile by ID
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserProfile>> UpdateProfile(string id, [FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var command = new UpdateUserProfileCommand(id, request);
            var updatedProfile = await _mediator.Send(command);
            if (updatedProfile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(updatedProfile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating profile {Id}", id);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while updating profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update a User profile by ID
    /// </summary>
    [HttpPut("id/{profileId}")]
    public async Task<ActionResult<UserProfile>> UpdateProfileById(string profileId, [FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var command = new UpdateUserProfileCommand(profileId, request);
            var updatedProfile = await _mediator.Send(command);
            if (updatedProfile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile not found: {profileId}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(updatedProfile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating profile {ProfileId}", profileId);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", profileId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while updating profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a User profile and all associated data (COPPA compliance)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProfile(string id)
    {
        try
        {
            var command = new DeleteUserProfileCommand(id);
            var deleted = await _mediator.Send(command);
            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while deleting profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Mark onboarding as complete for a User
    /// </summary>
    [HttpPost("{id}/complete-onboarding")]
    public async Task<ActionResult> CompleteOnboarding(string id)
    {
        try
        {
            var command = new CompleteOnboardingCommand(id);
            var success = await _mediator.Send(command);
            if (!success)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new { message = "Onboarding completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding for {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while completing onboarding",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create multiple profiles for onboarding
    /// </summary>
    [HttpPost("batch")]
    [Authorize]
    public async Task<ActionResult<List<UserProfile>>> CreateMultipleProfiles([FromBody] CreateMultipleProfilesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var command = new CreateMultipleProfilesCommand(request);
            var profiles = await _mediator.Send(command);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple profiles");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while creating multiple profiles",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Assign a character to a profile
    /// </summary>
    [HttpPost("{profileId}/assign-character")]
    public async Task<ActionResult> AssignCharacterToProfile(string profileId, [FromBody] ProfileAssignmentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    ValidationErrors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToList() ?? new List<string>()
                    ),
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var command = new AssignCharacterToProfileCommand(
                request.ProfileId,
                request.CharacterId,
                request.IsNpcAssignment);
            var success = await _mediator.Send(command);

            if (!success)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile or character not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new { message = "Character assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning character {CharacterId} to profile {ProfileId}",
                request.CharacterId, request.ProfileId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while assigning character",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Remove a profile from an account
    /// </summary>
    [HttpDelete("{profileId}/account")]
    [Authorize]
    public async Task<ActionResult> RemoveProfileFromAccount(string profileId)
    {
        try
        {
            var command = new RemoveProfileFromAccountCommand(profileId);
            var success = await _mediator.Send(command);

            if (!success)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile with ID {profileId} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new { message = "Profile removed from account successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile {ProfileId} from account", profileId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while removing profile from account",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
