# Entra External ID API Setup Guide

This guide explains how to configure the Mystira API to validate JWT tokens from Microsoft Entra External ID.

## Overview

The Mystira API validates JWT tokens issued by **Microsoft Entra External ID** to authenticate requests from the PWA. The API uses:

- ✅ **JWKS (JSON Web Key Set)** - Automatic key rotation from Entra External ID
- ✅ **RS256 Asymmetric Verification** - Industry standard for JWT validation
- ✅ **Issuer & Audience Validation** - Ensures tokens are from the correct source
- ✅ **Scope-based Authorization** - Fine-grained access control

## How It Works

```
┌─────────────────┐
│   Mystira PWA   │
│                 │
│  User logs in   │
│  via Entra      │
└────────┬────────┘
         │ Receives JWT access token
         ▼
┌─────────────────────────────────┐
│  PWA makes API request          │
│  Authorization: Bearer <token>  │
└────────┬────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│  Mystira API                    │
│  (JWT Bearer Middleware)        │
│                                 │
│  1. Extract token from header   │
│  2. Fetch JWKS from Entra       │
│  3. Validate signature          │
│  4. Validate issuer             │
│  5. Validate audience           │
│  6. Validate expiry             │
│  7. Extract claims              │
└────────┬────────────────────────┘
         │ Token valid
         ▼
┌─────────────────────────────────┐
│  API Controller                 │
│  [Authorize] attribute          │
│                                 │
│  User.Identity.Name             │
│  User.Claims                    │
└─────────────────────────────────┘
```

## Configuration

### 1. Update appsettings.json

The API configuration has already been updated with Entra External ID settings:

**File**: `src/Mystira.App.Api/appsettings.json`

```json
{
  "JwtSettings": {
    "JwksEndpoint": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/discovery/v2.0/keys",
    "Issuer": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/v2.0",
    "Audience": "<PUBLIC_API_CLIENT_ID>"
  },
  "MicrosoftEntraExternalId": {
    "Instance": "https://mystira.ciamlogin.com",
    "Domain": "mystira.onmicrosoft.com",
    "TenantId": "a816d461-fbf8-4477-83a6-a62ad74ff28f",
    "ClientId": "<PUBLIC_API_CLIENT_ID>",
    "Authority": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/v2.0",
    "ValidScopes": ["API.Access", "Profile.Read", "Stories.Read", "Stories.Write"]
  }
}
```

### 2. Get Public API Client ID from Terraform

```bash
cd C:\dev\Mystira.workspace\infra\terraform\modules\azure-ad-b2c\test
terraform output public_api_client_id
```

**Example output**:
```
public_api_client_id = "87654321-4321-4321-4321-987654321cba"
```

### 3. Update Configuration

Replace `<PUBLIC_API_CLIENT_ID>` in both places:

```json
{
  "JwtSettings": {
    "Audience": "87654321-4321-4321-4321-987654321cba"
  },
  "MicrosoftEntraExternalId": {
    "ClientId": "87654321-4321-4321-4321-987654321cba"
  }
}
```

## JWT Validation Flow

The API's existing JWT validation (in `Program.cs`) automatically handles Entra External ID tokens:

### 1. **JWKS Endpoint Configuration**

```csharp
// From Program.cs (lines 282-296)
if (!string.IsNullOrWhiteSpace(jwksEndpoint))
{
    // Use JWKS endpoint for key rotation support (most secure)
    options.MetadataAddress = jwksEndpoint;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    
    // Configure refresh interval for JWKS keys
    options.RefreshInterval = TimeSpan.FromHours(1);
    options.AutomaticRefreshInterval = TimeSpan.FromHours(24);
}
```

**What this does**:
- Fetches public keys from Entra External ID's JWKS endpoint
- Caches keys for 1 hour
- Automatically refreshes keys every 24 hours
- Supports key rotation without downtime

### 2. **Token Validation Parameters**

