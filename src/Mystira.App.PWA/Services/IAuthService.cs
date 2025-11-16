using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<Account?> GetCurrentAccountAsync();
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<(bool Success, string Message)> RequestPasswordlessSignupAsync(string email, string displayName);
    Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSignupAsync(string email, string code);
    Task<(bool Success, string Message)> RequestPasswordlessSigninAsync(string email);
    Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSigninAsync(string email, string code);
    Task<(bool Success, string Message, string? Token, string? RefreshToken)> RefreshTokenAsync(string token, string refreshToken);
    Task<string?> GetCurrentTokenAsync();
    event EventHandler<bool>? AuthenticationStateChanged;
}
