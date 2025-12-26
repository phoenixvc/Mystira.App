# Entra External ID PWA Setup Guide

This guide explains how to configure and use Microsoft Entra External ID authentication in the Mystira PWA.

## Overview

The Mystira PWA now supports authentication via **Microsoft Entra External ID** (formerly Azure AD B2C), which provides:

- ✅ **Google Social Login** - Users can sign in with their Google accounts
- ✅ **Email + Password** - Traditional email/password authentication
- ✅ **Secure Token Management** - OAuth 2.0 / OpenID Connect standards
- ✅ **Centralized Identity Management** - Single source of truth for user identities

## Configuration

### 1. Update appsettings.json

Add the Entra External ID configuration to your environment-specific appsettings file:

**appsettings.Development.json**:
```json
{
  "MicrosoftEntraExternalId": {
    "Authority": "https://mystira.ciamlogin.com/<TENANT_ID>/v2.0",
    "ClientId": "<PWA_CLIENT_ID>",
    "RedirectUri": "http://localhost:5173/authentication/login-callback",
    "PostLogoutRedirectUri": "http://localhost:5173"
  },
  "Authentication": {
    "Provider": "EntraExternalId",
    "EnableGoogleLogin": true
  }
}
```

**appsettings.Production.json**:
```json
{
  "MicrosoftEntraExternalId": {
    "Authority": "https://mystira.ciamlogin.com/<TENANT_ID>/v2.0",
    "ClientId": "<PWA_CLIENT_ID>",
    "RedirectUri": "https://mystira.app/authentication/login-callback",
    "PostLogoutRedirectUri": "https://mystira.app"
  },
  "Authentication": {
    "Provider": "EntraExternalId",
    "EnableGoogleLogin": true
  }
}
```

### 2. Get Configuration Values

After running the Terraform module (see `Mystira.workspace/infra/terraform/modules/azure-ad-b2c`), you'll get:

```bash
terraform output pwa_client_id
# Output: 12345678-1234-1234-1234-123456789abc

terraform output pwa_config
# Output includes Authority, ClientId, TenantId, etc.
```

Replace `<PWA_CLIENT_ID>` and `<TENANT_ID>` in your appsettings with these values.

### 3. Register Redirect URIs in Azure

Ensure the redirect URIs are registered in the Entra External ID app registration:

