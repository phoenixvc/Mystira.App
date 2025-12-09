using System.Net.Http.Headers;

namespace Mystira.App.PWA.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthHeaderHandler> _logger;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static bool _isRefreshing = false;

    public AuthHeaderHandler(IServiceProvider serviceProvider, ILogger<AuthHeaderHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authService = _serviceProvider.GetService<IAuthService>();

        // Skip adding Authorization for specific public/auth endpoints to avoid circular failures
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAuthEndpoint = path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
        var isPublicDiscordStatus = path.Equals("/api/discord/status", StringComparison.OrdinalIgnoreCase);
        var shouldSkipAuthHeader = isAuthEndpoint || isPublicDiscordStatus;

        if (authService != null && !shouldSkipAuthHeader)
        {
            try
            {
                // Proactively ensure token is valid before making request
                await authService.EnsureTokenValidAsync(5);

                var token = await authService.GetTokenAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("Added Bearer token to request: {Uri}", request.RequestUri);
                }
                else
                {
                    _logger.LogDebug("No token available for request: {Uri}", request.RequestUri);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding auth header to request");
            }
        }
        else if (shouldSkipAuthHeader)
        {
            // Ensure no Authorization header is sent to auth routes
            request.Headers.Authorization = null;
            _logger.LogDebug("Skipping Authorization header for route: {Uri}", request.RequestUri);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized - try to refresh token and retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && authService != null && !isAuthEndpoint)
        {
            _logger.LogInformation("Received 401 Unauthorized, attempting token refresh for: {Uri}", request.RequestUri);

            // Prevent multiple concurrent refresh attempts
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                if (!_isRefreshing)
                {
                    _isRefreshing = true;
                    try
                    {
                        // Force token refresh
                        var refreshSuccess = await authService.EnsureTokenValidAsync(expiryBufferMinutes: 999); // Force refresh

                        if (refreshSuccess)
                        {
                            // Clone the request and retry with new token
                            var newRequest = await CloneRequestAsync(request);
                            var newToken = await authService.GetTokenAsync();

                            if (!string.IsNullOrEmpty(newToken))
                            {
                                newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                                _logger.LogInformation("Retrying request with refreshed token: {Uri}", request.RequestUri);

                                // Dispose old response before returning new one
                                response.Dispose();
                                return await base.SendAsync(newRequest, cancellationToken);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Token refresh failed after 401, user may need to re-authenticate");
                        }
                    }
                    finally
                    {
                        _isRefreshing = false;
                    }
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copy headers
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
