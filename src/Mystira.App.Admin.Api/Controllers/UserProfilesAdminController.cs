using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UserProfilesAdminController : ControllerBase
{
    private readonly IUserProfileApiService _profileService;
    private readonly IAccountApiService _accountService;
    private readonly ILogger<UserProfilesAdminController> _logger;

    public UserProfilesAdminController(
        IUserProfileApiService profileService,
        IAccountApiService accountService,
        ILogger<UserProfilesAdminController> logger)
    {
        _profileService = profileService;
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Get all user profiles for a given account
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
            return StatusCode(500, new { message = "Internal server error while fetching profiles" });
        }
    }

    /// <summary>
    /// Create a new user profile
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserProfile>> CreateProfile([FromBody] CreateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var profile = await _profileService.CreateProfileAsync(request);
            return CreatedAtAction(nameof(GetProfilesByAccount), new { accountId = profile.AccountId }, profile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating profile");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return StatusCode(500, new { message = "Internal server error while creating profile" });
        }
    }

    /// <summary>
    /// Get a profile by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfile>> GetProfileById(string id)
    {
        try
        {
            var profile = await _profileService.GetProfileByIdAsync(id);
            if (profile == null)
            {
                return NotFound(new { message = $"Profile not found: {id}" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {Id}", id);
            return StatusCode(500, new { message = "Internal server error while fetching profile" });
        }
    }

    /// <summary>
    /// Update a user profile by name
    /// </summary>
    [HttpPut("{name}")]
    public async Task<ActionResult<UserProfile>> UpdateProfile(string name, [FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var profile = await _profileService.UpdateProfileAsync(name, request);
            if (profile == null)
            {
                return NotFound(new { message = $"Profile not found: {name}" });
            }

            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating profile {Name}", name);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {Name}", name);
            return StatusCode(500, new { message = "Internal server error while updating profile" });
        }
    }

    /// <summary>
    /// Update a user profile by ID
    /// </summary>
    [HttpPut("id/{profileId}")]
    public async Task<ActionResult<UserProfile>> UpdateProfileById(string profileId, [FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedProfile = await _profileService.UpdateProfileByIdAsync(profileId, request);
            if (updatedProfile == null)
            {
                return NotFound(new { message = $"Profile not found: {profileId}" });
            }

            return Ok(updatedProfile);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating profile {ProfileId}", profileId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", profileId);
            return StatusCode(500, new { message = "Internal server error while updating profile" });
        }
    }

    /// <summary>
    /// Delete a user profile
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<ActionResult> DeleteProfile(string name)
    {
        try
        {
            var deleted = await _profileService.DeleteProfileAsync(name);
            if (!deleted)
            {
                return NotFound(new { message = $"Profile not found: {name}" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {Name}", name);
            return StatusCode(500, new { message = "Internal server error while deleting profile" });
        }
    }

    /// <summary>
    /// Get all profiles
    /// </summary>
    [HttpGet]
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
            return StatusCode(500, new { message = "Internal server error while fetching profiles" });
        }
    }

    /// <summary>
    /// Get all non-guest profiles
    /// </summary>
    [HttpGet("non-guest")]
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
            return StatusCode(500, new { message = "Internal server error while fetching profiles" });
        }
    }

    /// <summary>
    /// Get all guest profiles
    /// </summary>
    [HttpGet("guest")]
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
            return StatusCode(500, new { message = "Internal server error while fetching profiles" });
        }
    }
}
