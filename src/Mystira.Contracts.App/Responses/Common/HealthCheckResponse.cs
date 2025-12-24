namespace Mystira.Contracts.App.Responses.Common;

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

