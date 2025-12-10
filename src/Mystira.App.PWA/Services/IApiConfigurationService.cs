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
    /// Checks if endpoint switching is enabled in configuration.
    /// Note: Use IsUserAllowedToSwitchEndpoints for user-specific checks.
    /// </summary>
    bool IsEndpointSwitchingAllowed();

    /// <summary>
    /// Checks if a specific user (by email) is allowed to switch endpoints.
    /// Returns true if: endpoint switching is enabled AND (no allowlist configured OR email is in allowlist).
    /// </summary>
    /// <param name="userEmail">The user's email address, or null if not authenticated.</param>
    bool IsUserAllowedToSwitchEndpoints(string? userEmail);

    /// <summary>
    /// Gets the list of email addresses allowed to switch endpoints.
    /// Returns empty list if no allowlist is configured (meaning all users can switch if enabled).
    /// </summary>
    IReadOnlyList<string> GetAllowedSwitchingEmails();

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
    /// Invalidates the cached endpoints list, forcing a reload from configuration on next access.
    /// </summary>
    void InvalidateEndpointsCache();
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