```csharp
// From Program.cs (lines 267-280)
var validationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,           // Must match Entra External ID issuer
    ValidateAudience = true,         // Must match Public API Client ID
    ValidateLifetime = true,         // Token must not be expired
    ValidateIssuerSigningKey = true, // Signature must be valid
    ValidIssuer = jwtIssuer,
    ValidAudience = jwtAudience,
    ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 min clock skew
    RoleClaimType = "role",          // Map "role" claim to ClaimTypes.Role
    NameClaimType = "name"           // Map "name" claim to ClaimTypes.Name
};
```

### 3. **Authentication Events**

The API logs authentication events for monitoring:

- **OnTokenValidated**: Successful authentication
- **OnAuthenticationFailed**: Failed validation (logged with reason)
- **OnChallenge**: Unauthorized access attempt

## Token Structure

### Access Token from Entra External ID

```json
{
  "iss": "https://mystira.ciamlogin.com/a816d461-fbf8-4477-83a6-a62ad74ff28f/v2.0",
  "aud": "87654321-4321-4321-4321-987654321cba",
  "sub": "12345678-1234-1234-1234-123456789abc",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "scp": "API.Access Profile.Read Stories.Read Stories.Write",
  "iat": 1640000000,
  "exp": 1640003600
}
```

**Claims**:
- `iss` (issuer): Entra External ID authority
- `aud` (audience): Public API Client ID
- `sub` (subject): User's unique ID in Entra
- `name`: User's display name
- `email`: User's email address
- `scp` (scopes): Granted permissions
- `iat` (issued at): Token issue time
- `exp` (expiry): Token expiration time

## Using Authentication in Controllers

### Basic Authorization

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        // User is authenticated - token was validated
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.Identity?.Name;
        
        return Ok(new { userId, email, name });
    }
}
```

### Scope-based Authorization

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StoriesController : ControllerBase
{
    [HttpGet]
    [RequiredScope("Stories.Read")]
    public async Task<IActionResult> GetStories()
    {
        // User must have "Stories.Read" scope
        return Ok(stories);
    }
    
    [HttpPost]
    [RequiredScope("Stories.Write")]
    public async Task<IActionResult> CreateStory([FromBody] Story story)
    {
        // User must have "Stories.Write" scope
        return Ok(createdStory);
    }
}
```

### Custom Scope Validation

```csharp
public class RequiredScopeAttribute : TypeFilterAttribute
{
    public RequiredScopeAttribute(params string[] scopes) 
        : base(typeof(RequiredScopeFilter))
    {
        Arguments = new object[] { scopes };
    }
}

public class RequiredScopeFilter : IAuthorizationFilter
{
    private readonly string[] _scopes;
    
    public RequiredScopeFilter(string[] scopes)
    {
        _scopes = scopes;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var scopeClaim = context.HttpContext.User.FindFirst("scp")?.Value;
        if (string.IsNullOrEmpty(scopeClaim))
        {
            context.Result = new ForbidResult();
            return;
        }
        
        var userScopes = scopeClaim.Split(' ');
        if (!_scopes.Any(s => userScopes.Contains(s)))
        {
            context.Result = new ForbidResult();
        }
    }
}
```

## CORS Configuration

Ensure the API allows requests from the PWA:

```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPWA", policy =>
    {
        policy.WithOrigins(
            "https://mystira.app",
            "http://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ...

app.UseCors("AllowPWA");
```

## Testing

### 1. Get a Token from PWA

1. Run the PWA: `dotnet run --project src/Mystira.App.PWA`
2. Log in with Google
3. Open browser DevTools → Application → Local Storage
4. Copy the value of `mystira_entra_token`

### 2. Test API with Token

```bash
# Using curl
curl -H "Authorization: Bearer <token>" \
     https://localhost:5260/api/profile

# Using PowerShell
$token = "<your-token>"
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Uri "https://localhost:5260/api/profile" -Headers $headers
```

### 3. Expected Response

**Success (200 OK)**:
```json
{
  "userId": "12345678-1234-1234-1234-123456789abc",
  "email": "john.doe@example.com",
  "name": "John Doe"
}
```

