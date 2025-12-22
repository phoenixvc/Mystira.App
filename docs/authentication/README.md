# Authentication Documentation

This directory contains documentation for authentication and authorization in the Mystira platform.

## Available Authentication Methods

### Microsoft Entra External ID (Recommended)

**Status**: ✅ Implemented  
**Documentation**: [ENTRA_EXTERNAL_ID_INTEGRATION.md](./ENTRA_EXTERNAL_ID_INTEGRATION.md)  
**PWA Setup**: [ENTRA_EXTERNAL_ID_PWA_SETUP.md](./ENTRA_EXTERNAL_ID_PWA_SETUP.md)

Microsoft Entra External ID is the primary authentication method for Mystira, providing:

- **Social Login**: Google (configured), Facebook, Discord (can be added)
- **Email + Password**: Traditional authentication
- **Centralized Management**: Single identity provider for all apps
- **Enterprise Security**: OAuth 2.0, OpenID Connect, MFA support

**Use Cases**:
- Production PWA authentication
- Mobile app authentication
- API-to-API authentication (service principals)

### Custom Passwordless Authentication (Legacy)

**Status**: ⚠️ Deprecated  
**Documentation**: See `AuthService.cs`

The original custom passwordless authentication using email verification codes.

**Use Cases**:
- Legacy support during migration
- Development/testing without Entra External ID setup

## Quick Start

### For PWA Development

1. **Configure Entra External ID**:
   ```json
   // appsettings.Development.json
   {
     "MicrosoftEntraExternalId": {
       "Authority": "https://mystira.ciamlogin.com/<TENANT_ID>/v2.0",
       "ClientId": "<PWA_CLIENT_ID>",
       "RedirectUri": "http://localhost:5173/authentication/login-callback"
     },
     "Authentication": {
       "Provider": "EntraExternalId"
     }
   }
   ```

2. **Register the service**:
   ```csharp
   // Program.cs
   builder.Services.AddScoped<IAuthService, EntraExternalIdAuthService>();
   ```

3. **Use in components**:
   ```razor
   @inject IAuthService AuthService
   
   <button @onclick="Login">Sign in with Google</button>
   
   @code {
       async Task Login()
       {
           if (AuthService is EntraExternalIdAuthService entra)
           {
               await entra.LoginWithEntraAsync();
           }
       }
   }
   ```

### For API Development

1. **Configure JWT validation** (already done in `appsettings.json`):
   ```json
   {
     "JwtSettings": {
       "JwksEndpoint": "https://mystira.ciamlogin.com/<TENANT_ID>/discovery/v2.0/keys",
       "Issuer": "https://mystira.ciamlogin.com/<TENANT_ID>/v2.0",
       "Audience": "<PUBLIC_API_CLIENT_ID>"
     }
   }
   ```

2. **Update configuration**:
   ```bash
   # Get Public API Client ID from Terraform
   terraform output public_api_client_id
   
   # Update appsettings.json with the Client ID
   ```

3. **Protect endpoints**:
   ```csharp
   [Authorize]
   [HttpGet("profile")]
   public async Task<IActionResult> GetProfile()
   {
       var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       // ...
   }
   ```

See [API Setup Guide](./ENTRA_EXTERNAL_ID_API_SETUP.md) for detailed instructions.

## Architecture

```
┌─────────────────┐
│   Mystira PWA   │
│  (Blazor WASM)  │
└────────┬────────┘
         │ 1. Redirect to login
         ▼
┌─────────────────────────┐
│  Entra External ID      │
│  (mystira.ciamlogin.com)│
│                         │
│  ┌──────────────────┐  │
│  │ Email + Password │  │
│  └──────────────────┘  │
│  ┌──────────────────┐  │
│  │  Google Login    │◄─┼─── 2. User authenticates
│  └──────────────────┘  │
└────────┬────────────────┘
         │ 3. Redirect with tokens
         ▼
┌─────────────────┐
│ /authentication │
│ /login-callback │
└────────┬────────┘
         │ 4. Store tokens
         ▼
┌─────────────────┐
│  Mystira API    │
│  (Validates JWT)│
└─────────────────┘
```

## Configuration Values

