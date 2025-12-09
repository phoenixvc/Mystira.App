using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Auth;

namespace Mystira.App.Infrastructure.Azure.Services;

/// <summary>
/// Azure Communication Services email implementation.
/// Provides email sending capabilities for authentication flows (signup/signin codes).
/// </summary>
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
                _logger.LogInformation(
                    "Azure Communication Services email client initialized. SenderEmail: {SenderEmail}",
                    _senderEmail);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex,
                    "Failed to initialize Azure Communication Services email client due to argument error. " +
                    "ConnectionString present: {HasConnectionString}, SenderEmail: {SenderEmail}",
                    !string.IsNullOrEmpty(connectionString),
                    _senderEmail);
                _isEnabled = false;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex,
                    "Failed to initialize Azure Communication Services email client due to request failure. " +
                    "ConnectionString present: {HasConnectionString}, SenderEmail: {SenderEmail}, ErrorCode: {ErrorCode}",
                    !string.IsNullOrEmpty(connectionString),
                    _senderEmail,
                    ex.ErrorCode);
                _isEnabled = false;
            }
        }
        else
        {
            _logger.LogWarning(
                "Azure Communication Services is not configured. Email sending will be logged only. " +
                "ConnectionString present: {HasConnectionString}, SenderEmail present: {HasSenderEmail}",
                !string.IsNullOrEmpty(connectionString),
                !string.IsNullOrEmpty(senderEmail));
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SendSignupCodeAsync(string toEmail, string displayName, string code)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[{OperationId}] Attempting to send signup verification email. " +
            "Recipient: {Email}, DisplayName: {DisplayName}, EmailEnabled: {IsEnabled}",
            operationId, toEmail, displayName, _isEnabled);

        try
        {
            if (!_isEnabled || _emailClient == null)
            {
                _logger.LogInformation(
                    "[{OperationId}] Email sending disabled (dev mode). Code for {Email}: {Code}",
                    operationId, toEmail, code);
                return (true, null);
            }

            var emailContent = GenerateSignupEmailContent(displayName, code);

            var emailMessage = new EmailMessage(
                senderAddress: _senderEmail,
                content: new EmailContent("Your Mystira Verification Code") { Html = emailContent },
                recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(toEmail) })
            );

            _logger.LogDebug(
                "[{OperationId}] Sending email via Azure Communication Services. " +
                "From: {SenderEmail}, To: {ToEmail}, Subject: {Subject}",
                operationId, _senderEmail, toEmail, "Your Mystira Verification Code");

            var operation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

            if (operation.HasCompleted)
            {
                _logger.LogInformation(
                    "[{OperationId}] Verification email sent successfully. " +
                    "Recipient: {Email}, Status: {Status}",
                    operationId, toEmail, operation.HasValue ? operation.Value.Status.ToString() : "Unknown");
                return (true, null);
            }

            _logger.LogWarning(
                "[{OperationId}] Email operation did not complete successfully. " +
                "Recipient: {Email}, HasCompleted: {HasCompleted}, HasValue: {HasValue}",
                operationId, toEmail, operation.HasCompleted, operation.HasValue);
            return (false, "Email sending did not complete");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Azure Communication Services request failed. " +
                "Recipient: {Email}, ErrorCode: {ErrorCode}, Status: {Status}, Message: {Message}",
                operationId, toEmail, ex.ErrorCode, ex.Status, ex.Message);
            return (false, $"Failed to send email: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Invalid operation error sending verification email. " +
                "Recipient: {Email}",
                operationId, toEmail);
            return (false, "Operation was invalid while sending email");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Argument error sending verification email. " +
                "Recipient: {Email}",
                operationId, toEmail);
            return (false, "Invalid argument provided for sending email");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SendSigninCodeAsync(string toEmail, string displayName, string code)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[{OperationId}] Attempting to send sign-in email. " +
            "Recipient: {Email}, DisplayName: {DisplayName}, EmailEnabled: {IsEnabled}",
            operationId, toEmail, displayName, _isEnabled);

        try
        {
            if (!_isEnabled || _emailClient == null)
            {
                _logger.LogInformation(
                    "[{OperationId}] Email sending disabled (dev mode). Signin code for {Email}: {Code}",
                    operationId, toEmail, code);
                return (true, null);
            }

            var emailContent = GenerateSigninEmailContent(displayName, code);

            var emailMessage = new EmailMessage(
                senderAddress: _senderEmail,
                content: new EmailContent("Your Mystira Sign-In Code") { Html = emailContent },
                recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(toEmail) })
            );

            _logger.LogDebug(
                "[{OperationId}] Sending sign-in email via Azure Communication Services. " +
                "From: {SenderEmail}, To: {ToEmail}",
                operationId, _senderEmail, toEmail);

            var operation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

            if (operation.HasCompleted)
            {
                _logger.LogInformation(
                    "[{OperationId}] Sign-in email sent successfully. " +
                    "Recipient: {Email}, Status: {Status}",
                    operationId, toEmail, operation.HasValue ? operation.Value.Status.ToString() : "Unknown");
                return (true, null);
            }

            _logger.LogWarning(
                "[{OperationId}] Sign-in email operation did not complete successfully. " +
                "Recipient: {Email}, HasCompleted: {HasCompleted}, HasValue: {HasValue}",
                operationId, toEmail, operation.HasCompleted, operation.HasValue);
            return (false, "Email sending did not complete");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Azure Communication Services request failed for sign-in email. " +
                "Recipient: {Email}, ErrorCode: {ErrorCode}, Status: {Status}, Message: {Message}",
                operationId, toEmail, ex.ErrorCode, ex.Status, ex.Message);
            return (false, $"Failed to send sign-in email: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Invalid operation error sending sign-in email. " +
                "Recipient: {Email}",
                operationId, toEmail);
            return (false, "Operation was invalid while sending sign-in email");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Argument error sending sign-in email. " +
                "Recipient: {Email}",
                operationId, toEmail);
            return (false, "Invalid argument provided for sending sign-in email");
        }
    }

    private static string GenerateSignupEmailContent(string displayName, string code)
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

    private static string GenerateSigninEmailContent(string displayName, string code)
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
