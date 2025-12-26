namespace Mystira.App.Api.Models;

/// <summary>
/// Local models that are not yet in Mystira.Contracts package.
/// Types that exist in Contracts should be imported from there instead.
/// </summary>

// Common models - kept locally as Contracts uses different naming
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
}

public class ErrorResponse
{
    public string? Error { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public string? TraceId { get; set; }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object>? Results { get; set; }
}
