using Mystira.App.Application.Ports.Auth;
using Mystira.App.Api.Services;

namespace Mystira.App.Api.Adapters;

/// <summary>
/// Adapter that adapts API.Services.IEmailService to Application.Ports.Auth.IEmailService
/// </summary>
public class EmailServiceAdapter : Application.Ports.Auth.IEmailService
{
    private readonly Services.IEmailService _apiService;

    public EmailServiceAdapter(Services.IEmailService apiService)
    {
        _apiService = apiService;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendSignupCodeAsync(string toEmail, string displayName, string code)
    {
        return await _apiService.SendSignupCodeAsync(toEmail, displayName, code);
    }

    public async Task<(bool Success, string? ErrorMessage)> SendSigninCodeAsync(string toEmail, string displayName, string code)
    {
        return await _apiService.SendSigninCodeAsync(toEmail, displayName, code);
    }
}

