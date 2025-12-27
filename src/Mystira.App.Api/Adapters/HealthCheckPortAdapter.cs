using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.Contracts.App.Ports.Health;

namespace Mystira.App.Api.Adapters;

/// <summary>
/// Adapter that adapts ASP.NET Core HealthCheckService to Mystira.Contracts.App.Ports.Health.IHealthCheckPort
/// </summary>
public class HealthCheckPortAdapter : IHealthCheckPort
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckPortAdapter(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <inheritdoc />
    public string Name => "AspNetCoreHealthCheck";

    /// <inheritdoc />
    public IEnumerable<string> Tags => ["api", "infrastructure"];

    public async Task<Mystira.Contracts.App.Ports.Health.HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var aspNetHealthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);

        return new Mystira.Contracts.App.Ports.Health.HealthReport
        {
            Status = MapHealthStatus(aspNetHealthReport.Status),
            TotalDuration = aspNetHealthReport.TotalDuration,
            Entries = aspNetHealthReport.Entries.ToDictionary(
                e => e.Key,
                e => new Mystira.Contracts.App.Ports.Health.HealthCheckEntry
                {
                    Status = MapHealthStatus(e.Value.Status),
                    Description = e.Value.Description,
                    Duration = e.Value.Duration,
                    Exception = e.Value.Exception,
                    Data = e.Value.Data != null ? new Dictionary<string, object>(e.Value.Data) : null
                })
        };
    }

    private static Mystira.Contracts.App.Ports.Health.HealthStatus MapHealthStatus(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus status)
    {
        return status switch
        {
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => Mystira.Contracts.App.Ports.Health.HealthStatus.Healthy,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => Mystira.Contracts.App.Ports.Health.HealthStatus.Degraded,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy => Mystira.Contracts.App.Ports.Health.HealthStatus.Unhealthy,
            _ => Mystira.Contracts.App.Ports.Health.HealthStatus.Unhealthy
        };
    }
}

