# Email Implementation for Passwordless Sign-Up

## Overview

Azure Communication Services (ACS) email sending has been integrated into the passwordless sign-up flow. When users sign up, they now receive a verification email with their magic code instead of only logging it to console.

## Architecture

### New Components

1. **IEmailService Interface** (`src/Mystira.App.Api/Services/IEmailService.cs`)
   - Single method: `SendSignupCodeAsync(email, displayName, code)`
   - Returns: `(bool Success, string? ErrorMessage)`
   - Abstraction allows for alternative implementations

2. **AzureEmailService Implementation** (`src/Mystira.App.Api/Services/AzureEmailService.cs`)
   - Uses Azure Communication Services
   - Graceful degradation: logs emails to console if not configured
   - Professional HTML email template
   - Error handling and logging

### Modified Components

1. **PasswordlessAuthService** (`src/Mystira.App.Api/Services/PasswordlessAuthService.cs`)
   - Injected IEmailService dependency
   - Calls SendSignupCodeAsync after creating pending signup
   - Returns error if email sending fails
   - Logs verification codes for development

2. **Program.cs**
   - Registered `IEmailService` → `AzureEmailService`
   - Available via dependency injection

3. **Configuration Files**
   - `appsettings.json` - Production configuration template
   - `appsettings.Development.json` - Development configuration

4. **Project File**
   - Added NuGet package: `Azure.Communication.Email` v1.0.1

## Configuration

### Required Settings

```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "endpoint=https://YOUR_RESOURCE.communication.azure.com/;accesskey=YOUR_KEY",
    "SenderEmail": "DoNotReply@mystira.azurecomm.net"
  }
}
```

### Environment Variables (for Azure deployment)

```
AzureCommunicationServices__ConnectionString=endpoint=...
AzureCommunicationServices__SenderEmail=DoNotReply@mystira.azurecomm.net
```

### Development Setup

Leave configuration empty to test without ACS:
```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "",
    "SenderEmail": ""
  }
}
```
When empty, codes are logged to console instead of being sent.

## Email Template

Professional HTML email featuring:
- Mystira branding with dragon emoji
- Clear call-to-action
- 6-digit code display
- 15-minute expiration warning
- Responsive design
- Security footer

Generated in `AzureEmailService.GenerateEmailContent()`

## Flow Diagram

```
User submits signup form
        ↓
API validates email & display name
        ↓
Generate 6-digit code
        ↓
Save PendingSignup to database
        ↓
Call IEmailService.SendSignupCodeAsync()
        ↓
        └─ If configured: Send via ACS
        └─ If not configured: Log to console
        ↓
Return success or error to user
```

## Error Handling

### Email Sending Fails
- User sees: "Failed to send verification email. Please try again later."
- PendingSignup is NOT deleted
- Can retry by submitting signup form again
- Error is logged with details for troubleshooting

### ACS Configuration Missing
- Service logs emails to console
- Signup flow still works
- Useful for development without ACS

### Network/Service Errors
- RequestFailedException caught and logged
- Includes error code from ACS
- User receives friendly error message

## Testing

### Manual Testing

1. Configure ACS connection details
2. Run API: `dotnet run --project src/Mystira.App.Api`
3. Use PWA signup or API endpoint:
   ```bash
   curl -X POST https://localhost:5001/api/auth/passwordless/signup \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","displayName":"Test User"}'
   ```
4. Check configured email inbox for verification email
5. Or check API logs if not configured

### Development Testing (No ACS)

1. Keep ACS settings empty in config
2. Run API
3. Execute signup request
4. Check API logs for code
5. Use code in signup verification

## Security Considerations

1. **Connection Strings**: Never commit to version control
2. **Access Keys**: Rotate periodically in Azure Portal
3. **Email Addresses**: Validated with `[EmailAddress]` attribute
4. **Rate Limiting**: Consider adding to prevent abuse
5. **Sender Email**: Should be verified/owned domain
6. **HTTPS Only**: All ACS communications use TLS

## Performance Notes

- Email sending is async, doesn't block signup flow
- Timeout: Uses Azure SDK defaults (~30 seconds)
- Graceful degradation if service unavailable
- Can be moved to background queue for high volume

## Dependencies

- `Azure.Communication.Email` v1.0.1
- No additional infrastructure needed (Azure handles it)
- Works with existing .NET 9 stack

## Deployment Checklist

- [ ] Create Azure Communication Services resource
- [ ] Set up verified email domain
- [ ] Get connection string and sender email
- [ ] Add to Azure App Service configuration
- [ ] Test email sending in staging
- [ ] Monitor delivery in production
- [ ] Set up bounce handling if needed

## Logging

Email service logs at different levels:

```
INFO:  Service initialized
INFO:  Sending verification email to: user@example.com
INFO:  Verification email sent successfully to: user@example.com

WARNING: Email sending disabled. Code for user@example.com: 123456
WARNING: Failed to send verification email to user@example.com

ERROR: Azure Communication Services error sending email
ERROR: Unexpected error sending email
```

## Files Changed

### New Files (2)
- `src/Mystira.App.Api/Services/IEmailService.cs`
- `src/Mystira.App.Api/Services/AzureEmailService.cs`

### Modified Files (4)
- `src/Mystira.App.Api/Services/PasswordlessAuthService.cs`
- `src/Mystira.App.Api/Program.cs`
- `src/Mystira.App.Api/appsettings.json`
- `src/Mystira.App.Api/appsettings.Development.json`
- `src/Mystira.App.Api/Mystira.App.Api.csproj`

## Alternative Implementations

To use a different email provider, implement `IEmailService`:

```csharp
public class SendGridEmailService : IEmailService
{
    public async Task<(bool Success, string? ErrorMessage)> 
        SendSignupCodeAsync(string toEmail, string displayName, string code)
    {
        // Implementation using SendGrid
    }
}
```

Then register in `Program.cs`:
```csharp
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
```

## Next Steps

1. **Real Email Configuration**: Set up ACS with verified domain
2. **Email Tracking**: Add Event Grid webhooks for delivery tracking
3. **Template Customization**: Customize branding to match Mystira
4. **Bounce Handling**: Implement suppression lists for failed emails
5. **Analytics**: Track signup success rates and email metrics

## Support

For issues:
1. Check `ACS_EMAIL_SETUP.md` for configuration help
2. Review application logs
3. Verify ACS resource exists and is accessible
4. Confirm sender email is verified in ACS
5. Check Azure SDK documentation

## References

- Azure Communication Services: https://azure.microsoft.com/en-us/services/communication-services/
- Email SDK: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/communication.email-readme
- Pricing: https://azure.microsoft.com/en-us/pricing/details/communication-services/
