using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mystira.App.Shared.Telemetry;

/// <summary>
/// Service for tracking user journey events and key business flows.
/// Use this to measure user engagement and identify drop-off points.
/// </summary>
public interface IUserJourneyAnalytics
{
    /// <summary>
    /// Tracks when a user starts a session (app opened).
    /// </summary>
    void TrackSessionStart(string? userId, string? deviceType = null, string? appVersion = null);

    /// <summary>
    /// Tracks when a user completes login.
    /// </summary>
    void TrackLogin(string? userId, string loginMethod, bool isNewUser = false);

    /// <summary>
    /// Tracks when a user views their profile.
    /// </summary>
    void TrackProfileView(string? userId, bool isOwnProfile = true);

    /// <summary>
    /// Tracks when a user starts playing a scenario.
    /// </summary>
    void TrackScenarioStart(string? userId, string scenarioId, string? scenarioName = null);

    /// <summary>
    /// Tracks when a user completes a scenario.
    /// </summary>
    void TrackScenarioComplete(string? userId, string scenarioId, TimeSpan duration, int? score = null);

    /// <summary>
    /// Tracks when a user abandons a scenario mid-play.
    /// </summary>
    void TrackScenarioAbandon(string? userId, string scenarioId, TimeSpan timeSpent, string? abandonReason = null);

    /// <summary>
    /// Tracks content interaction (view, like, share, etc.).
    /// </summary>
    void TrackContentInteraction(string? userId, string contentId, string interactionType);

    /// <summary>
    /// Tracks a navigation event within the app.
    /// </summary>
    void TrackNavigation(string? userId, string fromPage, string toPage);

    /// <summary>
    /// Tracks a feature usage event.
    /// </summary>
    void TrackFeatureUsage(string? userId, string featureName, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks user flow completion (e.g., onboarding, purchase).
    /// </summary>
    void TrackFlowComplete(string? userId, string flowName, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks a custom user journey milestone.
    /// </summary>
    void TrackMilestone(string? userId, string milestoneName, IDictionary<string, string>? properties = null);
}

/// <summary>
/// Implementation of IUserJourneyAnalytics that sends telemetry to Application Insights.
/// </summary>
public class UserJourneyAnalytics : IUserJourneyAnalytics
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<UserJourneyAnalytics> _logger;
    private readonly string _environment;

    // Regex for validating metric name components (prevents cardinality explosion)
    private static readonly Regex ValidMetricNamePattern = new(@"^[a-zA-Z][a-zA-Z0-9_]{0,31}$", RegexOptions.Compiled);

    // Known/allowed values for dynamic metric components (whitelist approach)
    private static readonly HashSet<string> AllowedInteractionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "View", "Play", "Like", "Share", "Download", "Favorite", "Comment", "Rate", "Complete", "Skip"
    };

