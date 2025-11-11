using Azure;
using Azure.Communication.Email;

namespace Mystira.App.Admin.Api.Services;

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
            background-color: #f5f5f5;
        }}
        .container {{
            background-color: white;
            border-radius: 8px;
            padding: 40px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .logo {{
            font-size: 28px;
            font-weight: bold;
            color: #8B5CF6;
            margin-bottom: 10px;
        }}
        .title {{
            font-size: 24px;
            font-weight: bold;
            color: #1F2937;
            margin-bottom: 10px;
        }}
        .subtitle {{
            font-size: 14px;
            color: #6B7280;
        }}
        .content {{
            margin: 30px 0;
        }}
        .greeting {{
            font-size: 16px;
            margin-bottom: 20px;
            color: #1F2937;
        }}
        .code-section {{
            text-align: center;
            margin: 30px 0;
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
            margin: 15px 0;
        }}
        .code-description {{
            font-size: 14px;
            color: #6B7280;
            margin-top: 10px;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #E5E7EB;
            text-align: center;
            font-size: 12px;
            color: #9CA3AF;
        }}
        .warning {{
            background-color: #FEF3C7;
            border-left: 4px solid #FBBF24;
            padding: 12px;
            border-radius: 4px;
            margin: 20px 0;
            font-size: 13px;
            color: #92400E;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""logo"">🐉 Mystira</div>
            <div class=""title"">Verify Your Email</div>
            <div class=""subtitle"">Complete your sign-up to start your adventure</div>
        </div>

        <div class=""content"">
            <div class=""greeting"">
                Hi {displayName},
            </div>

            <p>Welcome to Mystira! We're excited to have you join us. To complete your sign-up and start creating magical adventures for young heroes, please verify your email with the code below:</p>

            <div class=""code-section"">
                <div class=""code"">{code}</div>
                <div class=""code-description"">Your verification code (valid for 15 minutes)</div>
            </div>

            <div class=""warning"">
                <strong>⏰ This code will expire in 15 minutes</strong><br>
                If you didn't request this code, you can safely ignore this email.
            </div>

            <p>If you have any questions or need help, please don't hesitate to reach out to our support team.</p>
        </div>

        <div class=""footer"">
            <p>&copy; 2024 Mystira. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
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
            background-color: #f5f5f5;
        }}
        .container {{
            background-color: white;
            border-radius: 8px;
            padding: 40px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .logo {{
            font-size: 28px;
            font-weight: bold;
            color: #8B5CF6;
            margin-bottom: 10px;
        }}
        .title {{
            font-size: 24px;
            font-weight: bold;
            color: #1F2937;
            margin-bottom: 10px;
        }}
        .subtitle {{
            font-size: 14px;
            color: #6B7280;
        }}
        .content {{
            margin: 30px 0;
        }}
        .greeting {{
            font-size: 16px;
            margin-bottom: 20px;
            color: #1F2937;
        }}
        .code-section {{
            text-align: center;
            margin: 30px 0;
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
            margin: 15px 0;
        }}
        .code-description {{
            font-size: 14px;
            color: #6B7280;
            margin-top: 10px;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #E5E7EB;
            text-align: center;
            font-size: 12px;
            color: #9CA3AF;
        }}
        .warning {{
            background-color: #FEF3C7;
            border-left: 4px solid #FBBF24;
            padding: 12px;
            border-radius: 4px;
            margin: 20px 0;
            font-size: 13px;
            color: #92400E;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""logo"">🐉 Mystira</div>
            <div class=""title"">Sign In to Your Account</div>
            <div class=""subtitle"">Welcome back to your adventure</div>
        </div>

        <div class=""content"">
            <div class=""greeting"">
                Hi {displayName},
            </div>

            <p>Welcome back to Mystira! To sign in to your account and continue your adventures, please use the verification code below:</p>

            <div class=""code-section"">
                <div class=""code"">{code}</div>
                <div class=""code-description"">Your sign-in code (valid for 15 minutes)</div>
            </div>

            <div class=""warning"">
                <strong>⏰ This code will expire in 15 minutes</strong><br>
                If you didn't request this code, you can safely ignore this email.
            </div>

            <p>If you have any questions or need help, please don't hesitate to reach out to our support team.</p>
        </div>

        <div class=""footer"">
            <p>&copy; 2024 Mystira. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }
}
