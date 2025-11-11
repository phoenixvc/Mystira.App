namespace Mystira.App.Api.Services;

public interface IEmailService
{
    Task<(bool Success, string? ErrorMessage)> SendSignupCodeAsync(string toEmail, string displayName, string code);
}
