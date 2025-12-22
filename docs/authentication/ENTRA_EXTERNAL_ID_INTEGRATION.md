# Microsoft Entra External ID Integration Guide

This guide explains how to integrate Microsoft Entra External ID authentication into the Mystira.App Blazor PWA.

> **Important**: As of May 1, 2025, Azure AD B2C is no longer available for new customers. This guide has been updated for **Microsoft Entra External ID**, the next-generation customer identity and access management (CIAM) solution.

## Overview

Microsoft Entra External ID provides enterprise-grade consumer identity and access management with support for social login providers (Google, Facebook, etc.) and customizable sign-up experiences.

## Architecture

The integration follows a **hybrid authentication model**:

1. **Microsoft Entra External ID** handles user authentication and identity management.
2. **Mystira API** validates External ID tokens and issues its own JWT tokens for API access.
3. **PWA** uses MSAL.js to authenticate with External ID and exchanges tokens with the API.

```
┌─────────────┐         ┌───────────────────┐         ┌─────────────┐
│             │  Auth   │                   │  Token  │             │
│  Blazor PWA │────────▶│  Entra External ID  │────────▶│ Mystira API │
│             │◀────────│                   │◀────────│             │
└─────────────┘  Token  └───────────────────┘  JWT    └─────────────┘
```

## Prerequisites

Before integrating Entra External ID, ensure you have:

1. **External ID tenant** created and configured (see Terraform module documentation).
2. **App registrations** created via Terraform in `Mystira.workspace/infra/terraform/modules/azure-ad-b2c/`.
3. **Client IDs** from Terraform outputs for PWA and API.
4. **Sign-up and sign-in experiences** configured.
5. **Identity providers** configured (Google, Facebook, etc.).

## Step 1: Install Required NuGet Packages

Add the following packages to `Mystira.App.PWA.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Authentication.WebAssembly.Msal" Version="8.0.0" />
</ItemGroup>
```

## Step 2: Configure `appsettings.json`

Add External ID configuration to `wwwroot/appsettings.json`:

```json
{
  "MicrosoftEntraExternalId": {
    "Authority": "https://mystira.ciamlogin.com/<YOUR_TENANT_ID>/v2.0",
    "ClientId": "<PWA_CLIENT_ID_FROM_TERRAFORM>",
    "ValidateAuthority": true
  },
  "MystiraApi": {
    "Scopes": ["api://<API_CLIENT_ID>/API.Access"],
    "BaseUrl": "https://api.mystira.app/"
  }
}
```

**Environment-specific configuration:**

- **Development**: `wwwroot/appsettings.Development.json`
- **Staging**: `wwwroot/appsettings.Staging.json`
- **Production**: `wwwroot/appsettings.Production.json`

## Step 3: Register MSAL Authentication

Update `Program.cs` to register MSAL authentication services:

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ... existing registrations ...

// Configure Microsoft Entra External ID authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("MicrosoftEntraExternalId", options.ProviderOptions.Authentication);
    
    // Add API scopes
    var apiScopes = builder.Configuration.GetSection("MystiraApi:Scopes").Get<string[]>();
    if (apiScopes != null)
    {
        foreach (var scope in apiScopes)
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
        }
    }
});

await builder.Build().RunAsync();
```

## Step 4: Update `AuthHeaderHandler.cs`

Modify `Services/AuthHeaderHandler.cs` to use the External ID token:

```csharp
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _msalTokenProvider;
    private readonly IConfiguration _configuration;

    public AuthHeaderHandler(IAccessTokenProvider msalTokenProvider, IConfiguration configuration)
    {
        _msalTokenProvider = msalTokenProvider;
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var scopes = _configuration.GetSection("MystiraApi:Scopes").Get<string[]>();
        var tokenResult = await _msalTokenProvider.RequestAccessToken(new AccessTokenRequestOptions { Scopes = scopes });

        if (tokenResult.TryGetToken(out var token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

## Step 5: Update `App.razor`

Wrap the `Router` in `App.razor` with `CascadingAuthenticationState`:

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

## Step 6: Add Authentication Pages

Create `Pages/Authentication.razor` to handle MSAL redirects:

```razor
@page "/authentication/{action}"
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication

<RemoteAuthenticatorView Action="@Action" />
```

## Step 7: Configure API to Validate Tokens

Update `Mystira.App.Api/Program.cs` to validate External ID tokens:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add Microsoft Entra External ID authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("MicrosoftEntraExternalId"));

// ... rest of configuration ...

app.UseAuthentication();
app.UseAuthorization();
```

Add External ID configuration to the API's `appsettings.json`:

```json
{
  "MicrosoftEntraExternalId": {
    "Instance": "https://mystira.ciamlogin.com",
    "Domain": "mystira.onmicrosoft.com",
    "TenantId": "<YOUR_TENANT_ID>",
    "ClientId": "<PUBLIC_API_CLIENT_ID_FROM_TERRAFORM>"
  }
}
```

## Step 8: Testing

### Local Development

1. Update `appsettings.Development.json` with External ID configuration.
2. Set redirect URI in Entra External ID: `https://localhost:5001/authentication/login-callback`.
3. Run the PWA: `dotnet run --project src/Mystira.App.PWA`.
4. Navigate to `/authentication/login` to test.

### Production Deployment

1. Configure production redirect URIs in Entra External ID.
2. Update `appsettings.Production.json` with production settings.
3. Deploy and verify authentication flow.

## Troubleshooting

### Common Issues

**Issue**: "CORS error when authenticating"
- **Solution**: Add PWA origin to allowed CORS origins in API configuration.

**Issue**: "Token validation failed"
- **Solution**: Verify API Client ID and Tenant ID match the ones in Entra External ID.

### Debug Logging

Enable detailed authentication logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.Identity": "Debug"
    }
  }
}
```

## Security Considerations

1. **Token Storage**: Tokens are stored in `localStorage` by default. Consider `sessionStorage` for sensitive applications.
2. **HTTPS Only**: Always use HTTPS in production.
3. **Token Expiration**: Implement automatic token refresh.
4. **Scope Validation**: Validate API scopes on the backend.

## Migration from Azure AD B2C

1. **Phase 1**: Create new External ID tenant and configure it.
2. **Phase 2**: Update Terraform configuration and run it.
3. **Phase 3**: Migrate users using the Microsoft Graph API.
4. **Phase 4**: Update application configuration and code.
5. **Phase 5**: Test and gradually migrate traffic.

## References

- [Microsoft Entra External ID Documentation](https://learn.microsoft.com/en-us/entra/external-id/)
- [Blazor WebAssembly MSAL Authentication](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/hosted-with-microsoft-entra-id)
- [Terraform Module](../Mystira.workspace/infra/terraform/modules/azure-ad-b2c/README.md)
