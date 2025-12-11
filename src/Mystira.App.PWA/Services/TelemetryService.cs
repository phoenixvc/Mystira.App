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
/// Caches availability check to avoid repeated JSInterop calls when SDK is not loaded.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<TelemetryService> _logger;
    private bool? _isAvailable;
    private bool _availabilityChecked;

    public TelemetryService(IJSRuntime jsRuntime, ILogger<TelemetryService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task TrackEventAsync(string eventName, IDictionary<string, string>? properties = null)
    {
        if (!await IsAvailableAsync()) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("mystiraTelemetry.trackEvent", eventName, properties);
        }
        catch (Exception ex)
        {
            // Don't throw on telemetry failures - it's non-critical
            _logger.LogDebug(ex, "Failed to track event: {EventName}", eventName);
            _isAvailable = false; // Disable further attempts
        }
    }

    public async Task TrackExceptionAsync(Exception exception, IDictionary<string, string>? properties = null)
    {
        if (!await IsAvailableAsync()) return;

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
            _isAvailable = false;
        }
    }

    public async Task TrackMetricAsync(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        if (!await IsAvailableAsync()) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("mystiraTelemetry.trackMetric", metricName, value, properties);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to track metric: {MetricName}", metricName);
            _isAvailable = false;
        }
    }

    private async Task<bool> IsAvailableAsync()
    {
        if (_isAvailable.HasValue)
        {
            return _isAvailable.Value;
        }

        if (_availabilityChecked)
        {
            return false;
        }

        _availabilityChecked = true;

        try
        {
            // Check if mystiraTelemetry object is available (safer than eval)
            _isAvailable = await _jsRuntime.InvokeAsync<bool>("mystiraTelemetry.isAvailable");

            if (!_isAvailable.Value)
            {
                _logger.LogDebug("Application Insights telemetry SDK not available");
            }

            return _isAvailable.Value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check telemetry availability");
            _isAvailable = false;
            return false;
        }
    }
}
