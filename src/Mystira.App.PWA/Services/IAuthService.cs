using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<Account?> GetCurrentAccountAsync();
    Task<string?> GetTokenAsync();
    Task LogoutAsync();
    Task<string?> GetCurrentTokenAsync();
    void SetRememberMe(bool rememberMe);
    Task<bool> GetRememberMeAsync();

    /// <summary>
    /// Gets the token expiry time in UTC
    /// </summary>
    DateTime? GetTokenExpiryTime();

    /// <summary>
    /// Checks if token will expire within the specified minutes and refreshes if needed
    /// </summary>
    Task<bool> EnsureTokenValidAsync(int expiryBufferMinutes = 5);

    /// <summary>
    /// Event raised when token is about to expire (within 5 minutes)
    /// </summary>
    event EventHandler? TokenExpiryWarning;

    event EventHandler<bool>? AuthenticationStateChanged;
}