**Unauthorized (401)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**Forbidden (403)** - Missing required scope:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

## Troubleshooting

### Issue: "401 Unauthorized" on all requests

**Possible causes**:
1. Token expired (tokens are valid for 1 hour)
2. Audience mismatch (API Client ID doesn't match token audience)
3. Issuer mismatch (Authority doesn't match token issuer)

**Solution**:
1. Check token expiry: Decode the token at [jwt.io](https://jwt.io) and check `exp` claim
2. Verify `Audience` in appsettings matches the Public API Client ID
3. Verify `Issuer` matches the authority in the token's `iss` claim

### Issue: "Unable to obtain configuration from JWKS endpoint"

**Possible causes**:
1. JWKS endpoint URL is incorrect
2. Network connectivity issues
3. Entra External ID tenant not accessible

**Solution**:
1. Verify JWKS endpoint: `https://mystira.ciamlogin.com/<TENANT_ID>/discovery/v2.0/keys`
2. Test endpoint in browser - should return JSON with keys
3. Check API logs for detailed error messages

### Issue: "Signature validation failed"

**Possible causes**:
1. Token was signed by a different authority
2. JWKS keys not fetched correctly
3. Token was tampered with

**Solution**:
1. Verify the token was issued by Entra External ID
2. Restart the API to force JWKS refresh
3. Check API logs for signature validation errors

### Issue: "Scope validation failed"

**Possible causes**:
1. Token doesn't contain required scopes
2. PWA didn't request the correct scopes during login
3. Scopes not configured in Entra External ID app registration

**Solution**:
1. Check token claims at [jwt.io](https://jwt.io) - look for `scp` claim
2. Verify PWA requests scopes in `LoginWithEntraAsync()` method
3. Verify scopes are exposed in Public API app registration in Entra

## Security Considerations

### ✅ Best Practices

1. **Use HTTPS in Production**
   - Required for OAuth 2.0 flows
   - Protects tokens in transit

2. **Validate All Token Claims**
   - Issuer, audience, expiry are all validated
   - Prevents token reuse across different APIs

3. **Use JWKS for Key Rotation**
   - Automatic key rotation without downtime
   - No manual key management required

4. **Short Token Lifetime**
   - Default: 1 hour
   - Reduces risk if token is compromised

5. **Scope-based Authorization**
   - Fine-grained access control
   - Principle of least privilege

### ⚠️ Important Notes

1. **Public API Client ID is Public**
   - Safe to commit to repository
   - Used for audience validation

2. **No Client Secret Needed**
   - API validates tokens, doesn't issue them
   - Entra External ID handles token issuance

3. **JWKS Caching**
   - Keys cached for 1 hour
   - Automatic refresh every 24 hours
   - Reduces load on Entra External ID

## Monitoring

### Application Insights

The API logs authentication events to Application Insights:

**Successful authentication**:
```
JWT token validated for user: john.doe@example.com
```

**Failed authentication**:
```
JWT authentication failed on /api/profile (UA: Mozilla/5.0...)
Exception: IDX10223: Lifetime validation failed. The token is expired.
```

### Security Metrics

The API tracks security metrics:

- `TrackAuthenticationSuccess("JWT", userId)` - Successful logins
- `TrackTokenValidationFailed(clientIp, reason)` - Failed validations

## Next Steps

1. ✅ Update `appsettings.json` with Public API Client ID
2. ✅ Test API authentication with PWA tokens
3. ✅ Add scope-based authorization to controllers
4. ✅ Monitor authentication logs in Application Insights
5. ✅ Deploy to staging and production

## Related Documentation

- [PWA Setup Guide](./ENTRA_EXTERNAL_ID_PWA_SETUP.md)
- [Authentication Overview](./README.md)
- [Terraform Module](../../Mystira.workspace/infra/terraform/modules/azure-ad-b2c/README.md)
- [Microsoft Entra External ID Token Reference](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens)
