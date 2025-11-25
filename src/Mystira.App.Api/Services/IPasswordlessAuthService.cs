using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Legacy authentication service - DEPRECATED in favor of CQRS commands.
/// Use RequestPasswordlessSignupCommand, VerifyPasswordlessSignupCommand, etc. instead.
/// </summary>
[Obsolete("Use CQRS commands in Mystira.App.Application.CQRS.Auth.Commands instead. This service will be removed in a future version.")]
public interface IPasswordlessAuthService
{
    Task<(bool Success, string Message, string? Code)> RequestSignupAsync(string email, string displayName);
    Task<(bool Success, string Message, Account? Account)> VerifySignupAsync(string email, string code);
    Task<(bool Success, string Message, string? Code)> RequestSigninAsync(string email);
    Task<(bool Success, string Message, Account? Account)> VerifySigninAsync(string email, string code);
    Task<bool> CleanupExpiredSignupsAsync();
    Task<Account?> GetAccountByUserIdAsync(string userId);
}
