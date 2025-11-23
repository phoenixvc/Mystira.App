# Mystira.App.Shared

Shared infrastructure and cross-cutting concerns used across multiple layers of the application. This project contains reusable services, middleware, and utilities that don't fit neatly into a single architectural layer.

## Role in Hexagonal Architecture

**Layer**: **Shared Infrastructure (Cross-Cutting Concerns)**

The Shared layer provides:
- **Cross-cutting concerns** - Functionality used across multiple layers
- **Infrastructure utilities** - Logging, security, validation
- **Reusable services** - Services used by multiple projects
- **Middleware components** - HTTP pipeline components

**Dependency Flow**:
```
Multiple Layers (API, PWA, Application)
    ↓ use
Shared Infrastructure (THIS)
    ↓ uses
Domain Layer (for models)
```

**Key Principles**:
- ✅ **Reusable** - Shared across multiple projects
- ✅ **Framework-specific** - ASP.NET Core middleware and services
- ✅ **Infrastructure concerns** - Not business logic
- ✅ **Minimal dependencies** - Only depends on Domain

## Project Structure

```
Mystira.App.Shared/
├── Services/
│   ├── IUserProfileService.cs          # User profile service interface
│   ├── UserProfileService.cs           # User profile implementation
│   └── (Other shared services)
├── Middleware/
│   ├── SecurityHeadersMiddleware.cs    # HTTP security headers
│   └── (Other middleware)
├── Logging/
│   ├── PiiRedactor.cs                  # PII redaction for logs
│   └── (Other logging utilities)
├── Models/
│   ├── UserProfileRequests.cs          # Shared request models
│   └── (Other shared models)
└── Mystira.App.Shared.csproj
```

## Core Components

### Services

#### IUserProfileService / UserProfileService

Shared user profile management service:

```csharp
public interface IUserProfileService
{
    Task<UserProfile?> GetProfileAsync(string userId);
    Task<UserProfile> CreateProfileAsync(CreateProfileRequest request);
    Task UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task DeleteProfileAsync(string userId);
}
```

**Usage**:
- Used by API controllers
- Used by admin dashboard
- Encapsulates common profile operations
- Coordinates with repositories and domain logic

**Example**:
```csharp
public class UserProfileService : IUserProfileService
{
    private readonly IRepository<UserProfile> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<UserProfile> CreateProfileAsync(CreateProfileRequest request)
    {
        var profile = new UserProfile
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = request.DisplayName,
            AgeGroup = request.AgeGroup,
            FantasyTheme = request.FantasyTheme,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        return profile;
    }
}
```

### Middleware

#### SecurityHeadersMiddleware

Adds security headers to HTTP responses:

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";

        await _next(context);
    }
}
```

**Security Headers**:
- **X-Content-Type-Options**: Prevent MIME sniffing
- **X-Frame-Options**: Prevent clickjacking
- **X-XSS-Protection**: Enable XSS filtering
- **Referrer-Policy**: Control referrer information
- **Content-Security-Policy (CSP)**: Prevent XSS and injection attacks

**Usage**:
```csharp
// In Program.cs
app.UseMiddleware<SecurityHeadersMiddleware>();
```

### Logging

#### PiiRedactor

Redacts Personally Identifiable Information (PII) from logs:

```csharp
public class PiiRedactor
{
    public string RedactEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return email;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***@***";

        var localPart = parts[0];
        var domain = parts[1];

        var redactedLocal = localPart.Length > 2
            ? $"{localPart[0]}***{localPart[^1]}"
            : "***";

        return $"{redactedLocal}@{domain}";
    }

    public string RedactPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            return "***-***-****";

        return $"***-***-{phone[^4..]}";
    }

    public string RedactName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return name.Length > 1
            ? $"{name[0]}***"
            : "***";
    }
}
```

**COPPA Compliance**:
- Redacts email addresses: `john.doe@example.com` → `j***e@example.com`
- Redacts phone numbers: `123-456-7890` → `***-***-7890`
- Redacts names: `John Doe` → `J***`

**Usage**:
```csharp
var redactor = new PiiRedactor();
_logger.LogInformation("User logged in: {Email}", redactor.RedactEmail(user.Email));
```

### Models

#### UserProfileRequests

Shared request DTOs for user profile operations:

```csharp
public record CreateProfileRequest
{
    public required string DisplayName { get; init; }
    public required AgeGroup AgeGroup { get; init; }
    public FantasyTheme? FantasyTheme { get; init; }
    public string? AvatarUrl { get; init; }
}

public record UpdateProfileRequest
{
    public string? DisplayName { get; init; }
    public FantasyTheme? FantasyTheme { get; init; }
    public string? AvatarUrl { get; init; }
}
```

**Purpose**:
- Shared between API and Admin API
- Consistent validation rules
- Single source of truth for request shapes

## Usage in Projects

### In API Projects

```csharp
// Program.cs
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

