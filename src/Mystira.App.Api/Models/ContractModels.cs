namespace Mystira.App.Api.Models;

/// <summary>
/// Local API models that are not in Mystira.Contracts package.
/// </summary>

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

public class ValidationErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, List<string>> ValidationErrors { get; set; } = new();
    public string? TraceId { get; set; }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object>? Results { get; set; }
}
