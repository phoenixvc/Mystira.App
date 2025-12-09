using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.App.Application.Ports.Health;

namespace Mystira.App.Api.Adapters;

/// <summary>
/// Adapter that adapts ASP.NET Core HealthCheckService to Application.Ports.Health.IHealthCheckPort
/// </summary>
public class HealthCheckPortAdapter : IHealthCheckPort
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckPortAdapter(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<Application.Ports.Health.HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var aspNetHealthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);

        return new Application.Ports.Health.HealthReport
        {
            Status = aspNetHealthReport.Status.ToString(),
            Duration = aspNetHealthReport.TotalDuration,
            Entries = aspNetHealthReport.Entries.ToDictionary(
                e => e.Key,
                e => new Application.Ports.Health.HealthCheckEntry
                {
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description,
                    Duration = e.Value.Duration,
                    Exception = e.Value.Exception?.ToString(),
                    Data = e.Value.Data != null ? new Dictionary<string, object>(e.Value.Data) : null
                })
        };
    }
}

