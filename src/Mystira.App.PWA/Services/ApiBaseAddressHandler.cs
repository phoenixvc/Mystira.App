namespace Mystira.App.PWA.Services;

/// <summary>
/// HTTP message handler that dynamically resolves the API base address from the ApiConfigurationService.
/// This allows the API endpoint to be changed at runtime and persisted across PWA updates.
///
/// The handler intercepts all API requests and rewrites the URL to use the persisted
/// base URL from localStorage (if available), enabling users to switch API endpoints
/// without requiring an app restart.
/// </summary>
public class ApiBaseAddressHandler : DelegatingHandler
{
    private readonly IApiConfigurationService _apiConfigurationService;
    private readonly ILogger<ApiBaseAddressHandler> _logger;

    // Cache the persisted URL to avoid async calls on every request
    private string? _cachedPersistedUrl;
    private bool _cacheInitialized;

    public ApiBaseAddressHandler(
        IApiConfigurationService apiConfigurationService,
        ILogger<ApiBaseAddressHandler> logger)
    {
        _apiConfigurationService = apiConfigurationService;
        _logger = logger;

        // Subscribe to endpoint changes to invalidate cache
        _apiConfigurationService.EndpointChanged += (_, args) =>
        {
            _cachedPersistedUrl = args.NewUrl;
            _logger.LogInformation("API endpoint changed to: {NewUrl}", args.NewUrl);
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Initialize cache on first request
        if (!_cacheInitialized)
        {
            try
            {
                _cachedPersistedUrl = await _apiConfigurationService.GetApiBaseUrlAsync();
                _cacheInitialized = true;
                _logger.LogDebug("Initialized API base URL cache: {Url}", _cachedPersistedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get persisted API URL, using default");
                _cacheInitialized = true;
            }
        }

        // If we have a persisted URL that differs from the request's current base, rewrite the URL
        if (!string.IsNullOrEmpty(_cachedPersistedUrl))
        {
            var persistedUri = new Uri(_cachedPersistedUrl);
            var requestUri = request.RequestUri;

            // Check if the request is going to a different host than the persisted one
            // This handles cases where the HttpClient was configured with a default URL
            // but the user has persisted a different endpoint
            if (requestUri.IsAbsoluteUri &&
                !requestUri.Host.Equals(persistedUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                // Rewrite the URL to use the persisted base
                var pathAndQuery = requestUri.PathAndQuery;
                var newUri = new Uri(persistedUri, pathAndQuery);
                request.RequestUri = newUri;

                _logger.LogDebug("Rewrote API request from {OldHost} to {NewHost}: {Path}",
                    requestUri.Host, persistedUri.Host, pathAndQuery);
            }
            else if (!requestUri.IsAbsoluteUri)
            {
                // For relative URIs, just combine with the persisted base
                request.RequestUri = new Uri(persistedUri, requestUri.ToString());
                _logger.LogDebug("Resolved relative API request to: {RequestUri}", request.RequestUri);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
