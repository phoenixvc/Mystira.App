using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mystira.App.Api.Services;

public interface IHealthCheckService
{
    Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);
}