var app = builder.Build();
app.UseMiddleware<SecurityHeadersMiddleware>();
```

### In Controllers

```csharp
[ApiController]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly PiiRedactor _piiRedactor;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProfileRequest request)
    {
        _logger.LogInformation(
            "Creating profile for {DisplayName}",
            _piiRedactor.RedactName(request.DisplayName)
        );

        var profile = await _profileService.CreateProfileAsync(request);
        return Ok(profile);
    }
}
```

## Dependency Injection

Register shared services:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddSingleton<PiiRedactor>();

        return services;
    }

    public static IApplicationBuilder UseSharedMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();

        return app;
    }
}
```

**Usage**:
```csharp
// In API Program.cs
builder.Services.AddSharedServices();

var app = builder.Build();
app.UseSharedMiddleware();
```

## Security Features

### HTTP Security Headers

The `SecurityHeadersMiddleware` implements OWASP security best practices:

1. **Prevent MIME Sniffing**
   - Header: `X-Content-Type-Options: nosniff`
   - Prevents browsers from interpreting files as different MIME types

2. **Prevent Clickjacking**
   - Header: `X-Frame-Options: DENY`
   - Prevents the app from being embedded in iframes

3. **XSS Protection**
   - Header: `X-XSS-Protection: 1; mode=block`
   - Enables browser XSS filtering

4. **Referrer Policy**
   - Header: `Referrer-Policy: strict-origin-when-cross-origin`
   - Controls referrer information leakage

5. **Content Security Policy (CSP)**
   - Header: `Content-Security-Policy: ...`
   - Mitigates XSS, clickjacking, and code injection attacks

### PII Protection

The `PiiRedactor` ensures COPPA and GDPR compliance:

- **Child Protection (COPPA)**: No child PII in logs
- **Data Privacy (GDPR)**: Minimal PII exposure
- **Audit Compliance**: Logs without sensitive data

## Testing

### Unit Testing Services

```csharp
[Fact]
public async Task CreateProfileAsync_WithValidRequest_CreatesProfile()
{
    // Arrange
    var mockRepository = new Mock<IRepository<UserProfile>>();
    var mockUnitOfWork = new Mock<IUnitOfWork>();
    var service = new UserProfileService(mockRepository.Object, mockUnitOfWork.Object);

    var request = new CreateProfileRequest
    {
        DisplayName = "Test User",
        AgeGroup = AgeGroup.Ages7to9
    };

    // Act
    var profile = await service.CreateProfileAsync(request);

    // Assert
    Assert.NotNull(profile);
    Assert.Equal("Test User", profile.DisplayName);
}
```

### Unit Testing PII Redaction

```csharp
[Fact]
public void RedactEmail_WithValidEmail_RedactsCorrectly()
{
    // Arrange
    var redactor = new PiiRedactor();

    // Act
    var redacted = redactor.RedactEmail("john.doe@example.com");

    // Assert
    Assert.Equal("j***e@example.com", redacted);
}
```

### Integration Testing Middleware

```csharp
[Fact]
public async Task SecurityHeadersMiddleware_AddsSecurityHeaders()
{
    // Arrange
    var context = new DefaultHttpContext();
    var middleware = new SecurityHeadersMiddleware(next: (ctx) => Task.CompletedTask);

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
    Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
}
```

## Configuration

Shared services may have configuration:

```json
{
  "Shared": {
    "EnablePiiRedaction": true,
    "SecurityHeaders": {
      "EnableCSP": true,
      "CSPDirectives": "default-src 'self'; ..."
    }
  }
}
```

## Best Practices

1. **Keep It Thin**: Only truly shared code belongs here
2. **Avoid Business Logic**: Business rules belong in Domain/Application
3. **Single Responsibility**: Each service/middleware does one thing
4. **Testability**: All components should be unit testable
5. **Documentation**: Document security implications

## Common Patterns

### Extension Methods

Provide fluent configuration:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedServices();
        // Configure based on appsettings
        return services;
    }
}
```

### Scoped vs Singleton

- **Singleton**: `PiiRedactor` (stateless, reusable)
- **Scoped**: `UserProfileService` (per-request, uses DbContext)
- **Transient**: Rarely used in Shared

## Future Enhancements

- **Rate Limiting Middleware**: Prevent API abuse
- **Request Logging Middleware**: Structured request/response logs
- **Error Handling Middleware**: Centralized exception handling
- **Caching Service**: Distributed caching abstraction
- **Email Service**: Shared email sending
- **SMS Service**: Shared SMS notifications

## Related Documentation

- **[Domain](../Mystira.App.Domain/README.md)** - Domain models used by shared services
- **[Application](../Mystira.App.Application/README.md)** - Application layer using shared services
- **[API](../Mystira.App.Api/README.md)** - API using shared middleware

## License

Copyright (c) 2025 Mystira. All rights reserved.
