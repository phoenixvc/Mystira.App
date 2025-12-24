using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.Contracts.App.Ports.Health;

namespace Mystira.App.Shared.Adapters;

/// <summary>
/// Adapter that adapts ASP.NET Core HealthCheckService to Contracts.Ports.Health.IHealthCheckPort
/// </summary>
public class HealthCheckPortAdapter : IHealthCheckPort
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckPortAdapter(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<Mystira.App.Contracts.Ports.Health.HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var aspNetHealthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);

        return new Mystira.App.Contracts.Ports.Health.HealthReport
        {
            Status = aspNetHealthReport.Status.ToString(),
            Duration = aspNetHealthReport.TotalDuration,
            Entries = aspNetHealthReport.Entries.ToDictionary(
                e => e.Key,
                e => new HealthCheckEntry
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
