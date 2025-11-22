using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.App.Api.Models;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }


    /// <summary>
    /// Get the health status of the API and its dependencies
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HealthCheckResponse>> GetHealth()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            stopwatch.Stop();

            var response = new HealthCheckResponse
            {
                Status = report.Status.ToString(),
                Duration = stopwatch.Elapsed,
                Results = report.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)new
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description,
                        Duration = kvp.Value.Duration,
                        Exception = kvp.Value.Exception?.Message,
                        Data = kvp.Value.Data
                    }
                )
            };

            var statusCode = report.Status switch
            {
                HealthStatus.Healthy => 200,
                HealthStatus.Degraded => 200,
                HealthStatus.Unhealthy => 503,
                _ => 200
            };

            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error checking health status");

            return StatusCode(503, new HealthCheckResponse
            {
                Status = "Unhealthy",
                Duration = stopwatch.Elapsed,
                Results = new Dictionary<string, object>
                {
                    ["error"] = new { Message = "Health check failed", Exception = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Simple readiness probe for container orchestration
    /// </summary>
    [HttpGet("ready")]
    public ActionResult GetReady()
    {
        return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Simple liveness probe for container orchestration
    /// </summary>
    [HttpGet("live")]
    public ActionResult GetLive()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}
