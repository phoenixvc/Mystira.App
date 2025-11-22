using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Contracts.Requests.UserProfiles;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserProfilesController : ControllerBase
{
    private readonly IUserProfileApiService _profileService;
    private readonly IAccountApiService _accountService;
    private readonly ILogger<UserProfilesController> _logger;

    public UserProfilesController(
        IUserProfileApiService profileService,
        IAccountApiService accountService,
        ILogger<UserProfilesController> logger)
    {
        _profileService = profileService;
        _accountService = accountService;
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

            var profile = await _profileService.CreateProfileAsync(request);
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

            var profiles = await _accountService.GetUserProfilesForAccountAsync(accountId);
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
            var profile = await _profileService.GetProfileByIdAsync(id);
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

            var updatedProfile = await _profileService.UpdateProfileByIdAsync(id, request);
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

            var updatedProfile = await _profileService.UpdateProfileByIdAsync(profileId, request);
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
            var profile = await _profileService.GetProfileByIdAsync(id);
            if (profile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var deleted = await _profileService.DeleteProfileAsync(profile.Id);
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
            var profile = await _profileService.GetProfileByIdAsync(id);
            if (profile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var success = await _profileService.CompleteOnboardingAsync(profile.Name);
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

            var profiles = await _profileService.CreateMultipleProfilesAsync(request);
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

            var success = await _profileService.AssignCharacterToProfileAsync(
                request.ProfileId, request.CharacterId, request.IsNpcAssignment);

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
            var profile = await _profileService.GetProfileByIdAsync(profileId);
            if (profile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Profile with ID {profileId} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (!string.IsNullOrEmpty(profile.AccountId))
            {
                // Remove profile from account
                var account = await _accountService.GetAccountByIdAsync(profile.AccountId);
                if (account != null)
                {
                    account.UserProfileIds.Remove(profileId);
                    await _accountService.UpdateAccountAsync(account);
                }

                // Clear profile's account ID
                await _profileService.UpdateProfileByIdAsync(profileId, new UpdateUserProfileRequest
                {
                    AccountId = null
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
