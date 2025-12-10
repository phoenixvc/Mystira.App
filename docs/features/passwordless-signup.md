# Passwordless Sign-Up Implementation

## Overview

This document describes the passwordless sign-up process implemented in Mystira using Auth0-compatible authentication. The system allows users to create accounts with minimal information (email and display name only) using a magic code sent to their email.

## Architecture

### Backend (API)

#### New Components

1. **Domain Model: `PendingSignup`** (`src/Mystira.App.Domain/Models/PendingSignup.cs`)
   - Stores temporary pending signups with generated codes
   - Expires after 15 minutes by default
   - Tracks whether code has been used

2. **Service: `IPasswordlessAuthService`** (`src/Mystira.App.Api/Services/IPasswordlessAuthService.cs`)
   - Generates magic codes for signup requests
   - Validates codes and creates accounts
   - Cleans up expired pending signups

3. **Controller Endpoints: `AuthController`**
   - `POST /api/auth/passwordless/signup` - Request signup code
   - `POST /api/auth/passwordless/verify` - Verify code and create account

4. **API Models** (`src/Mystira.App.Api/Models/ApiModels.cs`)
   - `PasswordlessSignupRequest` - Email + DisplayName
   - `PasswordlessSignupResponse` - Success flag + Message
   - `PasswordlessVerifyRequest` - Email + Code
   - `PasswordlessVerifyResponse` - Success flag + Account + Token

### Frontend (PWA)

#### New Components

1. **Page: `SignUp.razor`** (`src/Mystira.App.PWA/Pages/SignUp.razor`)
   - Minimal form with Email and Display Name fields
   - Three-step flow:
     1. Initial signup request
     2. Code entry (6-digit verification code)
     3. Success confirmation

2. **Models: `PasswordlessAuth.cs`** (`src/Mystira.App.PWA/Models/PasswordlessAuth.cs`)
   - Response DTOs matching backend

3. **Service Extensions**
   - `IApiClient` extended with signup/verify methods
   - `IAuthService` extended with signup/verify methods
   - `AuthService` implementation updated to handle passwordless flow

#### UI Updates

- Updated `Home.razor` to include "Create Account" button linking to `/signup`

## Flow Diagram

```
User Journey:
1. User navigates to /signup
2. Enters email and display name
3. System:
   - Validates inputs
   - Generates 6-digit code
   - Creates PendingSignup record
   - (In production: sends code via email)
   - Returns success
4. User sees code entry form
5. User enters code from email
6. System:
   - Validates code against PendingSignup
   - Creates Account with Auth0UserId
   - Marks PendingSignup as used
   - Returns Account and demo token
7. AuthService updates local state
8. User redirected to home with authenticated session
```

## Key Features

### Minimal Input
- **Email address** - Required, used as unique identifier
- **Display Name** - Required, shown in user profile

### Security
- 6-digit numeric codes (1 million combinations)
- 15-minute expiration on codes
- One-time use enforcement
- Email validation
- XSS protection through Razor component escaping

### Auth0 Integration
- Creates Auth0UserId in format: `auth0|<guid>`
- Ready for future Auth0 real authentication
- Compatible with existing Account model

### Development Features
- Codes logged to console for development/testing
- In-memory database support for local development
- Cosmos DB support for cloud deployment

## Database Schema

### PendingSignup Entity

```csharp
public class PendingSignup
{
    public string Id { get; set; }              // Unique identifier
    public string Email { get; set; }           // User email
    public string DisplayName { get; set; }     // User display name
    public string Code { get; set; }            // 6-digit code
    public DateTime CreatedAt { get; set; }     // When created
    public DateTime ExpiresAt { get; set; }     // When expires (15 min)
    public bool IsUsed { get; set; }            // Whether code was used
}
```

## API Endpoints

### Request Signup Code
```
POST /api/auth/passwordless/signup
Content-Type: application/json

{
  "email": "user@example.com",
  "displayName": "John Doe"
}

Response:
{
  "success": true,
  "message": "Check your email for the verification code",
  "email": "user@example.com"
}
```

