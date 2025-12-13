using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
public class BadgesController : ControllerBase
{
    private readonly IBadgeAdminService _badgeAdminService;
    private readonly ILogger<BadgesController> _logger;

    public BadgesController(IBadgeAdminService badgeAdminService, ILogger<BadgesController> logger)
    {
        _badgeAdminService = badgeAdminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BadgeDto>>> GetBadges([FromQuery] BadgeQueryOptions query)
    {
        var badges = await _badgeAdminService.GetBadgesAsync(query);
        return Ok(badges);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BadgeDto>> GetBadgeById(string id)
    {
        var badge = await _badgeAdminService.GetBadgeByIdAsync(id);
        if (badge == null)
        {
            return NotFound(new { message = "Badge not found" });
        }

        return Ok(badge);
    }

    [HttpPost]
    public async Task<ActionResult<BadgeDto>> CreateBadge([FromBody] CreateBadgeRequest request)
    {
        try
        {
            var badge = await _badgeAdminService.CreateBadgeAsync(request);
            return CreatedAtAction(nameof(GetBadgeById), new { id = badge.Id }, badge);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Badge creation failed");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BadgeDto>> UpdateBadge(string id, [FromBody] UpdateBadgeRequest request)
    {
        try
        {
            var updated = await _badgeAdminService.UpdateBadgeAsync(id, request);
            if (updated == null)
            {
                return NotFound(new { message = "Badge not found" });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Badge update failed for {BadgeId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBadge(string id)
    {
        var removed = await _badgeAdminService.DeleteBadgeAsync(id);
        if (!removed)
        {
            return NotFound(new { message = "Badge not found" });
        }

        return NoContent();
    }

    [HttpGet("axis-achievements")]
    public async Task<ActionResult<IEnumerable<AxisAchievementDto>>> GetAxisAchievements([FromQuery] string? ageGroupId, [FromQuery] string? compassAxisId)
    {
        var results = await _badgeAdminService.GetAxisAchievementsAsync(ageGroupId, compassAxisId);
        return Ok(results);
    }

    [HttpPost("axis-achievements")]
    public async Task<ActionResult<AxisAchievementDto>> CreateAxisAchievement([FromBody] AxisAchievementRequest request)
    {
        try
        {
            var created = await _badgeAdminService.CreateAxisAchievementAsync(request);
            return CreatedAtAction(nameof(GetAxisAchievements), new { ageGroupId = created.AgeGroupId }, created);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Axis achievement creation failed");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("axis-achievements/{id}")]
    public async Task<ActionResult<AxisAchievementDto>> UpdateAxisAchievement(string id, [FromBody] AxisAchievementRequest request)
    {
        try
        {
            var updated = await _badgeAdminService.UpdateAxisAchievementAsync(id, request);
            if (updated == null)
            {
                return NotFound(new { message = "Axis achievement not found" });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Axis achievement update failed for {AxisAchievementId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("axis-achievements/{id}")]
    public async Task<IActionResult> DeleteAxisAchievement(string id)
    {
        var deleted = await _badgeAdminService.DeleteAxisAchievementAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = "Axis achievement not found" });
        }

        return NoContent();
    }

    [HttpPost("import")]
    public async Task<ActionResult<BadgeImportResult>> ImportBadges([FromForm] IFormFile configFile, [FromForm] bool overwrite = false)
    {
        if (configFile == null || configFile.Length == 0)
        {
            return BadRequest(new { message = "Configuration file is required" });
        }

        await using var stream = configFile.OpenReadStream();
        var result = await _badgeAdminService.ImportAsync(stream, overwrite);
        var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, result);
    }

    [HttpGet("age-groups/{ageGroupId}/snapshot")]
    public async Task<ActionResult<BadgeSnapshotDto>> GetSnapshot(string ageGroupId)
    {
        var snapshot = await _badgeAdminService.GetSnapshotAsync(ageGroupId);
        if (snapshot == null)
        {
            return NotFound(new { message = "Age group not found" });
        }

        return Ok(snapshot);
    }
}