    private static readonly HashSet<string> AllowedFlowNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Onboarding", "Registration", "Purchase", "ProfileSetup", "Tutorial", "FirstScenario", "Checkout"
    };

    public UserJourneyAnalytics(
        TelemetryClient? telemetryClient,
        ILogger<UserJourneyAnalytics> logger,
        string environment)
    {
        _telemetryClient = telemetryClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? "Unknown";
    }

    /// <summary>
    /// Validates and sanitizes a metric name component to prevent cardinality explosion.
    /// Returns sanitized value or "Other" if invalid.
    /// </summary>
    private static string SanitizeMetricComponent(string? value, string context)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        // Check if it matches valid pattern (alphanumeric, starts with letter, max 32 chars)
        if (ValidMetricNamePattern.IsMatch(value))
            return value;

        // If invalid, return a safe default to prevent unbounded cardinality
        return "Other";
    }

    /// <summary>
    /// Gets a safe interaction type for metric names.
    /// </summary>
    private static string GetSafeInteractionType(string interactionType)
    {
        return AllowedInteractionTypes.Contains(interactionType) ? interactionType : "Other";
    }

    /// <summary>
    /// Gets a safe flow name for metric names.
    /// </summary>
    private static string GetSafeFlowName(string flowName)
    {
        return AllowedFlowNames.Contains(flowName) ? flowName : SanitizeMetricComponent(flowName, "flow");
    }

    public void TrackSessionStart(string? userId, string? deviceType = null, string? appVersion = null)
    {
        var properties = CreateBaseProperties(userId);
        if (!string.IsNullOrEmpty(deviceType))
            properties["DeviceType"] = deviceType;
        if (!string.IsNullOrEmpty(appVersion))
            properties["AppVersion"] = appVersion;

        TrackEvent("Journey.SessionStart", properties);
        TrackMetric("Journey.DailyActiveUsers", 1, properties);

        _logger.LogInformation("Session started for user {UserId} on {DeviceType}", userId ?? "anonymous", deviceType ?? "unknown");
    }

    public void TrackLogin(string? userId, string loginMethod, bool isNewUser = false)
    {
        var properties = CreateBaseProperties(userId);
        properties["LoginMethod"] = loginMethod;
        properties["IsNewUser"] = isNewUser.ToString();

        TrackEvent("Journey.Login", properties);

        if (isNewUser)
        {
            TrackEvent("Journey.NewUserRegistration", properties);
            TrackMetric("Journey.NewUsers", 1, properties);
        }

        _logger.LogInformation("User {UserId} logged in via {Method} (new: {IsNew})", userId, loginMethod, isNewUser);
    }

    public void TrackProfileView(string? userId, bool isOwnProfile = true)
    {
        var properties = CreateBaseProperties(userId);
        properties["IsOwnProfile"] = isOwnProfile.ToString();

        TrackEvent("Journey.ProfileView", properties);

        _logger.LogDebug("User {UserId} viewed profile (own: {IsOwn})", userId, isOwnProfile);
    }

    public void TrackScenarioStart(string? userId, string scenarioId, string? scenarioName = null)
    {
        var properties = CreateBaseProperties(userId);
        properties["ScenarioId"] = scenarioId;
        if (!string.IsNullOrEmpty(scenarioName))
            properties["ScenarioName"] = scenarioName;

        TrackEvent("Journey.ScenarioStart", properties);
        TrackMetric("Journey.ScenarioPlays", 1, properties);

        _logger.LogInformation("User {UserId} started scenario {ScenarioId}", userId, scenarioId);
    }

    public void TrackScenarioComplete(string? userId, string scenarioId, TimeSpan duration, int? score = null)
    {
        var properties = CreateBaseProperties(userId);
        properties["ScenarioId"] = scenarioId;
        properties["DurationSeconds"] = duration.TotalSeconds.ToString("F1");
        if (score.HasValue)
            properties["Score"] = score.Value.ToString();

        TrackEvent("Journey.ScenarioComplete", properties);
        TrackMetric("Journey.ScenarioCompletions", 1, properties);
        TrackMetric("Journey.ScenarioDuration", duration.TotalSeconds, properties);

        _logger.LogInformation("User {UserId} completed scenario {ScenarioId} in {Duration}s",
            userId, scenarioId, duration.TotalSeconds);
    }

    public void TrackScenarioAbandon(string? userId, string scenarioId, TimeSpan timeSpent, string? abandonReason = null)
    {
        var properties = CreateBaseProperties(userId);
        properties["ScenarioId"] = scenarioId;
        properties["TimeSpentSeconds"] = timeSpent.TotalSeconds.ToString("F1");
        if (!string.IsNullOrEmpty(abandonReason))
            properties["AbandonReason"] = abandonReason;

        TrackEvent("Journey.ScenarioAbandon", properties);
        TrackMetric("Journey.ScenarioAbandons", 1, properties);

        _logger.LogInformation("User {UserId} abandoned scenario {ScenarioId} after {TimeSpent}s (reason: {Reason})",
            userId, scenarioId, timeSpent.TotalSeconds, abandonReason ?? "unknown");
    }

    public void TrackContentInteraction(string? userId, string contentId, string interactionType)
    {
        var properties = CreateBaseProperties(userId);
        properties["ContentId"] = contentId;
        properties["InteractionType"] = interactionType;

        // Use sanitized interaction type in metric names to prevent cardinality explosion
        var safeInteractionType = GetSafeInteractionType(interactionType);
        TrackEvent($"Journey.Content.{safeInteractionType}", properties);
        TrackMetric($"Journey.ContentInteractions.{safeInteractionType}", 1, properties);

        _logger.LogDebug("User {UserId} {Interaction} content {ContentId}", userId, interactionType, contentId);
    }

    public void TrackNavigation(string? userId, string fromPage, string toPage)
    {
        var properties = CreateBaseProperties(userId);
        properties["FromPage"] = fromPage;
        properties["ToPage"] = toPage;

        TrackEvent("Journey.Navigation", properties);

        _logger.LogDebug("User {UserId} navigated from {From} to {To}", userId, fromPage, toPage);
    }

    public void TrackFeatureUsage(string? userId, string featureName, IDictionary<string, string>? properties = null)
    {
        var eventProperties = CreateBaseProperties(userId);
        eventProperties["FeatureName"] = featureName;

        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                eventProperties[kvp.Key] = kvp.Value;
            }
        }

        // Use sanitized feature name in metric names to prevent cardinality explosion
        var safeFeatureName = SanitizeMetricComponent(featureName, "feature");
        TrackEvent($"Journey.Feature.{safeFeatureName}", eventProperties);
        TrackMetric($"Journey.FeatureUsage.{safeFeatureName}", 1, eventProperties);

        _logger.LogDebug("User {UserId} used feature {Feature}", userId, featureName);
    }

    public void TrackFlowComplete(string? userId, string flowName, TimeSpan duration, bool success)
    {
        var properties = CreateBaseProperties(userId);
        properties["FlowName"] = flowName;
        properties["DurationSeconds"] = duration.TotalSeconds.ToString("F1");
        properties["Success"] = success.ToString();

        // Use sanitized flow name in metric names to prevent cardinality explosion
        var safeFlowName = GetSafeFlowName(flowName);
        TrackEvent($"Journey.Flow.{safeFlowName}.{(success ? "Complete" : "Failed")}", properties);
        TrackMetric($"Journey.FlowCompletion.{safeFlowName}", success ? 1 : 0, properties);
        TrackMetric($"Journey.FlowDuration.{safeFlowName}", duration.TotalSeconds, properties);

        _logger.LogInformation("User {UserId} {Status} flow {Flow} in {Duration}s",
            userId, success ? "completed" : "failed", flowName, duration.TotalSeconds);
    }

    public void TrackMilestone(string? userId, string milestoneName, IDictionary<string, string>? properties = null)
    {
        var eventProperties = CreateBaseProperties(userId);
        eventProperties["MilestoneName"] = milestoneName;

        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                eventProperties[kvp.Key] = kvp.Value;
            }
        }

        // Use sanitized milestone name in metric names to prevent cardinality explosion
        var safeMilestoneName = SanitizeMetricComponent(milestoneName, "milestone");
        TrackEvent($"Journey.Milestone.{safeMilestoneName}", eventProperties);
        TrackMetric($"Journey.Milestones.{safeMilestoneName}", 1, eventProperties);

        _logger.LogInformation("User {UserId} reached milestone {Milestone}", userId, milestoneName);
    }

    private Dictionary<string, string> CreateBaseProperties(string? userId)
    {
        return new Dictionary<string, string>
        {
            ["Environment"] = _environment,
            ["EventType"] = "UserJourney",
            ["UserId"] = userId ?? "anonymous",
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        };
    }

    private void TrackEvent(string name, IDictionary<string, string>? properties = null)
    {
        if (_telemetryClient == null)
        {
            _logger.LogDebug("TelemetryClient not available, skipping event: {EventName}", name);
            return;
        }

        _telemetryClient.TrackEvent(name, properties);
    }

    private void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
        if (_telemetryClient == null) return;

        var metric = new MetricTelemetry(name, value);
        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                metric.Properties[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackMetric(metric);
    }
}

/// <summary>
/// Extension methods for registering UserJourneyAnalytics with dependency injection.
/// </summary>
public static class UserJourneyAnalyticsExtensions
{
    /// <summary>
    /// Adds UserJourneyAnalytics to the service collection.
    /// </summary>
    public static IServiceCollection AddUserJourneyAnalytics(this IServiceCollection services, string environment)
    {
        services.AddSingleton<IUserJourneyAnalytics>(sp =>
        {
            var telemetryClient = sp.GetService<TelemetryClient>();
            var logger = sp.GetRequiredService<ILogger<UserJourneyAnalytics>>();
            return new UserJourneyAnalytics(telemetryClient, logger, environment);
        });

        return services;
    }
}
