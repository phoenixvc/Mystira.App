using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mystira.App.Shared.Logging;

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

    /// <summary>
    /// Tracks a database query execution with performance metrics.
    /// </summary>
    void TrackDatabaseQuery(string queryName, string? collection, TimeSpan duration, bool success, double? requestUnits = null);

    /// <summary>
    /// Tracks blob storage operation performance.
    /// </summary>
    void TrackBlobOperation(string operation, string containerName, long? bytesTransferred, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks cache hit/miss for query caching.
    /// </summary>
    void TrackCacheAccess(string cacheKey, bool hit);

    /// <summary>
    /// Tracks JWT token refresh attempt.
    /// </summary>
    void TrackTokenRefresh(string? userId, bool success, string? failureReason = null);

    /// <summary>
    /// Tracks dual-write operation failure in polyglot persistence.
    /// </summary>
    void TrackDualWriteFailure(string entityType, string operation, string errorMessage, bool compensationEnabled);
}

/// <summary>
/// Implementation of ICustomMetrics that sends telemetry to Application Insights.
/// </summary>
public partial class CustomMetrics : ICustomMetrics
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

    public void TrackDatabaseQuery(string queryName, string? collection, TimeSpan duration, bool success, double? requestUnits = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["QueryName"] = queryName,
            ["Success"] = success.ToString(),
            ["Environment"] = _environment
        };

        if (!string.IsNullOrEmpty(collection))
            properties["Collection"] = collection;

        var metrics = new Dictionary<string, double>
        {
            ["DurationMs"] = duration.TotalMilliseconds
        };

        if (requestUnits.HasValue)
            metrics["RequestUnits"] = requestUnits.Value;

        TrackEvent("Database.Query", properties, metrics);
        TrackMetric($"Database.Query.{queryName}.Duration", duration.TotalMilliseconds, properties);

        if (requestUnits.HasValue)
            TrackMetric("Database.Query.RequestUnits", requestUnits.Value, properties);

        // Track slow queries (> 500ms)
        if (duration.TotalMilliseconds > 500)
        {
            _logger.LogWarning("Slow database query: {QueryName} took {Duration}ms (RU: {RU})",
                queryName, duration.TotalMilliseconds, requestUnits);
            TrackMetric("Database.SlowQueries", 1, properties);
        }
    }

    public void TrackBlobOperation(string operation, string containerName, long? bytesTransferred, TimeSpan duration, bool success)
    {
        var properties = new Dictionary<string, string>
        {
            ["Operation"] = operation,
            ["Container"] = containerName,
            ["Success"] = success.ToString(),
            ["Environment"] = _environment
        };

        var metrics = new Dictionary<string, double>
        {
            ["DurationMs"] = duration.TotalMilliseconds
        };

        if (bytesTransferred.HasValue)
            metrics["BytesTransferred"] = bytesTransferred.Value;

        TrackEvent($"BlobStorage.{operation}", properties, metrics);
        TrackMetric($"BlobStorage.{operation}.Duration", duration.TotalMilliseconds, properties);

        if (bytesTransferred.HasValue && duration.TotalSeconds > 0)
        {
            var throughputKBps = (bytesTransferred.Value / 1024.0) / duration.TotalSeconds;
            TrackMetric("BlobStorage.ThroughputKBps", throughputKBps, properties);
        }

        if (!success)
        {
            _logger.LogWarning("Blob storage operation failed: {Operation} on {Container}", operation, containerName);
            TrackMetric("BlobStorage.Failures", 1, properties);
        }
    }

    public void TrackCacheAccess(string cacheKey, bool hit)
    {
        var properties = new Dictionary<string, string>
        {
            ["CacheKey"] = cacheKey,
            ["Hit"] = hit.ToString(),
            ["Environment"] = _environment
        };

        TrackMetric(hit ? "Cache.Hits" : "Cache.Misses", 1, properties);

        _logger.LogDebug("Cache {Result} for key: {CacheKey}", hit ? "hit" : "miss", cacheKey);
    }

    public void TrackTokenRefresh(string? userId, bool success, string? failureReason = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["Success"] = success.ToString(),
            ["Environment"] = _environment
        };

        if (!string.IsNullOrEmpty(userId))
            properties["UserId"] = userId;

        if (!string.IsNullOrEmpty(failureReason))
            properties["FailureReason"] = failureReason;

        TrackEvent(success ? "Auth.TokenRefresh.Success" : "Auth.TokenRefresh.Failed", properties);
        TrackMetric(success ? "Auth.TokenRefreshes.Success" : "Auth.TokenRefreshes.Failed", 1, properties);

        if (!success)
        {
            _logger.LogWarning("Token refresh failed for user {UserId}: {Reason}", userId ?? "unknown", failureReason ?? "unknown");
        }
    }

    public void TrackDualWriteFailure(string entityType, string operation, string errorMessage, bool compensationEnabled)
    {
        // Sanitize error message to prevent sensitive data leakage
        var sanitizedError = SanitizeErrorMessage(errorMessage);

        var properties = new Dictionary<string, string>
        {
            ["EntityType"] = entityType,
            ["Operation"] = operation,
            ["ErrorMessage"] = sanitizedError,
            ["CompensationEnabled"] = compensationEnabled.ToString(),
            ["Environment"] = _environment,
            ["Severity"] = "High" // This should trigger alerting
        };

        TrackEvent("Polyglot.DualWrite.Failed", properties);
        TrackMetric("Polyglot.DualWriteFailures", 1, properties);

        _logger.LogError("Dual-write failure for {EntityType} during {Operation}. Compensation: {Compensation}. Error: {Error}",
            entityType, operation, compensationEnabled, sanitizedError);
    }

    /// <summary>
    /// Sanitizes error messages to prevent sensitive data leakage.
    /// Removes connection strings, credentials, emails, and truncates long messages.
    /// </summary>
    private static string SanitizeErrorMessage(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "[empty]";

        // Redact any email addresses that might be in the error message
        var sanitized = PiiRedactor.RedactEmailsInString(errorMessage);

        // Remove connection strings (common patterns)
        sanitized = ConnectionStringRegex().Replace(
            sanitized,
            "[CONNECTION_PARAM]=[REDACTED]");

        // Remove API keys and tokens
        sanitized = ApiKeyRegex().Replace(
            sanitized,
            "$1=[REDACTED]");

        // Remove newlines and carriage returns (log injection prevention)
        sanitized = sanitized.Replace("\r", "").Replace("\n", " ");

        // Truncate long error messages
        if (sanitized.Length > 200)
        {
            sanitized = sanitized[..200] + "...[truncated]";
        }

        return sanitized;
    }

    [GeneratedRegex(@"(Server|Data Source|Database|User Id|Password|Uid|Pwd|ConnectionString|AccountKey|AccountName|SharedAccessKey|SAS)=[^;]{1,256}", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionStringRegex();

    [GeneratedRegex(@"(api[_-]?key|token|bearer|authorization|secret)[\s:=""']+([\w_/+=-]{1,128})", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();
}

/// <summary>
/// Extension methods for registering CustomMetrics in DI.
/// </summary>
public static class CustomMetricsExtensions
{
    /// <summary>
    /// Adds ICustomMetrics service to the DI container.
    /// Logs a warning if Application Insights is not configured in non-development environments.
    /// </summary>
    public static IServiceCollection AddCustomMetrics(this IServiceCollection services, string environment)
    {
        services.AddSingleton<ICustomMetrics>(sp =>
        {
            var telemetryClient = sp.GetService<TelemetryClient>();
            var logger = sp.GetRequiredService<ILogger<CustomMetrics>>();
            var hostEnv = sp.GetService<IHostEnvironment>();

            // Warn if App Insights is not available in non-development environments
            if (telemetryClient == null && hostEnv != null && !hostEnv.IsDevelopment())
            {
                logger.LogError(
                    "Application Insights TelemetryClient is not configured in {Environment} environment! " +
                    "Metrics and telemetry will NOT be collected. " +
                    "Ensure APPLICATIONINSIGHTS_CONNECTION_STRING is set.",
                    environment);
            }
            else if (telemetryClient == null)
            {
                logger.LogWarning(
                    "Application Insights TelemetryClient is not available. " +
                    "Metrics will be logged locally but not sent to Application Insights.");
            }
            else
            {
                logger.LogInformation(
                    "Application Insights telemetry initialized for environment: {Environment}",
                    environment);
            }

            return new CustomMetrics(telemetryClient, logger, environment);
        });

        return services;
    }
}
