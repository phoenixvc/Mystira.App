namespace Mystira.App.PWA.Services;

/// <summary>
/// Service for managing API endpoint configuration with localStorage persistence.
/// This ensures the user's selected API endpoint survives PWA updates and refreshes.
/// </summary>
public interface IApiConfigurationService
{
    /// <summary>
    /// Gets the current API base URL (either persisted or default from config).
    /// </summary>
    Task<string> GetApiBaseUrlAsync();

    /// <summary>
    /// Gets the current Admin API base URL.
    /// </summary>
    Task<string> GetAdminApiBaseUrlAsync();

    /// <summary>
    /// Gets the current environment name (e.g., "Production", "Staging", "Development").
    /// </summary>
    Task<string> GetCurrentEnvironmentAsync();

    /// <summary>
    /// Sets and persists the API base URL to localStorage.
    /// Validates the URL before persisting.
    /// </summary>
    /// <param name="apiBaseUrl">The API URL to persist.</param>
    /// <param name="environmentName">The environment name (optional).</param>
    /// <exception cref="ArgumentException">Thrown if the URL is invalid.</exception>
    Task SetApiBaseUrlAsync(string apiBaseUrl, string? environmentName = null);

    /// <summary>
    /// Gets the list of available API endpoints from configuration.
    /// </summary>
    Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync();

    /// <summary>
    /// Checks if endpoint switching is allowed for this environment.
    /// Synchronous because it only reads from configuration.
    /// </summary>
    bool IsEndpointSwitchingAllowed();

    /// <summary>
    /// Clears the persisted endpoint, reverting to the default from config.
    /// </summary>
    Task ClearPersistedEndpointAsync();

    /// <summary>
    /// Validates an endpoint by checking if it's reachable.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>Health check result with status and any error message.</returns>
    Task<EndpointHealthResult> ValidateEndpointAsync(string url);

    /// <summary>
    /// Event raised when the API endpoint changes.
    /// </summary>
    event EventHandler<ApiEndpointChangedEventArgs>? EndpointChanged;
}

/// <summary>
/// Represents an available API endpoint configuration.
/// </summary>
public record ApiEndpoint
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
}

/// <summary>
/// Event args for API endpoint changes.
/// </summary>
public class ApiEndpointChangedEventArgs : EventArgs
{
    public string OldUrl { get; init; } = string.Empty;
    public string NewUrl { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
}

/// <summary>
/// Result of an endpoint health check.
/// </summary>
public record EndpointHealthResult
{
    public string Url { get; init; } = string.Empty;
    public bool IsHealthy { get; init; }
    public int? StatusCode { get; init; }
    public int ResponseTimeMs { get; init; }
    public string? ErrorMessage { get; init; }
}