### Development

| Setting | Value | Source |
|---------|-------|--------|
| **Tenant ID** | `a816d461-fbf8-4477-83a6-a62ad74ff28f` | Entra admin center |
| **Authority** | `https://mystira.ciamlogin.com/<TENANT_ID>/v2.0` | Constructed |
| **PWA Client ID** | From Terraform output | `terraform output pwa_client_id` |
| **API Client ID** | From Terraform output | `terraform output public_api_client_id` |
| **Redirect URI** | `http://localhost:5173/authentication/login-callback` | PWA configuration |

### Production

| Setting | Value | Source |
|---------|-------|--------|
| **Tenant ID** | `a816d461-fbf8-4477-83a6-a62ad74ff28f` | Same as dev |
| **Authority** | `https://mystira.ciamlogin.com/<TENANT_ID>/v2.0` | Same as dev |
| **PWA Client ID** | From Terraform output | `terraform output pwa_client_id` |
| **API Client ID** | From Terraform output | `terraform output public_api_client_id` |
| **Redirect URI** | `https://mystira.app/authentication/login-callback` | PWA configuration |

## Security Best Practices

### ✅ Do

- Use HTTPS in production
- Validate tokens on every API request
- Store tokens in `localStorage` (for SPAs)
- Use short-lived access tokens (1 hour default)
- Implement token refresh logic
- Validate redirect URIs strictly
- Use PKCE for mobile apps

### ❌ Don't

- Store tokens in cookies (CSRF risk for SPAs)
- Share tokens between different apps
- Log tokens or include in error messages
- Use client secrets in SPAs (they're public clients)
- Skip token validation in APIs
- Allow arbitrary redirect URIs

## Troubleshooting

See individual documentation files for detailed troubleshooting:

- [Entra External ID Integration Issues](./ENTRA_EXTERNAL_ID_INTEGRATION.md#troubleshooting)
- [PWA Setup Issues](./ENTRA_EXTERNAL_ID_PWA_SETUP.md#troubleshooting)
- [Terraform Module Issues](../../Mystira.workspace/infra/terraform/modules/azure-ad-b2c/README.md#troubleshooting)

## Migration Guide

### From Custom Auth to Entra External ID

1. **Phase 1: Setup** (Week 1)
   - Deploy Terraform module
   - Configure Entra External ID tenant
   - Set up Google identity provider

2. **Phase 2: Implementation** (Week 2)
   - Implement `EntraExternalIdAuthService`
   - Create login callback page
   - Update configuration

3. **Phase 3: Testing** (Week 3)
   - Test login flows (Google, email/password)
   - Validate token handling
   - Test API authentication

4. **Phase 4: Migration** (Week 4)
   - Export existing users
   - Import to Entra External ID via Graph API
   - Map user IDs

5. **Phase 5: Deployment** (Week 5)
   - Deploy to staging
   - User acceptance testing
   - Deploy to production

6. **Phase 6: Cleanup** (Week 6)
   - Monitor for issues
   - Remove legacy auth code
   - Update documentation

## Related Documentation

- [ADR-0011: Entra ID Authentication Integration](../architecture/adr/0011-entra-id-authentication-integration.md)
- [Terraform Module README](../../Mystira.workspace/infra/terraform/modules/azure-ad-b2c/README.md)
- [Microsoft Entra External ID Overview](https://learn.microsoft.com/en-us/entra/external-id/external-identities-overview)


## Technical Debt

### MSAL Popup Authentication

The current implementation uses a manual OAuth 2.0 redirect flow. A better user experience can be achieved by using MSAL.js with popup authentication, which would prevent users from leaving the page during sign-in.

**Recommendation**: Refactor the `EntraExternalIdAuthService` to use the `Microsoft.Authentication.WebAssembly.Msal` package with popup mode. This would require:

1. Configuring `AddMsalAuthentication` in `Program.cs` with `LoginMode = "popup"`.
2. Creating a wrapper service that implements `IAuthService` and uses MSAL's built-in authentication methods.
3. Removing the manual OAuth URL construction and redirect logic.

This would provide a more seamless authentication experience and reduce the amount of custom code to maintain.
