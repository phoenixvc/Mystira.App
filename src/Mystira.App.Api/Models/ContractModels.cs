using System.ComponentModel.DataAnnotations;

namespace Mystira.App.Api.Models;

/// <summary>
/// Local models to bridge gaps with Mystira.Contracts package.
/// These types are now available in Mystira.Contracts and should be removed
/// once all controllers are updated to use the Contracts namespace.
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
    [Required]
    [MinLength(1)]
    public string IpAssetId { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "ETH";

    public string? PayerReference { get; set; }
}

public class ClaimRoyaltiesRequest
{
    [Required]
    [MinLength(1)]
    public string IpAssetId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string ContributorWallet { get; set; } = string.Empty;
}

// GameSession models
public class CompleteScenarioRequest
{
    [Required]
    [MinLength(1)]
    public string SessionId { get; set; } = string.Empty;

    public Dictionary<string, double>? FinalScores { get; set; }
}

// UserProfile models
public class ProfileAssignmentRequest
{
    [Required]
    [MinLength(1)]
    public string ProfileId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string AccountId { get; set; } = string.Empty;

    public string CharacterId { get; set; } = string.Empty;

    public bool IsNpcAssignment { get; set; } = false;
}
