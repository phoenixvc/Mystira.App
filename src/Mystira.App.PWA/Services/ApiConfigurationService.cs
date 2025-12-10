using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Service that manages API endpoint configuration with localStorage persistence.
/// This service ensures that user-selected API endpoints survive PWA updates,
/// solving the issue of having to re-add domains after each release.
/// </summary>
public class ApiConfigurationService : IApiConfigurationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiConfigurationService> _logger;

    // LocalStorage keys - using specific prefix to avoid conflicts
    private const string ApiUrlStorageKey = "mystira_api_base_url";
    private const string AdminApiUrlStorageKey = "mystira_admin_api_base_url";
    private const string EnvironmentStorageKey = "mystira_api_environment";

    // Cache for configuration values
    private string? _cachedApiUrl;
    private string? _cachedAdminApiUrl;
    private string? _cachedEnvironment;
    private List<ApiEndpoint>? _cachedEndpoints;
    private bool _isInitialized;

    public event EventHandler<ApiEndpointChangedEventArgs>? EndpointChanged;

    public ApiConfigurationService(
        IJSRuntime jsRuntime,
        IConfiguration configuration,
        ILogger<ApiConfigurationService> logger)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetApiBaseUrlAsync()
    {
        await EnsureInitializedAsync();
        return _cachedApiUrl ?? GetDefaultApiUrl();
    }

    /// <inheritdoc />
    public async Task<string> GetAdminApiBaseUrlAsync()
    {
        await EnsureInitializedAsync();
        return _cachedAdminApiUrl ?? GetDefaultAdminApiUrl();
    }

    /// <inheritdoc />
    public async Task<string> GetCurrentEnvironmentAsync()
    {
        await EnsureInitializedAsync();
        return _cachedEnvironment ?? GetDefaultEnvironment();
    }

    /// <inheritdoc />
    public async Task SetApiBaseUrlAsync(string apiBaseUrl, string? environmentName = null)
    {
        var oldUrl = await GetApiBaseUrlAsync();

        try
        {
            // Persist to localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ApiUrlStorageKey, apiBaseUrl);

            if (!string.IsNullOrEmpty(environmentName))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", EnvironmentStorageKey, environmentName);
            }

            // Find matching admin API URL from available endpoints
            var endpoints = await GetAvailableEndpointsAsync();
            var matchingEndpoint = endpoints.FirstOrDefault(e =>
                e.Url.Equals(apiBaseUrl, StringComparison.OrdinalIgnoreCase));

            if (matchingEndpoint != null)
            {
                // Derive admin API URL from the same environment
                var adminApiUrl = DeriveAdminApiUrl(apiBaseUrl);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AdminApiUrlStorageKey, adminApiUrl);
                _cachedAdminApiUrl = adminApiUrl;
            }

            // Update cache
            _cachedApiUrl = apiBaseUrl;
            _cachedEnvironment = environmentName ?? _cachedEnvironment;

            _logger.LogInformation("API endpoint changed from {OldUrl} to {NewUrl} (Environment: {Environment})",
                oldUrl, apiBaseUrl, environmentName);

            // Raise event
            EndpointChanged?.Invoke(this, new ApiEndpointChangedEventArgs
            {
                OldUrl = oldUrl,
                NewUrl = apiBaseUrl,
                Environment = environmentName ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist API endpoint to localStorage");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync()
    {
        if (_cachedEndpoints != null)
        {
            return _cachedEndpoints;
        }

        var endpoints = new List<ApiEndpoint>();

        try
        {
            var endpointsSection = _configuration.GetSection("ApiConfiguration:AvailableEndpoints");
            if (endpointsSection.Exists())
            {
                foreach (var child in endpointsSection.GetChildren())
                {
                    var endpoint = new ApiEndpoint
                    {
                        Name = child["name"] ?? string.Empty,
                        Url = child["url"] ?? string.Empty,
                        Environment = child["environment"] ?? string.Empty
                    };

                    if (!string.IsNullOrEmpty(endpoint.Url))
                    {
                        endpoints.Add(endpoint);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load available endpoints from configuration");
        }

        // If no endpoints configured, create a default one
        if (endpoints.Count == 0)
        {
            endpoints.Add(new ApiEndpoint
            {
                Name = "Default",
                Url = GetDefaultApiUrl(),
                Environment = GetDefaultEnvironment()
            });
        }

        _cachedEndpoints = endpoints;
        return endpoints;
    }

    /// <inheritdoc />
    public async Task<bool> IsEndpointSwitchingAllowedAsync()
    {
        var allowSwitching = _configuration.GetValue<bool>("ApiConfiguration:AllowEndpointSwitching");
        return allowSwitching;
    }

    /// <inheritdoc />
    public async Task ClearPersistedEndpointAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ApiUrlStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AdminApiUrlStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", EnvironmentStorageKey);

            // Reset cache
            _cachedApiUrl = null;
            _cachedAdminApiUrl = null;
            _cachedEnvironment = null;
            _isInitialized = false;

            _logger.LogInformation("Cleared persisted API endpoint, reverting to default configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear persisted API endpoint from localStorage");
            throw;
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            // Try to load persisted values from localStorage
            var persistedApiUrl = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ApiUrlStorageKey);
            var persistedAdminApiUrl = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AdminApiUrlStorageKey);
            var persistedEnvironment = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", EnvironmentStorageKey);

            if (!string.IsNullOrEmpty(persistedApiUrl))
            {
                _cachedApiUrl = persistedApiUrl;
                _logger.LogInformation("Loaded persisted API URL from localStorage: {ApiUrl}", persistedApiUrl);
            }
            else
            {
                _cachedApiUrl = GetDefaultApiUrl();
                _logger.LogInformation("Using default API URL from configuration: {ApiUrl}", _cachedApiUrl);
            }

            if (!string.IsNullOrEmpty(persistedAdminApiUrl))
            {
                _cachedAdminApiUrl = persistedAdminApiUrl;
            }
            else
            {
                _cachedAdminApiUrl = GetDefaultAdminApiUrl();
            }

            _cachedEnvironment = !string.IsNullOrEmpty(persistedEnvironment)
                ? persistedEnvironment
                : GetDefaultEnvironment();

            _isInitialized = true;
        }
        catch (InvalidOperationException)
        {
            // JSInterop not available yet (pre-render). Use defaults.
            _cachedApiUrl = GetDefaultApiUrl();
            _cachedAdminApiUrl = GetDefaultAdminApiUrl();
            _cachedEnvironment = GetDefaultEnvironment();
            _logger.LogDebug("JSInterop not available, using default configuration values");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted configuration from localStorage, using defaults");
            _cachedApiUrl = GetDefaultApiUrl();
            _cachedAdminApiUrl = GetDefaultAdminApiUrl();
            _cachedEnvironment = GetDefaultEnvironment();
        }
    }

    private string GetDefaultApiUrl()
    {
        // Try new ApiConfiguration section first, then fall back to ConnectionStrings
        var apiUrl = _configuration.GetValue<string>("ApiConfiguration:DefaultApiBaseUrl")
                     ?? _configuration.GetConnectionString("MystiraApiBaseUrl")
                     ?? "https://api.mystira.app/";

        return EnsureTrailingSlash(apiUrl);
    }

    private string GetDefaultAdminApiUrl()
    {
        var adminApiUrl = _configuration.GetValue<string>("ApiConfiguration:AdminApiBaseUrl");

        if (!string.IsNullOrEmpty(adminApiUrl))
        {
            return EnsureTrailingSlash(adminApiUrl);
        }

        // Derive from default API URL
        return DeriveAdminApiUrl(GetDefaultApiUrl());
    }

    private string GetDefaultEnvironment()
    {
        return _configuration.GetValue<string>("ApiConfiguration:Environment") ?? "Production";
    }

    private static string DeriveAdminApiUrl(string apiUrl)
    {
        // Transform api.mystira.app -> adminapi.mystira.app
        // Transform api.dev.mystira.app -> adminapi.dev.mystira.app
        try
        {
            var uri = new Uri(apiUrl);
            var host = uri.Host;

            if (host.StartsWith("api."))
            {
                var newHost = "adminapi" + host.Substring(3);
                return $"{uri.Scheme}://{newHost}{uri.AbsolutePath}";
            }

            // If we can't derive, return same URL
            return apiUrl;
        }
        catch
        {
            return apiUrl;
        }
    }

    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith('/') ? url : url + "/";
    }
}
