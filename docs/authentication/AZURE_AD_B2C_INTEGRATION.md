# Azure AD B2C Authentication Integration Guide

This guide explains how to integrate Azure AD B2C authentication into the Mystira.App Blazor PWA.

## Overview

Azure AD B2C provides enterprise-grade consumer identity and access management with support for social login providers (Google, Discord, etc.) and custom user flows.

## Architecture

The integration follows a **hybrid authentication model**:

1. **Azure AD B2C** handles user authentication and identity management
2. **Mystira API** validates B2C tokens and issues its own JWT tokens for API access
3. **PWA** uses MSAL.js to authenticate with B2C and exchanges tokens with the API

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│             │  Auth   │              │  Token  │             │
│  Blazor PWA │────────▶│  Azure B2C   │────────▶│ Mystira API │
│             │◀────────│              │◀────────│             │
└─────────────┘  Token  └──────────────┘  JWT    └─────────────┘
```

## Prerequisites

Before integrating Azure AD B2C, ensure you have:

1. **Azure AD B2C tenant** created and configured (see Terraform module documentation)
2. **App registrations** created via Terraform in `Mystira.workspace/infra/terraform/modules/azure-ad-b2c/`
3. **Client IDs** from Terraform outputs for PWA and API
4. **User flows** configured (Sign-up/Sign-in, Password Reset, Profile Edit)
5. **Identity providers** configured (Google, Discord)

## Step 1: Install Required NuGet Packages

Add the following packages to `Mystira.App.PWA.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Authentication.WebAssembly.Msal" Version="8.0.0" />
  <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" Version="8.0.0" />
</ItemGroup>
```

## Step 2: Configure Azure AD B2C Settings

Add B2C configuration to `wwwroot/appsettings.json`:

```json
{
  "AzureAdB2C": {
    "Authority": "https://mystirab2c.b2clogin.com/mystirab2c.onmicrosoft.com/B2C_1_SignUpSignIn",
    "ClientId": "<PWA_CLIENT_ID_FROM_TERRAFORM>",
    "ValidateAuthority": false,
    "KnownAuthorities": ["mystirab2c.b2clogin.com"]
  },
  "MystiraApi": {
    "Scopes": ["https://mystirab2c.onmicrosoft.com/mystira-api/API.Access"],
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
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Authentication.WebAssembly.Msal;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ... existing registrations ...

// Configure Azure AD B2C authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
    
    // Add API scopes
    var apiScopes = builder.Configuration.GetSection("MystiraApi:Scopes").Get<string[]>();
    if (apiScopes != null)
    {
        foreach (var scope in apiScopes)
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
        }
    }
    
    // Configure cache location
    options.ProviderOptions.Cache.CacheLocation = "localStorage";
});

await builder.Build().RunAsync();
```

## Step 4: Create B2C Authentication Service

Create `Services/AzureB2CAuthService.cs`:

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mystira.App.PWA.Services;

public interface IAzureB2CAuthService
{
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task LoginAsync();
    Task LogoutAsync();
}

public class AzureB2CAuthService : IAzureB2CAuthService
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly NavigationManager _navigation;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureB2CAuthService> _logger;

    public AzureB2CAuthService(
        IAccessTokenProvider tokenProvider,
        NavigationManager navigation,
        IConfiguration configuration,
        ILogger<AzureB2CAuthService> logger)
    {
        _tokenProvider = tokenProvider;
        _navigation = navigation;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var scopes = _configuration.GetSection("MystiraApi:Scopes").Get<string[]>();
            var tokenResult = await _tokenProvider.RequestAccessToken(
                new AccessTokenRequestOptions
                {
                    Scopes = scopes
                });

            if (tokenResult.TryGetToken(out var token))
            {
                return token.Value;
            }

            _logger.LogWarning("Failed to acquire access token");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access token");
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public Task LoginAsync()
    {
        _navigation.NavigateToLogin("authentication/login");
        return Task.CompletedTask;
    }

    public Task LogoutAsync()
    {
        _navigation.NavigateToLogout("authentication/logout");
        return Task.CompletedTask;
    }
}
```

Register the service in `Program.cs`:

```csharp
builder.Services.AddScoped<IAzureB2CAuthService, AzureB2CAuthService>();
```

## Step 5: Create Authentication Pages

Create `Pages/Authentication.razor`:

