using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mystira.App.Shared.Telemetry;

/// <summary>
/// Service for tracking custom business metrics in Application Insights.
/// Use this service to track KPIs, business events, and custom measurements.
/// </summary>
public interface ICustomMetrics
{
    /// <summary>
    /// Tracks a game session being started.
    /// </summary>
    void TrackGameSessionStarted(string scenarioId, string profileId, string? accountId = null);

    /// <summary>
    /// Tracks a game session being completed.
    /// </summary>
    void TrackGameSessionCompleted(string sessionId, string scenarioId, TimeSpan duration, int choicesMade);

    /// <summary>
    /// Tracks a user sign-up event.
    /// </summary>
    void TrackUserSignUp(string method);

    /// <summary>
    /// Tracks a user sign-in event.
    /// </summary>
    void TrackUserSignIn(string method, bool success);

    /// <summary>
    /// Tracks scenario content being viewed.
    /// </summary>
    void TrackScenarioViewed(string scenarioId, string? profileId = null);

    /// <summary>
    /// Tracks media being accessed.
    /// </summary>
    void TrackMediaAccessed(string mediaType, string? contentId = null);

    /// <summary>
    /// Tracks content being played (scenario, story, game, etc.).
    /// </summary>
    void TrackContentPlays(string contentType, string contentId, string? profileId = null);

    /// <summary>
    /// Tracks a custom metric value.
    /// </summary>
    void TrackMetric(string name, double value, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a custom event.
    /// </summary>
    void TrackEvent(string name, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);

    /// <summary>
    /// Tracks a dependency call (external service, database, etc.).
    /// </summary>
    void TrackDependency(string type, string name, string data, DateTimeOffset startTime, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks an exception.
    /// </summary>
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
}

/// <summary>
/// Implementation of ICustomMetrics that sends telemetry to Application Insights.
/// </summary>
public class CustomMetrics : ICustomMetrics
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<CustomMetrics> _logger;
    private readonly string _environment;

    public CustomMetrics(TelemetryClient? telemetryClient, ILogger<CustomMetrics> logger, string environment)
    {
        _telemetryClient = telemetryClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? "Unknown";
    }

    public void TrackGameSessionStarted(string scenarioId, string profileId, string? accountId = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["ScenarioId"] = scenarioId,
            ["ProfileId"] = profileId,
            ["Environment"] = _environment
        };

        if (!string.IsNullOrEmpty(accountId))
            properties["AccountId"] = accountId;

        TrackEvent("GameSession.Started", properties);
        TrackMetric("GameSessions.Started", 1, properties);

        _logger.LogInformation("Game session started for scenario {ScenarioId} by profile {ProfileId}",
            scenarioId, profileId);
    }

    public void TrackGameSessionCompleted(string sessionId, string scenarioId, TimeSpan duration, int choicesMade)
    {
        var properties = new Dictionary<string, string>
        {
            ["SessionId"] = sessionId,
            ["ScenarioId"] = scenarioId,
            ["Environment"] = _environment
        };

        var metrics = new Dictionary<string, double>
        {
            ["DurationSeconds"] = duration.TotalSeconds,
            ["ChoicesMade"] = choicesMade
        };

        TrackEvent("GameSession.Completed", properties, metrics);
        TrackMetric("GameSessions.Completed", 1, properties);
        TrackMetric("GameSessions.Duration", duration.TotalSeconds, properties);

        _logger.LogInformation("Game session {SessionId} completed for scenario {ScenarioId}. Duration: {Duration}, Choices: {Choices}",
            sessionId, scenarioId, duration, choicesMade);
    }

    public void TrackUserSignUp(string method)
    {
        var properties = new Dictionary<string, string>
        {
            ["Method"] = method,
            ["Environment"] = _environment
        };

        TrackEvent("User.SignUp", properties);
        TrackMetric("Users.SignUps", 1, properties);

        _logger.LogInformation("User signed up via {Method}", method);
    }

