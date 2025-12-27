using Wolverine;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.UserBadges.Commands;
using Mystira.App.Application.CQRS.UserBadges.Queries;
using Mystira.Contracts.App.Requests.Badges;
using Mystira.App.Api.Models;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

/// <summary>
/// Controller for user badge management.
/// Follows hexagonal architecture - uses only IMessageBus (CQRS pattern).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserBadgesController : ControllerBase
{
    private readonly IMessageBus _bus;
    private readonly ILogger<UserBadgesController> _logger;

    public UserBadgesController(
        IMessageBus bus,
        ILogger<UserBadgesController> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// Award a badge to a user profile
    /// </summary>
    [HttpPost("award")]
    public async Task<ActionResult<UserBadge>> AwardBadge([FromBody] AwardBadgeRequest request)
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

            var command = new AwardBadgeCommand(request);
            var badge = await _bus.InvokeAsync<UserBadge>(command);
            return CreatedAtAction(nameof(GetUserBadges),
                new { userProfileId = request.UserProfileId }, badge);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error awarding badge");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding badge");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while awarding badge",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all badges for a user profile
    /// </summary>
    [HttpGet("user/{userProfileId}")]
    public async Task<ActionResult<List<UserBadge>>> GetUserBadges(string userProfileId)
    {
        try
        {
            var query = new GetUserBadgesQuery(userProfileId);
            var badges = await _bus.InvokeAsync<List<UserBadge>>(query);
            return Ok(badges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for user {UserProfileId}", userProfileId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badges",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get badges for a specific axis for a user profile
    /// </summary>
    [HttpGet("user/{userProfileId}/axis/{axis}")]
    public async Task<ActionResult<List<UserBadge>>> GetUserBadgesForAxis(string userProfileId, string axis)
    {
        try
        {
            var query = new GetUserBadgesForAxisQuery(userProfileId, axis);
            var badges = await _bus.InvokeAsync<List<UserBadge>>(query);
            return Ok(badges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for user {UserProfileId} and axis {Axis}",
                userProfileId, axis);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badges for axis",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Check if a user has earned a specific badge
    /// </summary>
    [HttpGet("user/{userProfileId}/badge/{badgeConfigurationId}/earned")]
    public async Task<ActionResult<bool>> HasUserEarnedBadge(string userProfileId, string badgeConfigurationId)
    {
        try
        {
            var query = new HasUserEarnedBadgeQuery(userProfileId, badgeConfigurationId);
            var hasEarned = await _bus.InvokeAsync<bool>(query);
            return Ok(new { hasEarned });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserProfileId} has badge {BadgeId}",
                userProfileId, badgeConfigurationId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while checking badge status",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get badge statistics for a user profile
    /// </summary>
    [HttpGet("user/{userProfileId}/statistics")]
    public async Task<ActionResult<Dictionary<string, int>>> GetBadgeStatistics(string userProfileId)
    {
        try
        {
            var query = new GetBadgeStatisticsQuery(userProfileId);
            var statistics = await _bus.InvokeAsync<Dictionary<string, int>>(query);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge statistics for user {UserProfileId}", userProfileId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badge statistics",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all badges for all profiles belonging to an account (identified by email)
    /// </summary>
    [HttpGet("account/{email}")]
    public async Task<ActionResult<List<UserBadge>>> GetBadgesForAccount(string email)
    {
        try
        {
            var query = new GetBadgesForAccountByEmailQuery(email);
            var badges = await _bus.InvokeAsync<List<UserBadge>>(query);

            if (!badges.Any())
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Account with email {email} not found or has no badges",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(badges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for account {Email}", email);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting account badges",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get badge statistics for all profiles belonging to an account (identified by email)
    /// </summary>
    [HttpGet("account/{email}/statistics")]
    public async Task<ActionResult<Dictionary<string, int>>> GetBadgeStatisticsForAccount(string email)
    {
        try
        {
            var query = new GetBadgeStatisticsForAccountByEmailQuery(email);
            var statistics = await _bus.InvokeAsync<Dictionary<string, int>>(query);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge statistics for account {Email}", email);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while getting account badge statistics",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
