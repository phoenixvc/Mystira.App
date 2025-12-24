using Microsoft.Extensions.Diagnostics.HealthChecks;
using ContractsPorts = Mystira.Contracts.App.Ports.Health;

namespace Mystira.App.Shared.Adapters;

/// <summary>
/// Adapter that adapts ASP.NET Core HealthCheckService to Contracts.Ports.Health.IHealthCheckPort
/// </summary>
public class HealthCheckPortAdapter : ContractsPorts.IHealthCheckPort
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckPortAdapter(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public string Name => "default";

    public IEnumerable<string> Tags => Array.Empty<string>();

    public async Task<ContractsPorts.HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var aspNetHealthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);

        return new ContractsPorts.HealthReport
        {
            Status = aspNetHealthReport.Status.ToString(),
            Duration = aspNetHealthReport.TotalDuration,
            Entries = aspNetHealthReport.Entries.ToDictionary(
                e => e.Key,
                e => new ContractsPorts.HealthCheckEntry
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
