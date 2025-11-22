using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Contracts.Responses.Badges;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserBadgesController : ControllerBase
{
    private readonly IUserBadgeApiService _badgeService;
    private readonly IAccountApiService _accountService;
    private readonly ILogger<UserBadgesController> _logger;

    public UserBadgesController(
        IUserBadgeApiService badgeService,
        IAccountApiService accountService,
        ILogger<UserBadgesController> logger)
    {
        _badgeService = badgeService;
        _accountService = accountService;
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

            var badge = await _badgeService.AwardBadgeAsync(request);
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
            var badges = await _badgeService.GetUserBadgesAsync(userProfileId);
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
            var badges = await _badgeService.GetUserBadgesForAxisAsync(userProfileId, axis);
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
            var hasEarned = await _badgeService.HasUserEarnedBadgeAsync(userProfileId, badgeConfigurationId);
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
            var statistics = await _badgeService.GetBadgeStatisticsAsync(userProfileId);
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
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Account with email {email} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var allBadges = new List<UserBadge>();
            var profiles = await _accountService.GetUserProfilesForAccountAsync(account.Id);

            foreach (var profile in profiles)
            {
                var badges = await _badgeService.GetUserBadgesAsync(profile.Id);
                allBadges.AddRange(badges);
            }

            return Ok(allBadges);
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
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Account with email {email} not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var combinedStatistics = new Dictionary<string, int>();
            var profiles = await _accountService.GetUserProfilesForAccountAsync(account.Id);

            foreach (var profile in profiles)
            {
                var profileStats = await _badgeService.GetBadgeStatisticsAsync(profile.Id);
                foreach (var stat in profileStats)
                {
                    if (combinedStatistics.ContainsKey(stat.Key))
                    {
                        combinedStatistics[stat.Key] += stat.Value;
                    }
                    else
                    {
                        combinedStatistics[stat.Key] = stat.Value;
                    }
                }
            }

            return Ok(combinedStatistics);
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
