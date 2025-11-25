namespace Mystira.App.Application.Ports.Auth;

/// <summary>
/// Port interface for email service operations.
/// Implementations handle sending authentication-related emails.
/// </summary>
public interface IEmailService
{
    Task<(bool Success, string? ErrorMessage)> SendSignupCodeAsync(string toEmail, string displayName, string code);
    Task<(bool Success, string? ErrorMessage)> SendSigninCodeAsync(string toEmail, string displayName, string code);
}
