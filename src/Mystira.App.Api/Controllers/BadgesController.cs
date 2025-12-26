using Wolverine;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Badges.Queries;
using Mystira.Contracts.App.Responses.Badges;
using Mystira.App.Api.Models;
using AppQueries = Mystira.App.Application.CQRS.Badges.Queries;

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
    private readonly IMessageBus _bus;
    private readonly ILogger<BadgesController> _logger;

    public BadgesController(IMessageBus bus, ILogger<BadgesController> logger)
    {
        _bus = bus;
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
            var response = await _bus.InvokeAsync<List<BadgeResponse>>(new GetBadgesByAgeGroupQuery(ageGroup));
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
            var response = await _bus.InvokeAsync<List<AxisAchievementResponse>>(new GetAxisAchievementsQuery(ageGroupId));
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
            var badge = await _bus.InvokeAsync<BadgeResponse?>(new GetBadgeDetailQuery(badgeId));
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
    /// Retrieves badge progress for the specified profile.
    /// </summary>
    /// <param name="profileId">The identifier of the profile whose badge progress to retrieve.</param>
    /// <returns>The badge progress for the profile as a <see cref="BadgeProgressResponse"/>.</returns>
    [HttpGet("profile/{profileId}")]
    public async Task<ActionResult<BadgeProgressResponse>> GetProfileBadgeProgress(string profileId)
    {
        try
        {
            var progress = await _bus.InvokeAsync<BadgeProgressResponse?>(new GetProfileBadgeProgressQuery(profileId));
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

    /// <summary>
    /// Calculate required badge scores per tier for a content bundle.
    /// Performs depth-first traversal of all scenarios in the bundle and calculates
    /// percentile-based score thresholds for each compass axis.
    /// </summary>
    /// <param name="request">Request payload containing a non-empty ContentBundleId and a non-empty list of Percentiles used to compute per-tier scores.</param>
    /// <returns>A list of CompassAxisScoreResult where each item maps the requested percentiles to score thresholds for a compass axis.</returns>
    [HttpPost("calculate-scores")]
    [ProducesResponseType(typeof(List<AppQueries.CompassAxisScoreResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AppQueries.CompassAxisScoreResult>>> CalculateBadgeScores(
        [FromBody] CalculateBadgeScoresRequest request)
    {
        if (request == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Request body is required",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        if (string.IsNullOrWhiteSpace(request.ContentBundleId))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "ContentBundleId is required",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        if (request.Percentiles == null || !request.Percentiles.Any())
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Percentiles array is required and must not be empty",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var query = new CalculateBadgeScoresQuery(
                request.ContentBundleId,
                request.Percentiles);

            var results = await _bus.InvokeAsync<List<AppQueries.CompassAxisScoreResult>>(query);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for badge score calculation");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Content bundle not found: {BundleId}", request.ContentBundleId);
            return NotFound(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating badge scores for bundle {BundleId}", request.ContentBundleId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while calculating badge scores",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}

/// <summary>
/// Request model for calculating badge scores per tier
/// </summary>
public class CalculateBadgeScoresRequest
{
    /// <summary>
    /// The ID of the content bundle containing scenarios to analyze
    /// </summary>
    public string ContentBundleId { get; set; } = string.Empty;

    /// <summary>
    /// Array of percentile values (0-100) to calculate score thresholds for.
    /// Example: [50, 75, 90, 95] for bronze, silver, gold, platinum tiers
    /// </summary>
    public List<double> Percentiles { get; set; } = new();
}
