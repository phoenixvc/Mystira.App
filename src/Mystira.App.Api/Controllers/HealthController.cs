using Wolverine;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Health.Queries;
using Mystira.App.Api.Models;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IMessageBus _bus;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IMessageBus bus, ILogger<HealthController> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// Get the health status of the API and its dependencies
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HealthCheckResponse>> GetHealth()
    {
        var query = new GetHealthCheckQuery();
        var result = await _bus.InvokeAsync<HealthCheckResult>(query);

        var response = new HealthCheckResponse
        {
            Status = result.Status,
            Duration = result.Duration,
            Results = result.Results
        };

        var statusCode = result.Status switch
        {
            "Healthy" => 200,
            "Degraded" => 200,
            "Unhealthy" => 503,
            _ => 200
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Simple readiness probe for container orchestration
    /// </summary>
    [HttpGet("ready")]
    public async Task<ActionResult> GetReady()
    {
        var query = new GetReadinessQuery();
        var result = await _bus.InvokeAsync<ProbeResult>(query);

        return Ok(new { status = result.Status, timestamp = result.Timestamp });
    }

    /// <summary>
    /// Simple liveness probe for container orchestration
    /// </summary>
    [HttpGet("live")]
    public async Task<ActionResult> GetLive()
    {
        var query = new GetLivenessQuery();
        var result = await _bus.InvokeAsync<ProbeResult>(query);

        return Ok(new { status = result.Status, timestamp = result.Timestamp });
    }
}
