using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Badges.Queries;
using Mystira.App.Contracts.Responses.Badges;
using Mystira.App.Contracts.Responses.Common;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for public badge endpoints
/// Provides badge configuration and progress information for players
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BadgesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BadgesController> _logger;

    public BadgesController(IMediator mediator, ILogger<BadgesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all badges for a specific age group
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BadgeResponse>>> GetBadgesByAgeGroup([FromQuery] string ageGroup)
    {
        if (string.IsNullOrWhiteSpace(ageGroup))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "ageGroup query parameter is required",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var response = await _mediator.Send(new GetBadgesByAgeGroupQuery(ageGroup));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for age group {AgeGroup}", ageGroup);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badges",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get axis achievement copy for a specific age group
    /// </summary>
    [HttpGet("axis-achievements")]
    public async Task<ActionResult<List<AxisAchievementResponse>>> GetAxisAchievements([FromQuery] string ageGroupId)
    {
        if (string.IsNullOrWhiteSpace(ageGroupId))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "ageGroupId query parameter is required",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var response = await _mediator.Send(new GetAxisAchievementsQuery(ageGroupId));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting axis achievements for age group {AgeGroup}", ageGroupId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching axis achievements",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get badge details for a specific badge
    /// </summary>
    [HttpGet("{badgeId}")]
    public async Task<ActionResult<BadgeResponse>> GetBadgeDetail(string badgeId)
    {
        try
        {
            var badge = await _mediator.Send(new GetBadgeDetailQuery(badgeId));
            if (badge == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Badge not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(badge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge {BadgeId}", badgeId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badge",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get badge progress and earned badges for a profile
    /// </summary>
    [HttpGet("profile/{profileId}")]
    public async Task<ActionResult<BadgeProgressResponse>> GetProfileBadgeProgress(string profileId)
    {
        try
        {
            var progress = await _mediator.Send(new GetProfileBadgeProgressQuery(profileId));
            if (progress == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Profile not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge progress for profile {ProfileId}", profileId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badge progress",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
