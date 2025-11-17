# Email Integration Setup

## Overview

This guide covers the complete setup and configuration of passwordless email authentication for Mystira using Azure Communication Services (ACS).

## Quick Start

### Development Mode (No Azure Required)

The system works out-of-the-box without any configuration. Verification codes are logged to the console.

```bash
# 1. Run API
dotnet run --project src/Mystira.App.Api

# 2. Request signup code
curl -X POST https://localhost:5001/api/auth/passwordless/signup \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","displayName":"Test User"}'

# 3. Check API logs for verification code
# Look for: "Code for test@example.com: 123456"

# 4. Verify code
curl -X POST https://localhost:5001/api/auth/passwordless/verify \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","code":"123456"}'
```

### Production Mode (With Azure)

Follow the [Azure Communication Services Setup](#azure-communication-services-setup) section below.

## Features

### Email Service
- ✅ Azure Communication Services integration
- ✅ Professional HTML email template with Mystira branding
- ✅ Graceful degradation (console logging when not configured)
- ✅ Comprehensive error handling and recovery
- ✅ Support for development and production configurations

### Security
- ✅ 6-digit codes (1 million combinations)
- ✅ 15-minute expiration
- ✅ One-time use enforcement
- ✅ Email address validation
- ✅ Account uniqueness checking
- ✅ TLS/HTTPS for all communications

### User Experience
- ✅ Minimal setup required
- ✅ Professional email template
- ✅ Clear expiration warnings
- ✅ Mobile-responsive design

## Architecture

### Components

1. **IEmailService Interface** (`src/Mystira.App.Api/Services/IEmailService.cs`)
   - Abstraction for email sending
   - Allows alternative implementations

2. **AzureEmailService** (`src/Mystira.App.Api/Services/AzureEmailService.cs`)
   - Azure Communication Services implementation
   - HTML email template generation
   - Graceful degradation when not configured

3. **PasswordlessAuthService** (`src/Mystira.App.Api/Services/PasswordlessAuthService.cs`)
   - Integrates email service
   - Generates verification codes
   - Manages pending signups

### Email Flow

```
User Signs Up
    ↓
API validates email & display name
    ↓
Generate 6-digit code
    ↓
Save to PendingSignup table
    ↓
Send email via ACS (or log if not configured)
    ↓
User receives email
    ↓
User enters code
    ↓
API verifies code
    ↓
Account created
```

## Configuration

### Development (Default)

No changes needed. Leave configuration empty in `appsettings.Development.json`:

```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "",
    "SenderEmail": ""
  }
}
```

**Behavior**: Codes are logged to console

### Production

Update `appsettings.json` or use environment variables:

```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "endpoint=https://YOUR_ACS.communication.azure.com/;accesskey=YOUR_KEY",
    "SenderEmail": "DoNotReply@YOUR_DOMAIN.azurecomm.net"
  }
}
```

**Environment Variables** (recommended for Azure App Service):
- `AzureCommunicationServices__ConnectionString`
- `AzureCommunicationServices__SenderEmail`

## Azure Communication Services Setup

### Prerequisites
- Azure subscription
- 10-15 minutes for setup

### Step 1: Create ACS Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource"
3. Search for "Communication Services"
4. Click "Create"
5. Fill in details:
   - **Resource Group**: Create new or select existing
   - **Name**: `mystira-acs`
   - **Data Location**: Select your region
6. Click "Create"

### Step 2: Set Up Email Domain

#### Option A: Azure-Managed Domain (Recommended for Development)

1. Go to your ACS resource
2. Navigate to "Email" → "Domains"
3. Click "Connect domain" → "Add Azure managed domain"
4. Azure generates a domain like `DoNotReply@mystira.azurecomm.net`
5. This email is immediately verified and ready to use

#### Option B: Custom Domain (Recommended for Production)

1. Go to your ACS resource
2. Click "Email" → "Domains"
3. Click "Connect domain" → "Add custom domain"
4. Enter your domain (e.g., `noreply@mystira.app`)
5. Follow DNS verification steps
6. Wait for verification to complete

### Step 3: Get Connection String

1. In your ACS resource, go to "Keys"
2. Copy the "Connection string"
3. Store it securely (treat it like a password)

### Step 4: Configure Application

Update your configuration file or set environment variables as shown in the [Configuration](#configuration) section above.

### Step 5: Test Email Sending

```bash
# Run API
dotnet run --project src/Mystira.App.Api

# Request code
curl -X POST https://localhost:5001/api/auth/passwordless/signup \
  -H "Content-Type: application/json" \
  -d '{"email":"your-email@example.com","displayName":"Your Name"}'

# Check your email inbox for the verification code
```

## Email Template

The HTML email template includes:
- Mystira branding with dragon emoji 🐉
- User's display name
- Large, easy-to-read 6-digit code
- 15-minute expiration warning
- Security footer
- Mobile-responsive design

To customize the template, edit the `GenerateEmailContent` method in `AzureEmailService.cs`.

## Error Handling

| Scenario | User Message | Action |
|----------|-------------|--------|
| Invalid email | "Invalid email address" | Fix email format |
| Account exists | "An account with this email already exists" | Use different email |
| Code expired | "Your verification code has expired" | Request new code |
| Invalid code | "Invalid or expired verification code" | Check email for correct code |
| Email send fails | "Failed to send verification email" | Check ACS config & logs |
| ACS not configured | Codes logged to console | Development mode |

## Troubleshooting

### Emails Not Being Sent

**Check:**
1. ACS configuration is complete in appsettings
2. Connection string format is correct
3. Sender email is verified in Azure
4. API logs for detailed error messages

**Solution:**
- Verify connection string includes both `endpoint` and `accesskey`
- Ensure no extra spaces or line breaks
- For managed domain: Use `@azurecomm.net` address
- For custom domain: Wait 15 minutes after DNS verification

### Can't Find Code in Logs

1. Search for "Code for" in API console output
2. Search for your email address
3. Ensure you're viewing API logs (not PWA logs)

### Email Goes to Spam

1. Use custom verified domain for production
2. Set up SPF and DKIM records
3. Monitor delivery statistics in Azure

## Performance

- Email sending: Async, ~30 second timeout
- Code generation: Immediate
- Database operations: Milliseconds
- Total signup flow: 2-3 seconds
- Scalable to millions of users with ACS

## Security Best Practices

1. **Never commit connection strings** to version control
2. **Use Azure Key Vault** for production secrets
3. **Enable monitoring** for failed email attempts
4. **Rotate access keys** periodically
5. **Implement rate limiting** to prevent abuse
6. **Monitor bounce rates** and suppress invalid addresses

## Pricing

Azure Communication Services email pricing:
- **Free tier**: 100 emails/month (up to 12 months)
- **Pay-as-you-go**: After free tier
- Check [Azure pricing](https://azure.microsoft.com/pricing/details/communication-services/) for current rates

## Advanced Configuration

### Rate Limiting

Consider implementing rate limiting on signup endpoints to prevent abuse:
- Limit signup requests per IP
- Limit signup requests per email address
- Implement exponential backoff

### Email Tracking

Azure Communication Services supports Event Grid integration for tracking:
- Email delivery status
- Bounce events
- Open/click tracking

See Azure documentation for Event Grid setup.

### Alternative Email Providers

To use a different email provider (e.g., SendGrid), implement `IEmailService`:

```csharp
public class SendGridEmailService : IEmailService
{
    public async Task<(bool Success, string? ErrorMessage)> 
        SendSignupCodeAsync(string toEmail, string displayName, string code)
    {
        // Implementation using SendGrid API
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
```

## Related Documentation

- **[Passwordless Signup Implementation](../features/PASSWORDLESS_SIGNUP.md)** - Technical implementation details
- **[Admin API Separation](../features/ADMIN_API_SEPARATION.md)** - Admin API architecture
- **[Main README](../../README.md)** - Project overview and getting started

## Support

For issues:
1. Check application logs for detailed error messages
2. Verify Azure Communication Services configuration
3. Consult Azure Communication Services documentation
4. Open an issue on the project repository

## References

- [Azure Communication Services Documentation](https://learn.microsoft.com/azure/communication-services/)
- [Email Service Overview](https://learn.microsoft.com/azure/communication-services/concepts/email/email-overview)
- [.NET Email SDK](https://learn.microsoft.com/dotnet/api/overview/azure/communication.email-readme)
