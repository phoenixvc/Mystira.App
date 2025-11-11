using System.Net.Http.Headers;

namespace Mystira.App.PWA.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthHeaderHandler> _logger;

    public AuthHeaderHandler(IAuthService authService, ILogger<AuthHeaderHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            
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

        return await base.SendAsync(request, cancellationToken);
    }
}
