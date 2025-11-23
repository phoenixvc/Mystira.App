namespace Mystira.App.Admin.Api.Services;

public interface IEmailService
{
    Task<(bool Success, string? ErrorMessage)> SendSignupCodeAsync(string toEmail, string displayName, string code);
    Task<(bool Success, string? ErrorMessage)> SendSigninCodeAsync(string toEmail, string displayName, string code);
}
