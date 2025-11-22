using Microsoft.AspNetCore.Mvc;
using Mystira.App.Domain.Models;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BadgeConfigurationsController : ControllerBase
{
    private readonly IBadgeConfigurationApiService _badgeConfigService;
    private readonly ILogger<BadgeConfigurationsController> _logger;

    public BadgeConfigurationsController(IBadgeConfigurationApiService badgeConfigService, ILogger<BadgeConfigurationsController> logger)
    {
        _badgeConfigService = badgeConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Get all badge configurations
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BadgeConfiguration>>> GetAllBadgeConfigurations()
    {
        try
        {
            var badgeConfigs = await _badgeConfigService.GetAllBadgeConfigurationsAsync();
            return Ok(badgeConfigs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all badge configurations");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badge configurations",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get badge configuration by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BadgeConfiguration>> GetBadgeConfiguration(string id)
    {
        try
        {
            var badgeConfig = await _badgeConfigService.GetBadgeConfigurationAsync(id);
            if (badgeConfig == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Badge configuration not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(badgeConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge configuration {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badge configuration",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get badge configurations by compass axis
    /// </summary>
    [HttpGet("axis/{axis}")]
    public async Task<ActionResult<List<BadgeConfiguration>>> GetBadgeConfigurationsByAxis(string axis)
    {
        try
        {
            var badgeConfigs = await _badgeConfigService.GetBadgeConfigurationsByAxisAsync(axis);
            return Ok(badgeConfigs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge configurations for axis {Axis}", axis);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while fetching badge configurations",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
