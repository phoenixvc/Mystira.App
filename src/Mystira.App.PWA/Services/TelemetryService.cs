using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Service for tracking telemetry events via Application Insights.
/// Uses JSInterop to call the Application Insights SDK loaded in index.html.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Tracks a custom event.
    /// </summary>
    Task TrackEventAsync(string eventName, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks an exception.
    /// </summary>
    Task TrackExceptionAsync(Exception exception, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a metric.
    /// </summary>
    Task TrackMetricAsync(string metricName, double value, IDictionary<string, string>? properties = null);
}

/// <summary>
/// Implementation of telemetry service using Application Insights via JSInterop.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(IJSRuntime jsRuntime, ILogger<TelemetryService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task TrackEventAsync(string eventName, IDictionary<string, string>? properties = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("mystiraTelemetry.trackEvent", eventName, properties);
        }
        catch (Exception ex)
        {
            // Don't throw on telemetry failures - it's non-critical
            _logger.LogDebug(ex, "Failed to track event: {EventName}", eventName);
        }
    }

    public async Task TrackExceptionAsync(Exception exception, IDictionary<string, string>? properties = null)
    {
        try
        {
            var errorProps = new Dictionary<string, string>(properties ?? new Dictionary<string, string>())
            {
                ["message"] = exception.Message,
                ["type"] = exception.GetType().Name,
                ["stackTrace"] = exception.StackTrace ?? string.Empty
            };

            await _jsRuntime.InvokeVoidAsync("mystiraTelemetry.trackException", errorProps);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to track exception");
        }
    }

    public async Task TrackMetricAsync(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("mystiraTelemetry.trackMetric", metricName, value, properties);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to track metric: {MetricName}", metricName);
        }
    }
}
