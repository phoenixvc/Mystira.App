using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services;

public interface IAuthApiClient
{
    Task<PasswordlessSignupResponse?> RequestPasswordlessSignupAsync(string email, string displayName);
    Task<PasswordlessVerifyResponse?> VerifyPasswordlessSignupAsync(string email, string code);
    Task<PasswordlessSigninResponse?> RequestPasswordlessSigninAsync(string email);
    Task<PasswordlessVerifyResponse?> VerifyPasswordlessSigninAsync(string email, string code);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string token, string refreshToken);
    Task<Account?> GetAccountByEmailAsync(string email);
}

