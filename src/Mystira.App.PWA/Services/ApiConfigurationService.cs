using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Mystira.App.PWA.Services;

/// <summary>
/// Service that manages API endpoint configuration with localStorage persistence.
/// This service ensures that user-selected API endpoints survive PWA updates,
/// solving the issue of having to re-add domains after each release.
/// </summary>
public class ApiConfigurationService : IApiConfigurationService, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiConfigurationService> _logger;
    private readonly IApiEndpointCache _endpointCache;
    private readonly ITelemetryService _telemetry;
    private readonly HttpClient _healthCheckClient;
    private readonly List<string> _allowedEmails;
    private bool _disposed;

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

    public ApiConfigurationService(
        IJSRuntime jsRuntime,
        IConfiguration configuration,
        ILogger<ApiConfigurationService> logger,
        IApiEndpointCache endpointCache,
        ITelemetryService telemetry)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _logger = logger;
        _endpointCache = endpointCache;
        _telemetry = telemetry;

        // Create a simple HttpClient for health checks (no auth needed)
        _healthCheckClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        // Load allowed emails from configuration (case-insensitive matching)
        _allowedEmails = _configuration.GetSection("ApiConfiguration:AllowedSwitchingEmails")
            .Get<List<string>>() ?? new List<string>();
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
        // Validate URL before persisting
        if (!ValidateUrl(apiBaseUrl, out var validationError))
        {
            _logger.LogError("Invalid API URL: {Url}. Error: {Error}", apiBaseUrl, validationError);
            throw new ArgumentException($"Invalid API URL: {validationError}", nameof(apiBaseUrl));
        }

        var oldUrl = await GetApiBaseUrlAsync();

        try
        {
            // Persist to localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ApiUrlStorageKey, apiBaseUrl);

            if (!string.IsNullOrEmpty(environmentName))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", EnvironmentStorageKey, environmentName);
            }

            // Derive admin API URL
            var adminApiUrl = ApiEndpointCache.DeriveAdminApiUrl(apiBaseUrl);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AdminApiUrlStorageKey, adminApiUrl);
            _cachedAdminApiUrl = adminApiUrl;

            // Update cache
            _cachedApiUrl = apiBaseUrl;
            _cachedEnvironment = environmentName ?? _cachedEnvironment;

            // Update singleton cache for handlers
            _endpointCache.Update(apiBaseUrl, adminApiUrl);

            _logger.LogInformation("API endpoint changed from {OldUrl} to {NewUrl} (Environment: {Environment})",
                oldUrl, apiBaseUrl, environmentName);

            // Track endpoint change in Application Insights
            await _telemetry.TrackEventAsync("EndpointChanged", new Dictionary<string, string>
            {
                ["OldUrl"] = oldUrl,
                ["NewUrl"] = apiBaseUrl,
                ["Environment"] = environmentName ?? "Unknown",
                ["Timestamp"] = DateTime.UtcNow.ToString("O")
            });
        }
        catch (Exception ex) when (ex is not ArgumentException)
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
                        Name = child["Name"] ?? child["name"] ?? string.Empty,
                        Url = child["Url"] ?? child["url"] ?? string.Empty,
                        Environment = child["Environment"] ?? child["environment"] ?? string.Empty
                    };

                    if (!string.IsNullOrEmpty(endpoint.Url) && ValidateUrl(endpoint.Url, out _))
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
    public bool IsEndpointSwitchingAllowed()
    {
        return _configuration.GetValue<bool>("ApiConfiguration:AllowEndpointSwitching");
    }

    /// <inheritdoc />
    public bool IsUserAllowedToSwitchEndpoints(string? userEmail)
    {
        // First check if switching is enabled at all
        if (!IsEndpointSwitchingAllowed())
        {
            return false;
        }

        // If no allowlist configured, all users can switch
        if (_allowedEmails.Count == 0)
        {
            return true;
        }

        // If user not authenticated, deny access
        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        // Check if user's email is in the allowlist (case-insensitive)
        return _allowedEmails.Any(e => e.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllowedSwitchingEmails()
    {
        return _allowedEmails.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task ClearPersistedEndpointAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ApiUrlStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AdminApiUrlStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", EnvironmentStorageKey);

            // Reset caches
            _cachedApiUrl = null;
            _cachedAdminApiUrl = null;
            _cachedEnvironment = null;
            _isInitialized = false;

            // Reset singleton cache and reinitialize with defaults
            _endpointCache.Clear();
            _endpointCache.Initialize(GetDefaultApiUrl(), GetDefaultAdminApiUrl());

            _logger.LogInformation("Cleared persisted API endpoint, reverting to default configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear persisted API endpoint from localStorage");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<EndpointHealthResult> ValidateEndpointAsync(string url)
    {
        if (!ValidateUrl(url, out var validationError))
        {
            return new EndpointHealthResult
            {
                Url = url,
                IsHealthy = false,
                ErrorMessage = $"Invalid URL: {validationError}"
            };
        }

        try
        {
            var healthUrl = EnsureTrailingSlash(url) + "health";

            var stopwatch = Stopwatch.StartNew();
            var response = await _healthCheckClient.GetAsync(healthUrl);
            stopwatch.Stop();

            return new EndpointHealthResult
            {
                Url = url,
                IsHealthy = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (HttpRequestException ex)
        {
            return new EndpointHealthResult
            {
                Url = url,
                IsHealthy = false,
                ErrorMessage = $"Connection failed: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new EndpointHealthResult
            {
                Url = url,
                IsHealthy = false,
                ErrorMessage = "Request timed out (5s)"
            };
        }
        catch (Exception ex)
        {
            return new EndpointHealthResult
            {
                Url = url,
                IsHealthy = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
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

            // Validate persisted URL before using it
            if (!string.IsNullOrEmpty(persistedApiUrl) && ValidateUrl(persistedApiUrl, out _))
            {
                _cachedApiUrl = persistedApiUrl;
                _logger.LogInformation("Loaded persisted API URL from localStorage: {ApiUrl}", persistedApiUrl);
            }
            else
            {
                if (!string.IsNullOrEmpty(persistedApiUrl))
                {
                    _logger.LogWarning("Persisted API URL is invalid, clearing: {ApiUrl}", persistedApiUrl);
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ApiUrlStorageKey);
                }
                _cachedApiUrl = GetDefaultApiUrl();
                _logger.LogInformation("Using default API URL from configuration: {ApiUrl}", _cachedApiUrl);
            }

            // Validate persisted admin URL
            if (!string.IsNullOrEmpty(persistedAdminApiUrl) && ValidateUrl(persistedAdminApiUrl, out _))
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

            // Initialize singleton cache
            _endpointCache.Initialize(_cachedApiUrl, _cachedAdminApiUrl);

            _isInitialized = true;
        }
        catch (InvalidOperationException)
        {
            // JSInterop not available yet (pre-render). Use defaults.
            _cachedApiUrl = GetDefaultApiUrl();
            _cachedAdminApiUrl = GetDefaultAdminApiUrl();
            _cachedEnvironment = GetDefaultEnvironment();
            _endpointCache.Initialize(_cachedApiUrl, _cachedAdminApiUrl);
            _logger.LogDebug("JSInterop not available, using default configuration values");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted configuration from localStorage, using defaults");
            _cachedApiUrl = GetDefaultApiUrl();
            _cachedAdminApiUrl = GetDefaultAdminApiUrl();
            _cachedEnvironment = GetDefaultEnvironment();
            _endpointCache.Initialize(_cachedApiUrl, _cachedAdminApiUrl);
        }
    }

    /// <summary>
    /// Validates that a URL is well-formed and uses HTTPS.
    /// </summary>
    private static bool ValidateUrl(string url, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL cannot be empty";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            error = "URL is not well-formed";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            error = "URL must use HTTP or HTTPS scheme";
            return false;
        }

        // Allow HTTP only for localhost
        if (uri.Scheme == Uri.UriSchemeHttp && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            error = "Non-localhost URLs must use HTTPS";
            return false;
        }

        return true;
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
        return ApiEndpointCache.DeriveAdminApiUrl(GetDefaultApiUrl());
    }

    private string GetDefaultEnvironment()
    {
        return _configuration.GetValue<string>("ApiConfiguration:Environment") ?? "Production";
    }

    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith('/') ? url : url + "/";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckClient.Dispose();
            _disposed = true;
        }
    }
}
