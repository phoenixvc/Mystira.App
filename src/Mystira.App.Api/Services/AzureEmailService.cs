using Azure;
using Azure.Communication.Email;

namespace Mystira.App.Api.Services;

public class AzureEmailService : IEmailService
{
    private readonly EmailClient? _emailClient;
    private readonly ILogger<AzureEmailService> _logger;
    private readonly string _senderEmail;
    private readonly bool _isEnabled;

    public AzureEmailService(IConfiguration configuration, ILogger<AzureEmailService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureCommunicationServices:ConnectionString"];
        var senderEmail = configuration["AzureCommunicationServices:SenderEmail"];

        _isEnabled = !string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(senderEmail);
        _senderEmail = senderEmail ?? string.Empty;

        if (_isEnabled)
        {
            try
            {
                _emailClient = new EmailClient(connectionString);
                _logger.LogInformation("Azure Communication Services email client initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Communication Services email client");
                _isEnabled = false;
            }
        }
        else
        {
            _logger.LogWarning("Azure Communication Services is not configured. Email sending will be logged only.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SendSignupCodeAsync(string toEmail, string displayName, string code)
    {
        try
        {
            if (!_isEnabled || _emailClient == null)
            {
                _logger.LogInformation("Email sending disabled. Code for {Email}: {Code}", toEmail, code);
                return (true, null);
            }

            var emailContent = GenerateSignupEmailContent(displayName, code);

            var emailMessage = new EmailMessage(
                senderAddress: _senderEmail,
                content: new EmailContent("Your Mystira Verification Code") { Html = emailContent },
                recipientAddress: toEmail
            );

            _logger.LogInformation("Sending verification email to: {Email}", toEmail);

            var operation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

            if (operation.HasCompleted)
            {
                _logger.LogInformation("Verification email sent successfully to: {Email}", toEmail);
                return (true, null);
            }

            _logger.LogWarning("Verification email operation did not complete for: {Email}", toEmail);
            return (false, "Email sending did not complete");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services error sending email to: {Email}. Error code: {Code}", toEmail, ex.ErrorCode);
            return (false, $"Failed to send email: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to: {Email}", toEmail);
            return (false, "An unexpected error occurred while sending email");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SendSigninCodeAsync(string toEmail, string displayName, string code)
    {
        try
        {
            if (!_isEnabled || _emailClient == null)
            {
                _logger.LogInformation("Email sending disabled. Signin code for {Email}: {Code}", toEmail, code);
                return (true, null);
            }

            var emailContent = GenerateSigninEmailContent(displayName, code);

            var emailMessage = new EmailMessage(
                senderAddress: _senderEmail,
                content: new EmailContent("Your Mystira Sign-In Code") { Html = emailContent },
                recipientAddress: toEmail
            );

            _logger.LogInformation("Sending sign-in email to: {Email}", toEmail);

            var operation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

            if (operation.HasCompleted)
            {
                _logger.LogInformation("Sign-in email sent successfully to: {Email}", toEmail);
                return (true, null);
            }

            _logger.LogWarning("Sign-in email operation did not complete for: {Email}", toEmail);
            return (false, "Email sending did not complete");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services error sending sign-in email to: {Email}. Error code: {Code}", toEmail, ex.ErrorCode);
            return (false, $"Failed to send sign-in email: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending sign-in email to: {Email}", toEmail);
            return (false, "An unexpected error occurred while sending sign-in email");
        }
    }

    private string GenerateSignupEmailContent(string displayName, string code)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Mystira Verification Code</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .logo {{
            font-size: 28px;
            font-weight: bold;
            color: #8B5CF6;
            margin-bottom: 10px;
        }}
        .title {{
            font-size: 22px;
            font-weight: bold;
            color: #1F2937;
            margin-bottom: 10px;
        }}
        .content {{
            margin: 20px 0;
        }}
        .greeting {{
            font-size: 16px;
            margin-bottom: 15px;
            color: #1F2937;
        }}
        .code-section {{
            text-align: center;
            margin: 25px 0;
            padding: 20px;
            background-color: #F9FAFB;
            border-radius: 8px;
        }}
        .code {{
            font-size: 32px;
            font-weight: bold;
            letter-spacing: 4px;
            color: #8B5CF6;
            font-family: 'Courier New', monospace;
            margin: 10px 0;
        }}
        .footer {{
            margin-top: 25px;
            text-align: center;
            font-size: 12px;
            color: #9CA3AF;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">Mystira</div>
        <div class=""title"">Verify Your Email</div>
    </div>

    <div class=""content"">
        <div class=""greeting"">
            Hi {displayName},
        </div>

        <p>Welcome to Mystira! Enter this code to complete your sign-up:</p>

        <div class=""code-section"">
            <div class=""code"">{code}</div>
        </div>

        <p style=""font-size: 14px; color: #6B7280;"">This code expires in 15 minutes.</p>
    </div>

    <div class=""footer"">
        <p>&copy; 2025 Mystira. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private string GenerateSigninEmailContent(string displayName, string code)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Mystira Sign-In Code</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .logo {{
            font-size: 28px;
            font-weight: bold;
            color: #8B5CF6;
            margin-bottom: 10px;
        }}
        .title {{
            font-size: 22px;
            font-weight: bold;
            color: #1F2937;
            margin-bottom: 10px;
        }}
        .content {{
            margin: 20px 0;
        }}
        .greeting {{
            font-size: 16px;
            margin-bottom: 15px;
            color: #1F2937;
        }}
        .code-section {{
            text-align: center;
            margin: 25px 0;
            padding: 20px;
            background-color: #F9FAFB;
            border-radius: 8px;
        }}
        .code {{
            font-size: 32px;
            font-weight: bold;
            letter-spacing: 4px;
            color: #8B5CF6;
            font-family: 'Courier New', monospace;
            margin: 10px 0;
        }}
        .footer {{
            margin-top: 25px;
            text-align: center;
            font-size: 12px;
            color: #9CA3AF;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">Mystira</div>
        <div class=""title"">Sign In</div>
    </div>

    <div class=""content"">
        <div class=""greeting"">
            Hi {displayName},
        </div>

        <p>Use this code to continue your adventure:</p>

        <div class=""code-section"">
            <div class=""code"">{code}</div>
        </div>

        <p style=""font-size: 14px; color: #6B7280;"">The code expires in 15 minutes.</p>
    </div>

    <div class=""footer"">
        <p>&copy; 2025 Mystira. All rights reserved.</p>
    </div>
</body>
</html>";
    }
}
