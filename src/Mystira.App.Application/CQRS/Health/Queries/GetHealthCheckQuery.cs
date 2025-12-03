namespace Mystira.App.Application.CQRS.Health.Queries;

/// <summary>
/// Query to retrieve application health status and dependency checks.
/// </summary>
public record GetHealthCheckQuery : IQuery<HealthCheckResult>;

/// <summary>
/// Result containing health check status and details.
/// </summary>
public record HealthCheckResult(
    string Status,
    TimeSpan Duration,
    Dictionary<string, object> Results
);