```razor
@page "/authentication/{action}"
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication

<RemoteAuthenticatorView Action="@Action">
    <LoggingIn>
        <div class="auth-container">
            <h3>Logging in...</h3>
            <p>Please wait while we authenticate you.</p>
        </div>
    </LoggingIn>
    
    <CompletingLoggingIn>
        <div class="auth-container">
            <h3>Completing login...</h3>
        </div>
    </CompletingLoggingIn>
    
    <LogInFailed>
        <div class="auth-container error">
            <h3>Login Failed</h3>
            <p>There was an error logging you in. Please try again.</p>
            <a href="/">Return to home</a>
        </div>
    </LogInFailed>
    
    <LogOut>
        <div class="auth-container">
            <h3>Logging out...</h3>
        </div>
    </LogOut>
    
    <CompletingLogOut>
        <div class="auth-container">
            <h3>Completing logout...</h3>
        </div>
    </CompletingLogOut>
    
    <LogOutFailed>
        <div class="auth-container error">
            <h3>Logout Failed</h3>
            <p>There was an error logging you out.</p>
            <a href="/">Return to home</a>
        </div>
    </LogOutFailed>
</RemoteAuthenticatorView>

@code {
    [Parameter]
    public string? Action { get; set; }
}
```

## Step 6: Update App.razor

Wrap the app with authentication state provider:

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated != true)
                    {
                        <RedirectToLogin />
                    }
                    else
                    {
                        <p>You are not authorized to access this resource.</p>
                    }
                </NotAuthorized>
            </AuthorizeRouteView>
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

## Step 7: Configure API to Validate B2C Tokens

Update `Mystira.App.Api/Program.cs` to validate Azure AD B2C tokens:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add Azure AD B2C authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

// ... rest of configuration ...
```

Add B2C configuration to `appsettings.json`:

```json
{
  "AzureAdB2C": {
    "Instance": "https://mystirab2c.b2clogin.com",
    "Domain": "mystirab2c.onmicrosoft.com",
    "ClientId": "<PUBLIC_API_CLIENT_ID_FROM_TERRAFORM>",
    "SignUpSignInPolicyId": "B2C_1_SignUpSignIn"
  }
}
```

## Step 8: Update AuthHeaderHandler

Modify `Services/AuthHeaderHandler.cs` to support both authentication methods:

```csharp
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenProvider _tokenProvider;
    private readonly IAzureB2CAuthService _b2cAuthService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthHeaderHandler> _logger;

    public AuthHeaderHandler(
        ITokenProvider tokenProvider,
        IAzureB2CAuthService b2cAuthService,
        IConfiguration configuration,
        ILogger<AuthHeaderHandler> logger)
    {
        _tokenProvider = tokenProvider;
        _b2cAuthService = b2cAuthService;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Check if B2C authentication is enabled
        var useB2C = _configuration.GetValue<bool>("AzureAdB2C:Enabled", false);

        string? token = null;

        if (useB2C)
        {
            // Try B2C authentication first
            token = await _b2cAuthService.GetAccessTokenAsync();
        }

        // Fall back to custom JWT authentication
        if (string.IsNullOrEmpty(token))
        {
            token = await _tokenProvider.GetTokenAsync();
        }

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

## Step 9: Testing

### Local Development

1. Update `appsettings.Development.json` with B2C configuration
2. Set redirect URI in Azure B2C: `https://localhost:5001/authentication/login-callback`
3. Run the PWA: `dotnet run --project src/Mystira.App.PWA`
4. Navigate to `/authentication/login` to test

### Production Deployment

1. Configure production redirect URIs in Azure B2C
2. Update `appsettings.Production.json` with production B2C settings
3. Deploy and verify authentication flow

## Troubleshooting

### Common Issues

**Issue**: "AADB2C90077: User does not have an existing session"
- **Solution**: Ensure user flows are correctly configured in Azure B2C

**Issue**: "CORS error when authenticating"
- **Solution**: Add PWA origin to allowed CORS origins in API configuration

**Issue**: "Token validation failed"
- **Solution**: Verify API Client ID matches the one in Azure B2C app registration

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

1. **Token Storage**: Tokens are stored in localStorage by default. Consider using sessionStorage for sensitive applications.
2. **HTTPS Only**: Always use HTTPS in production to protect tokens in transit.
3. **Token Expiration**: Implement automatic token refresh to maintain user sessions.
4. **Scope Validation**: Validate API scopes on the backend to ensure proper authorization.

## Migration Strategy

To migrate existing users to Azure AD B2C:

1. **Phase 1**: Enable B2C alongside existing authentication (hybrid mode)
2. **Phase 2**: Migrate user accounts to B2C using Azure AD B2C User Migration API
3. **Phase 3**: Deprecate custom JWT authentication
4. **Phase 4**: Remove custom authentication code

## References

- [Azure AD B2C Documentation](https://docs.microsoft.com/en-us/azure/active-directory-b2c/)
- [MSAL.NET Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-overview)
- [Blazor Authentication Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/)
- [Terraform Module](../../Mystira.workspace/infra/terraform/modules/azure-ad-b2c/README.md)
