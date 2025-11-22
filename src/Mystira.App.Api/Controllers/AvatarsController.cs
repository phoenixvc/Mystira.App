using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AvatarsController : ControllerBase
{
    private readonly IAvatarApiService _avatarService;
    private readonly ILogger<AvatarsController> _logger;

    public AvatarsController(IAvatarApiService avatarService, ILogger<AvatarsController> logger)
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
}
