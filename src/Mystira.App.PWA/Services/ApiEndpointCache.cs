namespace Mystira.App.PWA.Services;

/// <summary>
/// Thread-safe singleton cache for API endpoint URLs.
/// This solves the DelegatingHandler lifetime mismatch issue by providing
/// a single source of truth for endpoint URLs that persists across handler instances.
/// </summary>
public interface IApiEndpointCache
{
    /// <summary>
    /// Gets the cached API base URL, or null if not initialized.
    /// </summary>
    string? ApiBaseUrl { get; }

    /// <summary>
    /// Gets the cached Admin API base URL, or null if not initialized.
    /// </summary>
    string? AdminApiBaseUrl { get; }

    /// <summary>
    /// Whether the cache has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the cache with the given URLs. Thread-safe.
    /// </summary>
    void Initialize(string apiBaseUrl, string? adminApiBaseUrl = null);

    /// <summary>
    /// Updates the cached URLs. Thread-safe.
    /// </summary>
    void Update(string apiBaseUrl, string? adminApiBaseUrl = null);

    /// <summary>
    /// Clears the cache. Thread-safe.
    /// </summary>
    void Clear();

    /// <summary>
    /// Tries to get the appropriate base URL for a given request host.
    /// Returns true if a rewrite is needed, with the new base URL.
    /// </summary>
    bool TryGetRewriteUrl(Uri requestUri, out Uri? newBaseUri);
}

/// <summary>
/// Singleton implementation of API endpoint cache.
/// Uses lock-free reads with volatile fields for performance.
/// </summary>
public class ApiEndpointCache : IApiEndpointCache
{
    private readonly object _lock = new();
    private volatile string? _apiBaseUrl;
    private volatile string? _adminApiBaseUrl;
    private volatile bool _isInitialized;

    // Parsed URIs for efficient comparison (updated atomically with URLs)
    private volatile Uri? _apiBaseUri;
    private volatile Uri? _adminApiBaseUri;

    public string? ApiBaseUrl => _apiBaseUrl;
    public string? AdminApiBaseUrl => _adminApiBaseUrl;
    public bool IsInitialized => _isInitialized;

    public void Initialize(string apiBaseUrl, string? adminApiBaseUrl = null)
    {
        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;

            SetUrlsInternal(apiBaseUrl, adminApiBaseUrl);
            _isInitialized = true;
        }
    }

    public void Update(string apiBaseUrl, string? adminApiBaseUrl = null)
    {
        lock (_lock)
        {
            SetUrlsInternal(apiBaseUrl, adminApiBaseUrl);
            _isInitialized = true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _apiBaseUrl = null;
            _adminApiBaseUrl = null;
            _apiBaseUri = null;
            _adminApiBaseUri = null;
            _isInitialized = false;
        }
    }

    public bool TryGetRewriteUrl(Uri requestUri, out Uri? newBaseUri)
    {
        newBaseUri = null;

        if (!_isInitialized || requestUri == null || !requestUri.IsAbsoluteUri)
        {
            return false;
        }

        var requestHost = requestUri.Host;
        var requestPath = requestUri.AbsolutePath;

        // Determine if this is an admin API request based on the PATH, not host
        // Admin API paths must START with /admin/ or /api/admin/ (not just contain it anywhere)
        var isAdminRequest = requestPath.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase) ||
                             requestPath.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase);

        if (isAdminRequest)
        {
            // Route admin requests to admin API endpoint
            var adminUri = _adminApiBaseUri;
            if (adminUri != null && !requestHost.Equals(adminUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                newBaseUri = adminUri;
                return true;
            }
        }
        else
        {
            // Route regular API requests to main API endpoint
            var apiUri = _apiBaseUri;
            if (apiUri != null && !requestHost.Equals(apiUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                newBaseUri = apiUri;
                return true;
            }
        }

        return false;
    }

    private void SetUrlsInternal(string apiBaseUrl, string? adminApiBaseUrl)
    {
        _apiBaseUrl = EnsureTrailingSlash(apiBaseUrl);
        _apiBaseUri = TryParseUri(_apiBaseUrl);

        if (!string.IsNullOrEmpty(adminApiBaseUrl))
        {
            _adminApiBaseUrl = EnsureTrailingSlash(adminApiBaseUrl);
            _adminApiBaseUri = TryParseUri(_adminApiBaseUrl);
        }
        else if (_apiBaseUri != null)
        {
            // Derive admin URL from API URL
            var derivedAdminUrl = DeriveAdminApiUrl(_apiBaseUrl!);
            _adminApiBaseUrl = derivedAdminUrl;
            _adminApiBaseUri = TryParseUri(derivedAdminUrl);
        }
    }

    private static Uri? TryParseUri(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith('/') ? url : url + "/";
    }

    internal static string DeriveAdminApiUrl(string apiUrl)
    {
        try
        {
            var uri = new Uri(apiUrl);
            var host = uri.Host;

            if (host.StartsWith("api.", StringComparison.OrdinalIgnoreCase))
            {
                var newHost = "admin" + host.Substring(3);
                // Preserve port number if present
                var portSuffix = uri.IsDefaultPort ? "" : $":{uri.Port}";
                return $"{uri.Scheme}://{newHost}{portSuffix}{uri.AbsolutePath}";
            }

            return apiUrl;
        }
        catch
        {
            return apiUrl;
        }
    }
}
