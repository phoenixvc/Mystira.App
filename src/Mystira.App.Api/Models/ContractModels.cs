namespace Mystira.App.Api.Models;

/// <summary>
/// Local models to bridge gaps with Mystira.Contracts package.
/// These types are defined locally as they are not available in the current Contracts package version.
/// TODO: Move these to Mystira.Contracts package (see docs/contracts-migration.md)
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
