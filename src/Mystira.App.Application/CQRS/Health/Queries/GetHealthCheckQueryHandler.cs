using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Handler for retrieving application health check status.
/// Executes ASP.NET Core health checks and formats results.
/// </summary>
public class GetHealthCheckQueryHandler
    : IQueryHandler<GetHealthCheckQuery, HealthCheckResult>
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<GetHealthCheckQueryHandler> _logger;

    public GetHealthCheckQueryHandler(
        IHealthCheckService healthCheckService,
        ILogger<GetHealthCheckQueryHandler> _logger)
    {
        _healthCheckService = healthCheckService;
        this._logger = _logger;
    }

    public async Task<HealthCheckResult> Handle(
        GetHealthCheckQuery request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
            stopwatch.Stop();

            var results = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new
                {
                    Status = kvp.Value.Status.ToString(),
                    Description = kvp.Value.Description,
                    Duration = kvp.Value.Duration,
                    Exception = kvp.Value.Exception?.Message,
                    Data = kvp.Value.Data
                }
            );

            _logger.LogInformation("Health check completed: {Status}, Duration: {Duration}ms",
                report.Status, stopwatch.ElapsedMilliseconds);

            return new HealthCheckResult(
                Status: report.Status.ToString(),
                Duration: stopwatch.Elapsed,
                Results: results
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during health check execution");

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
