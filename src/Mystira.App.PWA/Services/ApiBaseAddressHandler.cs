namespace Mystira.App.PWA.Services;

/// <summary>
/// HTTP message handler that dynamically resolves the API base address from the ApiConfigurationService.
/// This allows the API endpoint to be changed at runtime and persisted across PWA updates.
/// </summary>
public class ApiBaseAddressHandler : DelegatingHandler
{
    private readonly IApiConfigurationService _apiConfigurationService;
    private readonly ILogger<ApiBaseAddressHandler> _logger;

    public ApiBaseAddressHandler(
        IApiConfigurationService apiConfigurationService,
        ILogger<ApiBaseAddressHandler> logger)
    {
        _apiConfigurationService = apiConfigurationService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // If the request doesn't have an absolute URI, resolve it from the configuration service
        if (request.RequestUri != null && !request.RequestUri.IsAbsoluteUri)
        {
            var baseUrl = await _apiConfigurationService.GetApiBaseUrlAsync();

            if (!string.IsNullOrEmpty(baseUrl))
            {
                var baseUri = new Uri(baseUrl);
                request.RequestUri = new Uri(baseUri, request.RequestUri);
                _logger.LogDebug("Resolved API request to: {RequestUri}", request.RequestUri);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
