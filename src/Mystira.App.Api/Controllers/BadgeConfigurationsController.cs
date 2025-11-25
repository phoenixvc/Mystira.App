using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Api.Services;
using Mystira.App.Application.CQRS.BadgeConfigurations.Queries;
using Mystira.App.Contracts.Requests.Badges;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BadgeConfigurationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BadgeConfigurationsController> _logger;

    public BadgeConfigurationsController(IMediator mediator, ILogger<BadgeConfigurationsController> logger)
    {
        _mediator = mediator;
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
            var query = new GetAllBadgeConfigurationsQuery();
            var badgeConfigs = await _mediator.Send(query);
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
            var query = new GetBadgeConfigurationQuery(id);
            var badgeConfig = await _mediator.Send(query);
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
            var query = new GetBadgeConfigurationsByAxisQuery(axis);
            var badgeConfigs = await _mediator.Send(query);
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