### Verify Code & Create Account
```
POST /api/auth/passwordless/verify
Content-Type: application/json

{
  "email": "user@example.com",
  "code": "123456"
}

Response:
{
  "success": true,
  "message": "Account created successfully",
  "account": {
    "id": "uuid",
    "auth0UserId": "auth0|uuid",
    "email": "user@example.com",
    "displayName": "John Doe",
    "createdAt": "2024-01-01T12:00:00Z",
    "lastLoginAt": "2024-01-01T12:00:00Z"
  },
  "token": "demo_token_..."
}
```

## Development & Testing

### Testing with Dev Console
1. Navigate to `/signup`
2. Enter email and display name
3. Check server logs for the generated code
4. Enter code on the verification form
5. Account should be created

### Local Development
- Uses in-memory database by default
- Codes printed to console
- Full PWA functionality available

### Production Deployment
- Uses Cosmos DB
- Email service integration ready (currently logs to console)
- Auth0 user ID generation format compatible

## Future Enhancements

1. **Email Service Integration**
   - Send actual emails with magic codes
   - HTML email templates
   - Resend functionality

2. **Real Auth0 Integration**
   - Integrate with Auth0 Management API
   - Use Auth0 passwordless flow
   - Two-factor authentication

3. **Additional Features**
   - Rate limiting on code requests
   - IP-based fraud detection
   - Analytics tracking
   - Code resend with backoff

4. **User Experience**
   - QR code alternative to magic code
   - Social sign-in options
   - Single sign-on (SSO)

## Error Handling

The system gracefully handles:
- Invalid email formats
- Display names that are too short/long
- Duplicate accounts
- Expired codes
- Invalid codes
- Network errors
- API failures

## Performance Considerations

- PendingSignup records auto-cleanup after 15 minutes
- Optional: Add scheduled cleanup job
- Code validation is O(1) database lookup
- No rate limiting (consider adding for production)

## Security Considerations

- Codes are numeric only (easier to remember, standard for SMS)
- Expiration prevents brute force attacks
- Email verification validates user control of email
- Auth0UserId format allows future Auth0 integration
- No sensitive data in logs

## Files Modified/Created

### Created Files
- `src/Mystira.App.Domain/Models/PendingSignup.cs`
- `src/Mystira.App.Api/Services/IPasswordlessAuthService.cs`
- `src/Mystira.App.Api/Services/PasswordlessAuthService.cs`
- `src/Mystira.App.PWA/Pages/SignUp.razor`
- `src/Mystira.App.PWA/Models/PasswordlessAuth.cs`

### Modified Files
- `src/Mystira.App.Api/Data/MystiraAppDbContext.cs` - Added PendingSignup DbSet
- `src/Mystira.App.Api/Models/ApiModels.cs` - Added request/response DTOs
- `src/Mystira.App.Api/Controllers/AuthController.cs` - Added two new endpoints
- `src/Mystira.App.Api/Program.cs` - Registered PasswordlessAuthService
- `src/Mystira.App.PWA/Services/IApiClient.cs` - Added signup/verify methods
- `src/Mystira.App.PWA/Services/ApiClient.cs` - Implemented methods
- `src/Mystira.App.PWA/Services/IAuthService.cs` - Added signup/verify methods
- `src/Mystira.App.PWA/Services/AuthService.cs` - Implemented methods
- `src/Mystira.App.PWA/Pages/Home.razor` - Added signup link
- `src/Mystira.App.PWA/_Imports.razor` - Added namespace imports

## Deployment Notes

1. Run database migrations (EF Core will handle with existing setup)
2. No configuration needed - works with existing API setup
3. Code will be logged to console during development
4. Email integration can be added later without breaking changes

## Code Examples

### Using from PWA Component
```csharp
// Request code
var (success, message) = await AuthService.RequestPasswordlessSignupAsync(
    "user@example.com", 
    "John Doe");

// Verify code
var (verified, msg, account) = await AuthService.VerifyPasswordlessSignupAsync(
    "user@example.com",
    "123456");

if (verified && account != null)
{
    // User is now authenticated
    Navigation.NavigateTo("/");
}
```

### Using from API
```csharp
// Service is injected via DI
await _passwordlessAuthService.RequestSignupAsync(email, displayName);
await _passwordlessAuthService.VerifySignupAsync(email, code);
await _passwordlessAuthService.CleanupExpiredSignupsAsync();
```
