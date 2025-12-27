using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mystira.Contracts.App.Ports.Health;

namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Wolverine message handler for retrieving application health check status.
/// Executes health checks and formats results.
/// </summary>
public static class GetHealthCheckQueryHandler
{
    public static async Task<HealthCheckResult> Handle(
        GetHealthCheckQuery request,
        IHealthCheckPort healthCheckPort,
        ILogger<GetHealthCheckQuery> logger,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var report = await healthCheckPort.CheckHealthAsync(ct);
            stopwatch.Stop();

            var results = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new
                {
                    Status = kvp.Value.Status,
                    Description = kvp.Value.Description,
                    Duration = kvp.Value.Duration,
                    Exception = kvp.Value.Exception,
                    Data = kvp.Value.Data
                }
            );

            logger.LogInformation("Health check completed: {Status}, Duration: {Duration}ms",
                report.Status, stopwatch.ElapsedMilliseconds);

            return new HealthCheckResult(
                Status: report.Status.ToString(),
                Duration: report.TotalDuration,
                Results: results
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error during health check execution");

            var errorResults = new Dictionary<string, object>
            {
                ["error"] = new { Message = "Health check failed", Exception = ex.Message }
            };

            return new HealthCheckResult(
                Status: "Unhealthy",
                Duration: stopwatch.Elapsed,
                Results: errorResults
            );
        }
    }
}
