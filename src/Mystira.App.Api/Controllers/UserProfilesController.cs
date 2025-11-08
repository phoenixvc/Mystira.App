using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

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
    /// Create a new DM profile
    /// </summary>
    [HttpPost]
    [Authorize]
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
            return CreatedAtAction(nameof(GetProfile), new { name = profile.Name }, profile);
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
    /// Get a DM profile by name
    /// </summary>
    [HttpGet("{name}")]
    [Authorize] // Requires authentication
    public async Task<ActionResult<UserProfile>> GetProfile(string name)
    {
        try
        {
            var profile = await _profileService.GetProfileAsync(name);
            if (profile == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Profile not found: {name}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {Name}", name);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update a DM profile
    /// </summary>
    [HttpPut("{name}")]
    [Authorize] // Requires authentication
    public async Task<ActionResult<UserProfile>> UpdateProfile(string name, [FromBody] UpdateUserProfileRequest request)
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

            var profile = await _profileService.UpdateProfileAsync(name, request);
            if (profile == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Profile not found: {name}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating profile {Name}", name);
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {Name}", name);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a DM profile and all associated data (COPPA compliance)
    /// </summary>
    [HttpDelete("{name}")]
    [Authorize] // Requires authentication
    public async Task<ActionResult> DeleteProfile(string name)
    {
        try
        {
            var deleted = await _profileService.DeleteProfileAsync(name);
            if (!deleted)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Profile not found: {name}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {Name}", name);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while deleting profile",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Mark onboarding as complete for a DM
    /// </summary>
    [HttpPost("{name}/complete-onboarding")]
    [Authorize] // Requires authentication
    public async Task<ActionResult> CompleteOnboarding(string name)
    {
        try
        {
            var success = await _profileService.CompleteOnboardingAsync(name);
            if (!success)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Profile not found: {name}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new { message = "Onboarding completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding for {Name}", name);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while completing onboarding",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all DM profiles (admin endpoint)
    /// </summary>
    [HttpGet]
    [Authorize] // Requires authentication - could add admin role check
    public async Task<ActionResult<List<UserProfile>>> GetAllProfiles()
    {
        try
        {
            var profiles = await _profileService.GetAllProfilesAsync();
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all profiles");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching profiles",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create a guest profile with optional name
    /// </summary>
    [HttpPost("guest")]
    public async Task<ActionResult<UserProfile>> CreateGuestProfile([FromBody] CreateGuestProfileRequest request)
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

            var profile = await _profileService.CreateGuestProfileAsync(request);
            return CreatedAtAction(nameof(GetProfile), new { name = profile.Name }, profile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating guest profile");
            return BadRequest(new ErrorResponse 
            { 
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest profile");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while creating guest profile",
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
    /// Get non-guest profiles
    /// </summary>
    [HttpGet("non-guest")]
    [Authorize]
    public async Task<ActionResult<List<UserProfile>>> GetNonGuestProfiles()
    {
        try
        {
            var profiles = await _profileService.GetNonGuestProfilesAsync();
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting non-guest profiles");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching non-guest profiles",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get guest profiles
    /// </summary>
    [HttpGet("guest")]
    [Authorize]
    public async Task<ActionResult<List<UserProfile>>> GetGuestProfiles()
    {
        try
        {
            var profiles = await _profileService.GetGuestProfilesAsync();
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guest profiles");
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while fetching guest profiles",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Assign a character to a profile
    /// </summary>
    [HttpPost("{profileId}/assign-character")]
    [Authorize]
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
    /// Get all user profiles for a specific account (identified by email)
    /// </summary>
    [HttpGet("account/{email}")]
    public async Task<ActionResult<List<UserProfile>>> GetProfilesForAccount(string email)
    {
        try
        {
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Account with email {email} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var profiles = await _accountService.GetUserProfilesForAccountAsync(account.Id);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for account {Email}", email);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while getting account profiles",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create a profile for a specific account (identified by email)
    /// </summary>
    [HttpPost("account/{email}")]
    [Authorize]
    public async Task<ActionResult<UserProfile>> CreateProfileForAccount(string email, [FromBody] CreateUserProfileRequest request)
    {
        try
        {
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Account with email {email} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

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
            
            // Link the profile to the account
            profile.AccountId = account.Id;
            await _profileService.UpdateProfileAsync(profile.Id, new UpdateUserProfileRequest
            {
                AccountId = account.Id
            });

            // Update account's profile list
            account.UserProfileIds.Add(profile.Id);
            await _accountService.UpdateAccountAsync(account);

            return CreatedAtAction(nameof(GetProfile), new { name = profile.Name }, profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile for account {Email}", email);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while creating profile for account",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update a profile's account association
    /// </summary>
    [HttpPut("{profileId}/account")]
    [Authorize]
    public async Task<ActionResult> UpdateProfileAccount(string profileId, [FromBody] UpdateProfileAccountRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = "Email is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var account = await _accountService.GetAccountByEmailAsync(request.Email);
            if (account == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Account with email {request.Email} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var profile = await _profileService.GetProfileByIdAsync(profileId);
            if (profile == null)
            {
                return NotFound(new ErrorResponse 
                { 
                    Message = $"Profile with ID {profileId} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            // Update profile's account ID
            await _profileService.UpdateProfileAsync(profileId, new UpdateUserProfileRequest
            {
                AccountId = account.Id
            });

            // Add profile to account's profile list if not already there
            if (!account.UserProfileIds.Contains(profileId))
            {
                account.UserProfileIds.Add(profileId);
                await _accountService.UpdateAccountAsync(account);
            }

            return Ok(new { message = "Profile account association updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile account association for profile {ProfileId}", profileId);
            return StatusCode(500, new ErrorResponse 
            { 
                Message = "Internal server error while updating profile account association",
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
                await _profileService.UpdateProfileAsync(profileId, new UpdateUserProfileRequest
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