    public void TrackUserSignIn(string method, bool success)
    {
        var properties = new Dictionary<string, string>
        {
            ["Method"] = method,
            ["Success"] = success.ToString(),
            ["Environment"] = _environment
        };

        TrackEvent(success ? "User.SignIn.Success" : "User.SignIn.Failed", properties);
        TrackMetric(success ? "Users.SignIns.Success" : "Users.SignIns.Failed", 1, properties);

        if (success)
        {
            _logger.LogInformation("User signed in via {Method}", method);
        }
        else
        {
            _logger.LogWarning("User sign-in failed via {Method}", method);
        }
    }

    public void TrackScenarioViewed(string scenarioId, string? profileId = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["ScenarioId"] = scenarioId,
            ["Environment"] = _environment
        };

        if (!string.IsNullOrEmpty(profileId))
            properties["ProfileId"] = profileId;

        TrackEvent("Scenario.Viewed", properties);
        TrackMetric("Scenarios.Views", 1, properties);
    }

    public void TrackMediaAccessed(string mediaType, string? contentId = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["MediaType"] = mediaType,
            ["Environment"] = _environment
        };

        if (!string.IsNullOrEmpty(contentId))
            properties["ContentId"] = contentId;

        TrackEvent("Media.Accessed", properties);
        TrackMetric("Media.Accesses", 1, properties);
    }

    public void TrackContentPlays(string contentType, string contentId, string? profileId = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["ContentType"] = contentType,
            ["ContentId"] = contentId,
            ["Environment"] = _environment
        };

        if (!string.IsNullOrEmpty(profileId))
            properties["ProfileId"] = profileId;

        TrackEvent("Content.Play", properties);
        TrackMetric("Mystira.ContentPlays", 1, properties);

        _logger.LogInformation("Content played: {ContentType}/{ContentId} by profile {ProfileId}",
            contentType, contentId, profileId ?? "anonymous");
    }

    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
        if (_telemetryClient == null)
        {
            _logger.LogDebug("Metric tracked (no App Insights): {Name} = {Value}", name, value);
            return;
        }

        var metric = new MetricTelemetry(name, value);

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                metric.Properties[prop.Key] = prop.Value;
            }
        }

        _telemetryClient.TrackMetric(metric);
    }

    public void TrackEvent(string name, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        if (_telemetryClient == null)
        {
            _logger.LogDebug("Event tracked (no App Insights): {Name}", name);
            return;
        }

        var eventTelemetry = new EventTelemetry(name);

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                eventTelemetry.Properties[prop.Key] = prop.Value;
            }
        }

        if (metrics != null)
        {
            foreach (var metric in metrics)
            {
                eventTelemetry.Metrics[metric.Key] = metric.Value;
            }
        }

        _telemetryClient.TrackEvent(eventTelemetry);
    }

    public void TrackDependency(string type, string name, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        if (_telemetryClient == null)
        {
            _logger.LogDebug("Dependency tracked (no App Insights): {Type}/{Name} - Success: {Success}", type, name, success);
            return;
        }

        var dependency = new DependencyTelemetry
        {
            Type = type,
            Name = name,
            Data = data,
            Timestamp = startTime,
            Duration = duration,
            Success = success
        };

        dependency.Properties["Environment"] = _environment;

        _telemetryClient.TrackDependency(dependency);
    }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        if (_telemetryClient == null)
        {
            _logger.LogError(exception, "Exception tracked (no App Insights): {ExceptionType}", exception.GetType().Name);
            return;
        }

        var exceptionTelemetry = new ExceptionTelemetry(exception);

        exceptionTelemetry.Properties["Environment"] = _environment;

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                exceptionTelemetry.Properties[prop.Key] = prop.Value;
            }
        }

        _telemetryClient.TrackException(exceptionTelemetry);
    }
}

/// <summary>
/// Extension methods for registering CustomMetrics in DI.
/// </summary>
public static class CustomMetricsExtensions
{
    /// <summary>
    /// Adds ICustomMetrics service to the DI container.
    /// </summary>
    public static IServiceCollection AddCustomMetrics(this IServiceCollection services, string environment)
    {
        services.AddSingleton<ICustomMetrics>(sp =>
        {
            var telemetryClient = sp.GetService<TelemetryClient>();
            var logger = sp.GetRequiredService<ILogger<CustomMetrics>>();
            return new CustomMetrics(telemetryClient, logger, environment);
        });

        return services;
    }
}