1. Go to [Microsoft Entra admin center](https://entra.microsoft.com/)
2. Switch to your External ID tenant
3. Navigate to **Applications** → **App registrations**
4. Select "Mystira PWA (dev)" or "Mystira PWA (prod)"
5. Go to **Authentication**
6. Under **Single-page application**, verify these redirect URIs are listed:
   - Development: `http://localhost:5173/authentication/login-callback`
   - Production: `https://mystira.app/authentication/login-callback`

## Service Registration

### Program.cs

Update `Program.cs` to register the Entra External ID authentication service:

```csharp
using Mystira.App.PWA.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ... other services ...

// Register authentication service based on configuration
var authProvider = builder.Configuration["Authentication:Provider"];

if (authProvider == "EntraExternalId")
{
    builder.Services.AddScoped<IAuthService, EntraExternalIdAuthService>();
    builder.Logging.AddFilter("Mystira.App.PWA.Services.EntraExternalIdAuthService", LogLevel.Debug);
}
else
{
    // Fallback to custom auth service
    builder.Services.AddScoped<IAuthService, AuthService>();
}

await builder.Build().RunAsync();
```

## Usage

### Login Flow

1. **User clicks "Sign in with Google"** (or email/password)
2. **PWA calls** `EntraExternalIdAuthService.LoginWithEntraAsync()`
3. **User is redirected** to Entra External ID login page
4. **User authenticates** (via Google or email/password)
5. **Entra redirects back** to `/authentication/login-callback`
6. **LoginCallback.razor** processes the tokens
7. **User is redirected** to home page, now authenticated

### Code Example

**Login.razor**:
```razor
@inject IAuthService AuthService

<button class="btn btn-primary" @onclick="LoginWithGoogle">
    <i class="fab fa-google"></i> Sign in with Google
</button>

@code {
    private async Task LoginWithGoogle()
    {
        if (AuthService is EntraExternalIdAuthService entraService)
        {
            await entraService.LoginWithEntraAsync();
        }
    }
}
```

### Logout Flow

```csharp
await AuthService.LogoutAsync();
// User is logged out locally and redirected to Entra logout endpoint
```

## Google Social Login Setup

To enable Google login in Entra External ID:

### 1. Create Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Navigate to **APIs & Services** → **Credentials**
4. Click **Create Credentials** → **OAuth 2.0 Client ID**
5. Application type: **Web application**
6. Authorized redirect URIs:
   ```
   https://mystira.ciamlogin.com/<TENANT_NAME>/oauth2/authresp
   ```
7. Save **Client ID** and **Client Secret**

### 2. Configure Google in Entra External ID

1. Go to [Microsoft Entra admin center](https://entra.microsoft.com/)
2. Switch to your External ID tenant
3. Navigate to **External ID** → **Identity providers**
4. Click **+ Google**
5. Enter:
   - **Client ID**: From Google Cloud Console
   - **Client Secret**: From Google Cloud Console
6. Click **Save**

### 3. Add Google to Sign-in Flow

1. Navigate to **External ID** → **User flows**
2. Select your user flow (or create one)
3. Under **Identity providers**, check **Google**
4. Click **Save**

## Security Considerations

### Token Storage

- **Access tokens** are stored in `localStorage` as `mystira_entra_token`
- **ID tokens** are stored in `localStorage` as `mystira_entra_id_token`
- Tokens are automatically included in API requests via `IAuthService.GetTokenAsync()`

### CORS Configuration

Ensure your API allows requests from your PWA origin:

```csharp
// In API Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPWA", policy =>
    {
        policy.WithOrigins("https://mystira.app", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Token Validation in API

The API should validate tokens from Entra External ID:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://mystira.ciamlogin.com/<TENANT_ID>/v2.0";
        options.Audience = "<API_CLIENT_ID>";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });
```

## Troubleshooting

### Issue: "Configuration missing" error

**Solution**: Ensure `appsettings.json` has the `MicrosoftEntraExternalId` section with all required fields.

### Issue: Redirect loop after login

**Solution**: Check that the redirect URI in appsettings matches exactly what's registered in Entra External ID.

### Issue: Google login not showing

**Solution**: 
1. Verify Google is configured as an identity provider in Entra External ID
2. Ensure Google is enabled in the user flow
3. Check that Google OAuth credentials are correct

### Issue: "State mismatch" error

**Solution**: This is a security check. Clear browser cache and try again. If persists, check for clock skew between client and server.

## Migration from Custom Auth

If you're migrating from the custom passwordless authentication:

1. **Keep both services** during transition:
   ```csharp
   builder.Services.AddScoped<IAuthService, EntraExternalIdAuthService>();
   builder.Services.AddScoped<AuthService>(); // Legacy service
   ```

2. **Migrate existing users**:
   - Export users from your database
   - Import to Entra External ID via Microsoft Graph API
   - Map `ExternalId` field to Entra `sub` claim

3. **Update UI** to show both login options during transition

4. **Remove legacy auth** after full migration

## Testing

### Local Testing

1. Start the PWA: `dotnet run --project src/Mystira.App.PWA`
2. Navigate to `http://localhost:5173`
3. Click "Sign in with Google"
4. Authenticate with your Google account
5. Verify you're redirected back and logged in

### Production Testing

1. Deploy PWA to production
2. Ensure redirect URIs are registered for production domain
3. Test login flow end-to-end
4. Verify tokens are valid and API calls succeed

## Additional Resources

- [Microsoft Entra External ID Documentation](https://learn.microsoft.com/en-us/entra/external-id/)
- [OAuth 2.0 Implicit Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-implicit-grant-flow)
- [Google OAuth 2.0 Setup](https://developers.google.com/identity/protocols/oauth2)
- [Terraform Module README](../../Mystira.workspace/infra/terraform/modules/azure-ad-b2c/README.md)
