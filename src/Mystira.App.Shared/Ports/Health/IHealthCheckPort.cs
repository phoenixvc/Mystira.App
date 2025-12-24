namespace Mystira.Contracts.App.Ports.Health;

/// <summary>
/// Port interface for health check operations.
/// Implementations wrap ASP.NET Core health check infrastructure.
/// </summary>
public interface IHealthCheckPort
{
    /// <summary>
    /// Execute all health checks
    /// </summary>
    Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Platform-agnostic health report
/// </summary>
public class HealthReport
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, HealthCheckEntry> Entries { get; set; } = new();
}

/// <summary>
/// Individual health check entry
/// </summary>
public class HealthCheckEntry
{
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Exception { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}
