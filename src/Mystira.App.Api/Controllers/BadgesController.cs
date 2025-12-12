using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.Ports.Data;
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
    private readonly IBadgeRepository _badgeRepository;
    private readonly ICompassAxisRepository _axisRepository;
    private readonly IAxisAchievementRepository _axisAchievementRepository;
    private readonly IUserBadgeRepository _userBadgeRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly ILogger<BadgesController> _logger;

    public BadgesController(
        IBadgeRepository badgeRepository,
        ICompassAxisRepository axisRepository,
        IAxisAchievementRepository axisAchievementRepository,
        IUserBadgeRepository userBadgeRepository,
        IUserProfileRepository profileRepository,
        ILogger<BadgesController> logger)
    {
        _badgeRepository = badgeRepository;
        _axisRepository = axisRepository;
        _axisAchievementRepository = axisAchievementRepository;
        _userBadgeRepository = userBadgeRepository;
        _profileRepository = profileRepository;
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
            var badges = await _badgeRepository.GetByAgeGroupAsync(ageGroup);
            var response = badges
                .OrderBy(b => b.CompassAxisId)
                .ThenBy(b => b.TierOrder)
                .Select(b => new BadgeResponse
                {
                    Id = b.Id,
                    AgeGroupId = b.AgeGroupId,
                    CompassAxisId = b.CompassAxisId,
                    Tier = b.Tier,
                    TierOrder = b.TierOrder,
                    Title = b.Title,
                    Description = b.Description,
                    RequiredScore = b.RequiredScore,
                    ImageId = b.ImageId
                })
                .ToList();

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
            var achievements = await _axisAchievementRepository.GetByAgeGroupAsync(ageGroupId);
            var axes = await _axisRepository.GetAllAsync();

            var axisLookup = axes
                .SelectMany(a => new[] { (Key: a.Id, Value: a), (Key: a.Name, Value: a) })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

            var response = achievements
                .OrderBy(a => a.CompassAxisId)
                .ThenBy(a => a.AxesDirection)
                .Select(a =>
                {
                    axisLookup.TryGetValue(a.CompassAxisId, out var axis);
                    var axisName = axis != null && !string.IsNullOrWhiteSpace(axis.Name)
                        ? axis.Name
                        : (axis?.Id ?? a.CompassAxisId);

                    return new AxisAchievementResponse
                    {
                        Id = a.Id,
                        AgeGroupId = a.AgeGroupId,
                        CompassAxisId = a.CompassAxisId,
                        CompassAxisName = axisName,
                        AxesDirection = a.AxesDirection,
                        Description = a.Description
                    };
                })
                .ToList();

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
            var badge = await _badgeRepository.GetByIdAsync(badgeId);
            if (badge == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Badge not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var response = new BadgeResponse
            {
                Id = badge.Id,
                AgeGroupId = badge.AgeGroupId,
                CompassAxisId = badge.CompassAxisId,
                Tier = badge.Tier,
                TierOrder = badge.TierOrder,
                Title = badge.Title,
                Description = badge.Description,
                RequiredScore = badge.RequiredScore,
                ImageId = badge.ImageId
            };

            return Ok(response);
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
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Profile not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var ageGroupId = profile.AgeGroup?.Value ?? "6-9";

            // Get all badges for this age group
            var badgesByAxis = (await _badgeRepository.GetByAgeGroupAsync(ageGroupId))
                .GroupBy(b => b.CompassAxisId)
                .ToDictionary(g => g.Key, g => g.OrderBy(b => b.TierOrder).ToList());

            // Get earned badges for this profile
            var earnedBadges = (await _userBadgeRepository.GetByUserProfileIdAsync(profileId))
                .ToDictionary(b => b.BadgeId ?? string.Empty, b => b);

            // Get all axes for this age group
            var axes = await _axisRepository.GetAllAsync();
            var axisDictionary = axes.ToDictionary(a => a.Id, a => a);

            // Build response
            var response = new BadgeProgressResponse
            {
                AgeGroupId = ageGroupId,
                AxisProgresses = new List<AxisProgressResponse>()
            };

            // Calculate progress for each axis that has badges
            foreach (var (axisId, badges) in badgesByAxis.OrderBy(x => x.Key))
            {
                var axis = axisDictionary.TryGetValue(axisId, out var a) ? a : null;
                var axisName = axis?.Name ?? axisId;

                // Get score for this axis from player's total axis scores
                // Note: We'd need to query PlayerScenarioScores to get the aggregate score
                // For now, using 0 as default (in production, calculate from session history)
                var currentScore = 0f;

                var axisTiers = new List<BadgeTierProgressResponse>();
                foreach (var badge in badges)
                {
                    var isEarned = earnedBadges.TryGetValue(badge.Id, out var earnedBadge);

                    axisTiers.Add(new BadgeTierProgressResponse
                    {
                        BadgeId = badge.Id,
                        Tier = badge.Tier,
                        TierOrder = badge.TierOrder,
                        Title = badge.Title,
                        Description = badge.Description,
                        RequiredScore = badge.RequiredScore,
                        ImageId = badge.ImageId,
                        IsEarned = isEarned,
                        EarnedAt = isEarned ? earnedBadge.EarnedAt : null,
                        ProgressToThreshold = currentScore,
                        RemainingScore = Math.Max(0, badge.RequiredScore - currentScore)
                    });
                }

                response.AxisProgresses.Add(new AxisProgressResponse
                {
                    AxisId = axisId,
                    AxisName = axisName,
                    CurrentScore = currentScore,
                    Tiers = axisTiers
                });
            }

            return Ok(response);
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
