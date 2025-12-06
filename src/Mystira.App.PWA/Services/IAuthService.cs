using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<Account?> GetCurrentAccountAsync();
    Task<string?> GetTokenAsync();
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<(bool Success, string Message)> RequestPasswordlessSignupAsync(string email, string displayName);
    Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSignupAsync(string email, string code);
    Task<(bool Success, string Message)> RequestPasswordlessSigninAsync(string email);
    Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSigninAsync(string email, string code);
    Task<(bool Success, string Message, string? Token, string? RefreshToken)> RefreshTokenAsync(string token, string refreshToken);
    Task<string?> GetCurrentTokenAsync();
    void SetRememberMe(bool rememberMe);

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
