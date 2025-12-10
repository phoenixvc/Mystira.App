namespace Mystira.App.PWA.Services;

/// <summary>
/// HTTP message handler that dynamically resolves the API base address from the singleton cache.
/// This allows the API endpoint to be changed at runtime and persisted across PWA updates.
///
/// The handler intercepts all API requests and rewrites the URL to use the cached
/// base URL (if available), enabling users to switch API endpoints without requiring an app restart.
///
/// Design notes:
/// - Uses IApiEndpointCache (singleton) for thread-safe URL caching
/// - Does NOT subscribe to events (avoids memory leaks with handler pooling)
/// - Handles both regular API and Admin API requests
/// - Stateless - all state is in the singleton cache
/// </summary>
public class ApiBaseAddressHandler : DelegatingHandler
{
    private readonly IApiEndpointCache _endpointCache;
    private readonly ILogger<ApiBaseAddressHandler> _logger;

    public ApiBaseAddressHandler(
        IApiEndpointCache endpointCache,
        ILogger<ApiBaseAddressHandler> logger)
    {
        _endpointCache = endpointCache;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Check if URL rewrite is needed
        if (_endpointCache.TryGetRewriteUrl(request.RequestUri, out var newBaseUri) && newBaseUri != null)
        {
            var originalUri = request.RequestUri;
            var pathAndQuery = originalUri.PathAndQuery;
            var newUri = new Uri(newBaseUri, pathAndQuery);
            request.RequestUri = newUri;

            _logger.LogDebug("Rewrote API request from {OldHost} to {NewHost}: {Path}",
                originalUri.Host, newBaseUri.Host, pathAndQuery);
        }
        else if (!request.RequestUri.IsAbsoluteUri && !string.IsNullOrEmpty(_endpointCache.ApiBaseUrl))
        {
            // Handle relative URIs by combining with cached base
            var baseUri = new Uri(_endpointCache.ApiBaseUrl);
            request.RequestUri = new Uri(baseUri, request.RequestUri.ToString());
            _logger.LogDebug("Resolved relative API request to: {RequestUri}", request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
