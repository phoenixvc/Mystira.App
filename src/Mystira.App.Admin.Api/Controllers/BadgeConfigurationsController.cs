using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Admin.Api.Services;
using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Controllers;

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

    /// <summary>
    /// Create a new badge configuration
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BadgeConfiguration>> CreateBadgeConfiguration([FromBody] CreateBadgeConfigurationRequest request)
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

            var badgeConfig = await _badgeConfigService.CreateBadgeConfigurationAsync(request);
            return CreatedAtAction(nameof(GetBadgeConfiguration), new { id = badgeConfig.Id }, badgeConfig);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating badge configuration");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating badge configuration");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while creating badge configuration",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update a badge configuration
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<BadgeConfiguration>> UpdateBadgeConfiguration(string id, [FromBody] UpdateBadgeConfigurationRequest request)
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

            var badgeConfig = await _badgeConfigService.UpdateBadgeConfigurationAsync(id, request);
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating badge configuration {Id}", id);
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating badge configuration {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while updating badge configuration",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a badge configuration
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteBadgeConfiguration(string id)
    {
        try
        {
            var deleted = await _badgeConfigService.DeleteBadgeConfigurationAsync(id);
            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Badge configuration not found: {id}",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting badge configuration {Id}", id);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while deleting badge configuration",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Export badge configurations as YAML
    /// </summary>
    [HttpGet("export")]
    [Authorize]
    public async Task<ActionResult> ExportBadgeConfigurations()
    {
        try
        {
            var yamlContent = await _badgeConfigService.ExportBadgeConfigurationsAsYamlAsync();
            return File(System.Text.Encoding.UTF8.GetBytes(yamlContent), "application/x-yaml", "badge_configurations.yaml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting badge configurations");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while exporting badge configurations",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Import badge configurations from YAML
    /// </summary>
    [HttpPost("import")]
    [Authorize]
    public async Task<ActionResult<List<BadgeConfiguration>>> ImportBadgeConfigurations([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "No file provided",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            if (!file.FileName.EndsWith(".yaml") && !file.FileName.EndsWith(".yml"))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "File must be a YAML file (.yaml or .yml)",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            using var stream = file.OpenReadStream();
            var badgeConfigs = await _badgeConfigService.ImportBadgeConfigurationsFromYamlAsync(stream);
            return Ok(badgeConfigs);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error importing badge configurations");
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing badge configurations");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error while importing badge configurations",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
