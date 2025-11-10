# Mystira App - Auth0 Authentication Setup

This document provides step-by-step instructions for configuring Auth0 authentication for the Mystira PWA application with passwordless-first login, social providers, and rotating refresh tokens.

## Overview

The Mystira PWA uses Auth0's Universal Login with the following authentication features:
- **Passwordless-first**: Email Magic Link authentication as the primary method
- **Social login**: Google, Apple, Microsoft, Facebook support
- **Rotating refresh tokens**: "Stay signed in" functionality with offline_access
- **Identifier-first flow**: Clean UX starting with email input
- **Passkeys support**: Optional WebAuthn setup post-login
- **Account linking**: Automatic linking for accounts with verified emails

## Auth0 Tenant Setup

### 1. Create Auth0 Application

1. Sign in to your [Auth0 Dashboard](https://manage.auth0.com/)
2. Navigate to **Applications** → **Applications**
3. Click **Create Application**
4. Choose **Single Page Web Applications (SPA)**
5. Name your application (e.g., "Mystira PWA")
6. Click **Create**

### 2. Configure Application URLs

Go to **Settings** → **Application URIs** and configure:

#### Allowed Callback URLs
```
https://your-swa-host.azurestaticapps.net/authentication/login-callback
http://localhost:5000/authentication/login-callback  # Development
```

#### Allowed Logout URLs
```
https://your-swa-host.azurestaticapps.net/
http://localhost:5000/  # Development
```

#### Allowed Web Origins
```
https://your-swa-host.azurestaticapps.net
http://localhost:5000  # Development
```

### 3. Enable Advanced Settings

#### Enable Refresh Tokens
1. Go to **Settings** → **Advanced Settings** → **OAuth**
2. Turn **ON** "Allow Offline Access"
3. Turn **ON** "Refresh Token Rotation"
4. Set "Token Expiration" for refresh tokens (recommended: 30 days)
5. Set "Absolute Refresh Token Lifetime" (recommended: 1 year)

#### Configure Token Settings
- **JWT Signature Algorithm**: RS256
- **ID Token Lifetime**: 36000 seconds (10 hours)
- **Access Token Lifetime**: 86400 seconds (24 hours)

### 4. Configure Universal Login

#### Enable Identifier-First Experience
1. Go to **Universal Login** → **Login**
2. Select **New Experience** (Beta)
3. Enable **Identifier First** flow
4. Customize the login page template if needed

#### Configure Login Page
- Set a custom logo and branding
- Configure the page to show email input first
- Enable "More options" for social login buttons

### 5. Enable Passwordless Email

1. Navigate to **Authentication** → **Passwordless**
2. Click on **Email**
3. Enable the Email passwordless method
4. Configure email settings:
   - **Connection Name**: `email` (keep default)
   - **Email Syntax**: `{{user.email}}`
   - **Template**: Use the default magic link template
   - **Result URL**: Your application's callback URL
   - **Auth0 Forwarded Request Header**: `auth0-forwarded-for`

#### Email Template Configuration
Customize the magic link email template:
- **Subject**: "Sign in to Mystira"
- **Body**: Include your branding and clear instructions
- **Link Lifetime**: 5 minutes (recommended for security)

### 6. Configure Social Connections

#### Google
1. Go to **Authentication** → **Social**
2. Click on **Google**
3. Enable the connection
4. Configure your Google OAuth app credentials
5. Set **Connection Name**: `google-oauth2`

#### Apple
1. Go to **Authentication** → **Social**
2. Click on **Apple**
3. Enable the connection
4. Configure your Apple Developer app credentials
5. Set **Connection Name**: `apple`

#### Microsoft
1. Go to **Authentication** → **Social**
2. Click on **Microsoft Live**
3. Enable the connection
4. Configure your Microsoft Azure app credentials
5. Set **Connection Name**: `windowslive`

#### Facebook (Optional)
1. Go to **Authentication** → **Social**
2. Click on **Facebook**
3. Enable the connection
4. Configure your Facebook app credentials
5. Set **Connection Name**: `facebook`

### 7. Enable Passkeys/WebAuthn (Optional)

1. Go to **Authentication** → **Database** → **Username-Password-Authentication**
2. Go to the **Password Policy** tab
3. Enable **WebAuthn**
4. Configure as needed for your security requirements

### 8. Configure Multi-Factor Authentication (Optional)

1. Go to **Security** → **Multi-factor Auth**
2. Enable **Auth0 Guardian** or your preferred MFA method
3. Configure TOTP and WebAuthn options
4. Set up step-up authentication policies

### 9. Set Up Custom Domain (Recommended)

1. Go to **Settings** → **Custom Domains**
2. Add your custom domain (e.g., `auth.mystira.com`)
3. Follow the DNS configuration instructions
4. Update your application configuration to use the custom domain

### 10. Configure Account Linking

1. Go to **Authentication** → **Database** → **Username-Password-Authentication**
2. Go to the **Account Linking** tab
3. Enable account linking
4. Configure linking rules for accounts with verified emails

### 11. Enable Security Features

#### Bot Detection
1. Go to **Security** → **Bot Protection**
2. Enable bot detection for passwordless authentication
3. Configure CAPTCHA settings if needed

#### Attack Protection
1. Go to **Security** → **Attack Protection**
2. Configure brute force protection
3. Set up suspicious IP throttling
4. Enable breached password detection

## Application Configuration

### Update appsettings.json

```json
{
  "Auth0": {
    "Authority": "https://YOUR_TENANT.auth0.com",
    "ClientId": "YOUR_CLIENT_ID",
    "DefaultScopes": "openid profile email",
    "Audience": ""
  }
}
```

Replace:
- `YOUR_TENANT.auth0.com` with your Auth0 tenant domain
- `YOUR_CLIENT_ID` with your application's Client ID

### Environment Configuration

#### Development
```json
{
  "Auth0": {
    "Authority": "https://your-dev-tenant.auth0.com",
    "ClientId": "dev-client-id"
  }
}
```

#### Production
```json
{
  "Auth0": {
    "Authority": "https://auth.mystira.com",
    "ClientId": "prod-client-id"
  }
}
```

## Azure Static Web Apps Configuration

### Update staticwebapp.config.json

The application includes a pre-configured `staticwebapp.config.json` with:

- SPA routing fallback to `/index.html`
- No-store headers for authentication routes
- Security headers for XSS and clickjacking protection
- Proper MIME types for WASM and JSON files

### Deployment Configuration

Update your GitHub Actions workflow (`.github/workflows/swa-deploy.yml`) to include:

```yaml
- name: Build And Deploy
  id: builddeploy
  uses: Azure/static-web-apps-deploy@v1
  with:
    azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_ }}
    repo_token: ${{ secrets.GITHUB_TOKEN }}
    action: "upload"
    ###### app location ######
    app_location: "/src/Mystira.App.PWA"
    ###### api location ######
    api_location: ""
    ###### output location ######
    output_location: "bin/Release/net8.0/publish/wwwroot"
    ###### build configuration ######
    build_configuration: "Release"
    dotnet_version: "8.0"
```

## Testing the Implementation

### 1. Local Development
1. Run the PWA locally: `dotnet run --project src/Mystira.App.PWA`
2. Navigate to `http://localhost:5000`
3. Click "Sign In" to test the login flow

### 2. Email Magic Link Flow
1. Enter your email address
2. Check the "Stay signed in" option (optional)
3. Click "Continue"
4. Check your email for the magic link
5. Click the link to complete authentication

### 3. Social Login Flow
1. Click "More options"
2. Select a social provider (Google, Apple, Microsoft)
3. Complete the OAuth flow
4. Verify you're redirected back to the app

### 4. Refresh Token Testing
1. Check "Stay signed in" during login
2. Close and reopen the browser
3. Navigate back to the app
4. Verify you're still authenticated

## Security Considerations

### 1. Magic Link Security
- Keep magic link expiration short (5 minutes)
- Use one-time use links
- Monitor for suspicious login attempts
- Implement rate limiting on passwordless requests

### 2. Refresh Token Security
- Enable refresh token rotation
- Set appropriate token lifetimes
- Monitor refresh token usage
- Implement device fingerprinting if needed

### 3. Social Login Security
- Verify social provider configurations
- Monitor for account enumeration attacks
- Implement proper error handling
- Use secure redirect URIs

### 4. Account Linking Security
- Only link accounts with verified emails
- Implement proper consent flows
- Monitor for account takeover attempts
- Provide clear user controls

## Troubleshooting

### Common Issues

#### Login Redirect Loop
- Verify callback URLs are correctly configured
- Check for CORS issues
- Ensure proper logout handling

#### Magic Link Not Working
- Verify email configuration in Auth0
- Check email delivery settings
- Verify result URL configuration

#### Social Login Failures
- Verify social provider credentials
- Check OAuth app configurations
- Ensure proper redirect URIs

#### Refresh Token Issues
- Verify offline access is enabled
- Check refresh token rotation settings
- Monitor token expiration times

### Debug Mode

Enable debug mode in Auth0:
1. Go to **Support** → **Troubleshooting**
2. Enable debug logs
3. Check real-time logs for authentication issues

## Support and Monitoring

### 1. Auth0 Dashboard Monitoring
- Monitor login success/failure rates
- Track active user sessions
- Monitor API usage and rate limits
- Set up alerts for suspicious activity

### 2. Application Monitoring
- Track authentication state changes
- Monitor token refresh failures
- Log user interaction patterns
- Implement error reporting

### 3. Performance Monitoring
- Monitor login flow completion times
- Track magic link delivery times
- Monitor social login performance
- Optimize for mobile networks

## Additional Resources

- [Auth0 SPA Quickstart](https://auth0.com/docs/quickstarts/spa/blazor-webassembly)
- [Auth0 Passwordless Documentation](https://auth0.com/docs/connections/passwordless)
- [Auth0 Social Documentation](https://auth0.com/docs/connections/social)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/azure/static-web-apps/)
- [Blazor WebAssembly Authentication](https://docs.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-authentication-library)

## License

This implementation follows the Mystira App licensing terms and Auth0's acceptable use policies.