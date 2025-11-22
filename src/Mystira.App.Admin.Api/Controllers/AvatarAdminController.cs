using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize]
public class AvatarAdminController : ControllerBase
{
    private readonly IAvatarApiService _avatarService;
    private readonly ILogger<AvatarAdminController> _logger;

    public AvatarAdminController(IAvatarApiService avatarService, ILogger<AvatarAdminController> logger)
    {
        _avatarService = avatarService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available avatars grouped by age group
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AvatarResponse>> GetAvatars()
    {
        try
        {
            var avatars = await _avatarService.GetAvatarsAsync();
            return Ok(avatars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting avatars");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting avatars",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets avatars for a specific age group
    /// </summary>
    [HttpGet("{ageGroup}")]
    public async Task<ActionResult<AvatarConfigurationResponse>> GetAvatarsByAgeGroup(string ageGroup)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Age group is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var avatars = await _avatarService.GetAvatarsByAgeGroupAsync(ageGroup);

            if (avatars == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"No avatars found for age group: {ageGroup}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(avatars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting avatars for age group: {AgeGroup}", ageGroup);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting avatars",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Sets avatars for a specific age group
    /// </summary>
    [HttpPost("{ageGroup}")]
    public async Task<ActionResult<AvatarConfigurationFile>> SetAvatarsForAgeGroup(string ageGroup, [FromBody] List<string> mediaIds)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Age group is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (mediaIds == null || mediaIds.Count == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "At least one media ID is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var result = await _avatarService.SetAvatarsForAgeGroupAsync(ageGroup, mediaIds);
            _logger.LogInformation("Set {Count} avatars for age group: {AgeGroup}", mediaIds.Count, ageGroup);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting avatars for age group: {AgeGroup}", ageGroup);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while setting avatars",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Adds an avatar to a specific age group
    /// </summary>
    [HttpPost("{ageGroup}/add")]
    public async Task<ActionResult<AvatarConfigurationFile>> AddAvatarToAgeGroup(string ageGroup, [FromBody] string mediaId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Age group is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (string.IsNullOrWhiteSpace(mediaId))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Media ID is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var result = await _avatarService.AddAvatarToAgeGroupAsync(ageGroup, mediaId);
            _logger.LogInformation("Added avatar {MediaId} to age group: {AgeGroup}", mediaId, ageGroup);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding avatar to age group: {AgeGroup}", ageGroup);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while adding avatar",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Removes an avatar from a specific age group
    /// </summary>
    [HttpDelete("{ageGroup}/remove/{mediaId}")]
    public async Task<ActionResult<AvatarConfigurationFile>> RemoveAvatarFromAgeGroup(string ageGroup, string mediaId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ageGroup))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Age group is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (string.IsNullOrWhiteSpace(mediaId))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Media ID is required",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var result = await _avatarService.RemoveAvatarFromAgeGroupAsync(ageGroup, mediaId);
            _logger.LogInformation("Removed avatar {MediaId} from age group: {AgeGroup}", mediaId, ageGroup);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing avatar from age group: {AgeGroup}", ageGroup);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while removing avatar",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
