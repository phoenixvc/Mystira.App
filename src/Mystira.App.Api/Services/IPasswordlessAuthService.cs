using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public interface IPasswordlessAuthService
{
    Task<(bool Success, string Message, string? Code)> RequestSignupAsync(string email, string displayName);
    Task<(bool Success, string Message, Account? Account)> VerifySignupAsync(string email, string code);
    Task<bool> CleanupExpiredSignupsAsync();
}
