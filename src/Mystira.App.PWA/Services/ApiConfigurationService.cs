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
    private readonly IHttpClientFactory _httpClientFactory;
    private bool _disposed;

    // LocalStorage keys - using specific prefix to avoid conflicts
    private const string ApiUrlStorageKey = "mystira_api_base_url";
    private const string AdminApiUrlStorageKey = "mystira_admin_api_base_url";
    private const string EnvironmentStorageKey = "mystira_api_environment";

    // Cache for configuration values (volatile for thread safety)
    private volatile string? _cachedApiUrl;
    private volatile string? _cachedAdminApiUrl;
    private volatile string? _cachedEnvironment;
    private volatile List<ApiEndpoint>? _cachedEndpoints;
    private volatile bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public ApiConfigurationService(
        IJSRuntime jsRuntime,
        IConfiguration configuration,
        ILogger<ApiConfigurationService> logger,
        IApiEndpointCache endpointCache,
        ITelemetryService telemetry,
        IHttpClientFactory httpClientFactory)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _logger = logger;
        _endpointCache = endpointCache;
        _telemetry = telemetry;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gets the current allowlist from configuration (supports hot-reload).
    /// </summary>
    private IReadOnlyList<string> GetAllowedEmailsFromConfig()
    {
        return _configuration.GetSection("ApiConfiguration:AllowedSwitchingEmails")
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
    public Task<IReadOnlyList<ApiEndpoint>> GetAvailableEndpointsAsync()
    {
        if (_cachedEndpoints != null)
        {
            return Task.FromResult<IReadOnlyList<ApiEndpoint>>(_cachedEndpoints);
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
        return Task.FromResult<IReadOnlyList<ApiEndpoint>>(endpoints);
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

        // Get current allowlist from config (supports hot-reload)
        var allowedEmails = GetAllowedEmailsFromConfig();

        // If no allowlist configured, all users can switch
        if (allowedEmails.Count == 0)
        {
            return true;
        }

        // If user not authenticated, deny access
        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        // Check if user's email is in the allowlist (case-insensitive)
        return allowedEmails.Any(e => e.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllowedSwitchingEmails()
    {
        return GetAllowedEmailsFromConfig();
    }

    /// <inheritdoc />
    public void InvalidateEndpointsCache()
    {
        _cachedEndpoints = null;
        _logger.LogDebug("Endpoints cache invalidated");
    }

    /// <inheritdoc />
    public async Task ClearPersistedEndpointAsync()
    {
        // Acquire lock to prevent race condition with EnsureInitializedAsync
        await _initLock.WaitAsync();
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ApiUrlStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AdminApiUrlStorageKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", EnvironmentStorageKey);

            // Reset singleton cache first
            _endpointCache.Clear();

            // Get defaults before resetting state
            var defaultApiUrl = GetDefaultApiUrl();
            var defaultAdminApiUrl = GetDefaultAdminApiUrl();
            var defaultEnvironment = GetDefaultEnvironment();

            // Reset caches atomically (while holding lock)
            _cachedApiUrl = defaultApiUrl;
            _cachedAdminApiUrl = defaultAdminApiUrl;
            _cachedEnvironment = defaultEnvironment;

            // Reinitialize cache with defaults
            _endpointCache.Initialize(defaultApiUrl, defaultAdminApiUrl);

            // Keep initialized = true to avoid re-reading from localStorage
            // (we just cleared it, so we know defaults are correct)

            _logger.LogInformation("Cleared persisted API endpoint, reverting to default configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear persisted API endpoint from localStorage");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<EndpointHealthResult> ValidateEndpointAsync(string url, CancellationToken cancellationToken = default)
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

        var healthUrl = EnsureTrailingSlash(url) + "health";
        const int maxRetries = 2;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Check cancellation before making request
                cancellationToken.ThrowIfCancellationRequested();

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                var stopwatch = Stopwatch.StartNew();
                var response = await httpClient.GetAsync(healthUrl, cancellationToken);
                stopwatch.Stop();

                return new EndpointHealthResult
                {
                    Url = url,
                    IsHealthy = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // User cancelled - don't retry
                return new EndpointHealthResult
                {
                    Url = url,
                    IsHealthy = false,
                    ErrorMessage = "Health check cancelled"
                };
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                // Transient error - retry after short delay
                _logger.LogDebug("Health check attempt {Attempt} failed for {Url}: {Error}. Retrying...",
                    attempt + 1, url, ex.Message);
                await Task.Delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)), cancellationToken);
                continue;
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
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout (not user cancellation)
                if (attempt < maxRetries)
                {
                    _logger.LogDebug("Health check attempt {Attempt} timed out for {Url}. Retrying...",
                        attempt + 1, url);
                    continue;
                }
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

        // Should not reach here, but just in case
        return new EndpointHealthResult
        {
            Url = url,
            IsHealthy = false,
            ErrorMessage = "Health check failed after retries"
        };
    }

    private async Task EnsureInitializedAsync()
    {
        // Fast path - already initialized (volatile read)
        if (_isInitialized)
        {
            return;
        }

        // Slow path - acquire lock for thread-safe initialization
        await _initLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_isInitialized)
            {
                return;
            }

            await InitializeFromStorageAsync();
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task InitializeFromStorageAsync()
    {
        try
        {
            // Try to load persisted values from localStorage
            var persistedApiUrl = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ApiUrlStorageKey);
            var persistedAdminApiUrl = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AdminApiUrlStorageKey);
            var persistedEnvironment = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", EnvironmentStorageKey);

            // Validate persisted URL before using it (check for whitespace too)
            if (!string.IsNullOrWhiteSpace(persistedApiUrl) && ValidateUrl(persistedApiUrl, out _))
            {
                _cachedApiUrl = persistedApiUrl;
                _logger.LogInformation("Loaded persisted API URL from localStorage: {ApiUrl}", persistedApiUrl);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(persistedApiUrl))
                {
                    _logger.LogWarning("Persisted API URL is invalid, clearing: {ApiUrl}", persistedApiUrl);
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ApiUrlStorageKey);
                }
                _cachedApiUrl = GetDefaultApiUrl();
                _logger.LogInformation("Using default API URL from configuration: {ApiUrl}", _cachedApiUrl);
            }

            // Validate persisted admin URL
            if (!string.IsNullOrWhiteSpace(persistedAdminApiUrl) && ValidateUrl(persistedAdminApiUrl, out _))
            {
                _cachedAdminApiUrl = persistedAdminApiUrl;
            }
            else
            {
                _cachedAdminApiUrl = GetDefaultAdminApiUrl();
            }

            _cachedEnvironment = !string.IsNullOrWhiteSpace(persistedEnvironment)
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
            _isInitialized = true;
            _logger.LogDebug("JSInterop not available, using default configuration values");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted configuration from localStorage, using defaults");
            _cachedApiUrl = GetDefaultApiUrl();
            _cachedAdminApiUrl = GetDefaultAdminApiUrl();
            _cachedEnvironment = GetDefaultEnvironment();
            _endpointCache.Initialize(_cachedApiUrl, _cachedAdminApiUrl);
            _isInitialized = true;
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
            _initLock.Dispose();
            _disposed = true;
        }
    }
}
