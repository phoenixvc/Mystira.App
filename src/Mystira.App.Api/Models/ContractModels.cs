namespace Mystira.App.Api.Models;

/// <summary>
/// Local models to bridge gaps with Mystira.Contracts package.
/// These types are defined locally as they are not available in the current Contracts package version.
/// </summary>

// Common models
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
}

public class ErrorResponse
{
    public string? Error { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public string? TraceId { get; set; }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object>? Results { get; set; }
}

public class AgeGroupDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MinimumAge { get; set; }
    public int MaximumAge { get; set; }
}

public class ArchetypeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Auth models
public class PasswordlessSignupRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class PasswordlessSignupResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Email { get; set; }
    public string? ErrorDetails { get; set; }
}

public class PasswordlessVerifyRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class PasswordlessVerifyResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Account { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string? ErrorDetails { get; set; }
}

public class PasswordlessSigninRequest
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordlessSigninResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Email { get; set; }
    public string? ErrorDetails { get; set; }
}

public class RefreshTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}

// Royalty models
public class PayRoyaltyRequest
{
    public string IpAssetId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ETH";
    public string? PayerReference { get; set; }
}

public class ClaimRoyaltiesRequest
{
    public string IpAssetId { get; set; } = string.Empty;
    public string ContributorWallet { get; set; } = string.Empty;
}

// GameSession models
public class CompleteScenarioRequest
{
    public string SessionId { get; set; } = string.Empty;
    public Dictionary<string, double>? FinalScores { get; set; }
}

// UserProfile models
public class ProfileAssignmentRequest
{
    public string ProfileId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
}
