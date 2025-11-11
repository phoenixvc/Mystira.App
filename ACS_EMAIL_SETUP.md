# Azure Communication Services Email Setup Guide

## Overview

This document explains how to set up and configure Azure Communication Services (ACS) for sending verification emails in the Mystira passwordless sign-up flow.

## Prerequisites

- Azure Subscription
- Azure Communication Services resource
- Verified sender email domain or custom domain

## Step 1: Create Azure Communication Services Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource"
3. Search for "Communication Services"
4. Click "Create"
5. Fill in the details:
   - Resource Group: Create new or select existing
   - Name: `mystira-acs` (or your preferred name)
   - Data Location: Select your preferred region
6. Click "Create"

## Step 2: Set Up Email Domain

### Option A: Using Azure-managed Domain (Easiest for Development)

1. Go to your ACS resource
2. In the left menu, find "Email" section
3. Click "Domains"
4. Click "Connect domain" → "Add Azure managed domain"
5. This generates a domain like `DoNotReply@mystira.azurecomm.net`
6. This email is immediately verified and ready to use

### Option B: Using Custom Domain (Production Recommended)

1. Go to your ACS resource
2. Click "Email" → "Domains"
3. Click "Connect domain" → "Add custom domain"
4. Enter your domain (e.g., `noreply@mystira.app`)
5. Follow DNS verification steps
6. Once verified, you can use this as sender

## Step 3: Get Connection String

1. In your ACS resource, go to "Keys" in the left menu
2. Copy the "Connection string"
3. Keep this secure - it's like a password

## Step 4: Configure Application

### Development Environment

1. Open `appsettings.Development.json`
2. Add your ACS configuration:

```json
"AzureCommunicationServices": {
  "ConnectionString": "endpoint=https://your-acs-resource.communication.azure.com/;accesskey=YOUR_ACCESS_KEY",
  "SenderEmail": "DoNotReply@mystira.azurecomm.net"
}
```

### Production Environment

1. Open `appsettings.json`
2. Add your ACS configuration:

```json
"AzureCommunicationServices": {
  "ConnectionString": "endpoint=https://your-acs-resource.communication.azure.com/;accesskey=YOUR_ACCESS_KEY",
  "SenderEmail": "noreply@mystira.app"
}
```

### Azure App Service Configuration

For deployed applications, set these environment variables instead:
- `AzureCommunicationServices__ConnectionString`
- `AzureCommunicationServices__SenderEmail`

## Step 5: Test Email Sending

### Using Swagger UI

1. Run the API: `dotnet run --project src/Mystira.App.Api`
2. Open `https://localhost:5001` (or your API URL)
3. Look for the Swagger UI
4. Find the `/api/auth/passwordless/signup` endpoint
5. Execute with test data:
```json
{
  "email": "test@example.com",
  "displayName": "Test User"
}
```

### Using curl

```bash
curl -X POST https://localhost:5001/api/auth/passwordless/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "displayName": "Test User"
  }'
```

## Step 6: Handling Email Bounces and Complaints

For production deployments, consider setting up:

1. **Event Grid Integration**: Monitor email delivery events
2. **Suppression Lists**: Automatically suppress bounced addresses
3. **Feedback Loops**: Track complaints and unsubscribes

See Azure documentation for advanced email management.

## Troubleshooting

### "Sender email is not verified" Error

**Solution**: 
- For managed domain: Ensure you're using the auto-generated `@azurecomm.net` domain
- For custom domain: Verify DNS records are properly configured
- Wait 15 minutes after domain verification

### "Connection string is invalid" Error

**Solution**:
- Copy the entire connection string from Azure Portal
- Ensure no extra spaces or line breaks
- Check that `endpoint` and `accesskey` are both present

### Emails Not Being Received

**Common causes**:
1. Check spam/junk folder
2. Verify recipient email is correct
3. Check ACS quota - may have hit send limit
4. Review Azure logs for detailed error messages

### Development: Emails Not Being Sent (Configuration Empty)

**Expected behavior**: When `ConnectionString` or `SenderEmail` are empty in configuration:
- Service logs the code to console instead of sending email
- Signup flow still works
- This is intentional for development without ACS

To enable real emails during development:
1. Add ACS configuration to `appsettings.Development.json`
2. Restart the application

## Email Template Customization

The email template is generated in `AzureEmailService.cs` in the `GenerateEmailContent` method.

To customize:
1. Edit `GenerateEmailContent` method
2. Modify HTML content
3. Update styling as needed
4. Restart application

## API Integration Flow

```
User Signs Up
    ↓
API validates email & display name
    ↓
Generate 6-digit code
    ↓
Save to PendingSignup table
    ↓
Call AzureEmailService.SendSignupCodeAsync()
    ↓
ACS API sends email
    ↓
User receives email with code
    ↓
User enters code in PWA
    ↓
API verifies code and creates account
```

## Best Practices

1. **Rate Limiting**: Consider implementing rate limits on signup requests
2. **Bounce Handling**: Monitor and handle email bounces
3. **Custom Branding**: Customize email templates with your branding
4. **Logging**: Monitor ACS API usage and failures
5. **Fallback**: Have a fallback email service or manual verification flow
6. **Testing**: Use sandbox/test emails during development

## Pricing

Azure Communication Services email pricing:
- **Free tier**: 100 emails/month (up to 12 months)
- **Pay-as-you-go**: After free tier or for higher volumes
- Check [Azure pricing](https://azure.microsoft.com/en-us/pricing/details/communication-services/) for current rates

## Security Considerations

1. **Never commit connection strings** to version control
2. **Use Azure Key Vault** for production secrets
3. **Enable TLS** for all communications
4. **Monitor access logs** for unauthorized usage
5. **Rotate access keys** periodically

## References

- [Azure Communication Services Documentation](https://learn.microsoft.com/en-us/azure/communication-services/)
- [Email Service Overview](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview)
- [Send Email with ACS](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/send-email)
- [.NET Email SDK](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/communication.email-readme)

## FAQ

**Q: Can I send emails from any domain?**
A: No, only verified domains. Use Azure-managed domain for quick setup or verify your own custom domain.

**Q: How many emails can I send?**
A: Depends on your plan. Free tier offers 100/month. Contact support for higher limits.

**Q: What happens if ACS is down?**
A: Users won't receive codes. Consider having a backup delivery method or manually sending codes.

**Q: Can I customize the email design?**
A: Yes, edit the `GenerateEmailContent` method in `AzureEmailService.cs`.

**Q: How long do verification codes last?**
A: Currently 15 minutes. Configurable in `PasswordlessAuthService.cs` (`CodeExpiryMinutes` constant).

**Q: Is email tracking supported?**
A: Not in the current implementation. ACS supports event webhooks for detailed tracking - refer to Azure docs.

## Support

For issues or questions:
1. Check Azure Communication Services documentation
2. Review application logs
3. Open an issue on the project repository
4. Contact Azure support if ACS-specific
