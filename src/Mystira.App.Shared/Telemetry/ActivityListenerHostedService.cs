using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mystira.App.Shared.Telemetry;

/// <summary>
/// Hosted service that manages the lifecycle of the ActivityListener for distributed tracing.
/// Ensures proper disposal of the listener when the application shuts down.
/// </summary>
public class ActivityListenerHostedService : IHostedService, IDisposable
{
    private readonly ILogger<ActivityListenerHostedService> _logger;
    private readonly TelemetryClient? _telemetryClient;
    private ActivityListener? _listener;

    public ActivityListenerHostedService(
        ILogger<ActivityListenerHostedService> logger,
        TelemetryClient? telemetryClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryClient = telemetryClient;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ActivityListener for distributed tracing");

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Mystira"),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                _logger.LogDebug("Activity started: {OperationName} ({TraceId})",
                    activity.OperationName, activity.TraceId);
            },
            ActivityStopped = activity =>
            {
                if (_telemetryClient != null && activity.Duration.TotalMilliseconds > 0)
                {
                    // Track as dependency for visibility in Application Map
                    var dependency = new DependencyTelemetry
                    {
                        Name = activity.OperationName,
                        Type = activity.GetTagItem("span.type")?.ToString() ?? "Internal",
                        Duration = activity.Duration,
                        Success = activity.Status != ActivityStatusCode.Error,
                        Timestamp = activity.StartTimeUtc
                    };

                    foreach (var tag in activity.Tags)
                    {
                        dependency.Properties[tag.Key] = tag.Value ?? "";
                    }

                    _telemetryClient.TrackDependency(dependency);
                }
            }
        };

        ActivitySource.AddActivityListener(_listener);
        _logger.LogInformation("ActivityListener started successfully");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping ActivityListener");
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_listener != null)
        {
            _listener.Dispose();
            _listener = null;
            _logger.LogInformation("ActivityListener disposed");
        }
    }
}
