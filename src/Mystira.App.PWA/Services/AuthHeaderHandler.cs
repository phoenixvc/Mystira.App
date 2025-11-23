using System.Net.Http.Headers;

namespace Mystira.App.PWA.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthHeaderHandler> _logger;

    public AuthHeaderHandler(IServiceProvider serviceProvider, ILogger<AuthHeaderHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Resolve IAuthService lazily to avoid circular dependency
            var authService = _serviceProvider.GetService<IAuthService>();

            if (authService != null)
            {
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
            else
            {
                _logger.LogWarning("AuthService not available for request: {Uri}", request.RequestUri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding auth header to request");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
