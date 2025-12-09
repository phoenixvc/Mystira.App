namespace Mystira.App.Domain.Models;

/// <summary>
/// Result of a royalty payment transaction
/// </summary>
public class RoyaltyPaymentResult
{
    /// <summary>
    /// Unique ID for this payment
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// The IP Asset ID that received the payment
    /// </summary>
    public string IpAssetId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction hash on the blockchain
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Amount paid (in WIP token)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Token used for payment (e.g., WIP token address)
    /// </summary>
    public string TokenAddress { get; set; } = string.Empty;

    /// <summary>
    /// Optional payer reference (e.g., order ID)
    /// </summary>
    public string? PayerReference { get; set; }

    /// <summary>
    /// When the payment was made
    /// </summary>
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the payment was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Distribution breakdown by contributor
    /// </summary>
    public List<RoyaltyDistribution> Distributions { get; set; } = new();
}

/// <summary>
/// Individual royalty distribution to a contributor
/// </summary>
public class RoyaltyDistribution
{
    /// <summary>
    /// Contributor wallet address
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Contributor name (if known)
    /// </summary>
    public string? ContributorName { get; set; }

    /// <summary>
    /// Share percentage
    /// </summary>
    public decimal SharePercentage { get; set; }

    /// <summary>
    /// Amount distributed to this contributor
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Claimable royalty balance for an IP Asset
/// </summary>
public class RoyaltyBalance
{
    /// <summary>
    /// The IP Asset ID
    /// </summary>
    public string IpAssetId { get; set; } = string.Empty;

    /// <summary>
    /// Total claimable balance across all contributors
    /// </summary>
    public decimal TotalClaimable { get; set; }

    /// <summary>
    /// Total amount already claimed
    /// </summary>
    public decimal TotalClaimed { get; set; }

    /// <summary>
    /// Total amount paid into this IP Asset
    /// </summary>
    public decimal TotalReceived { get; set; }

    /// <summary>
    /// Token used for royalties (e.g., WIP token address)
    /// </summary>
    public string TokenAddress { get; set; } = string.Empty;

    /// <summary>
    /// Breakdown by contributor
    /// </summary>
    public List<ContributorBalance> ContributorBalances { get; set; } = new();

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Balance information for a single contributor
/// </summary>
public class ContributorBalance
{
    /// <summary>
    /// Contributor wallet address
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Contributor name (if known)
    /// </summary>
    public string? ContributorName { get; set; }

    /// <summary>
    /// Share percentage
    /// </summary>
    public decimal SharePercentage { get; set; }

    /// <summary>
    /// Amount available to claim
    /// </summary>
    public decimal ClaimableAmount { get; set; }

    /// <summary>
    /// Amount already claimed
    /// </summary>
    public decimal ClaimedAmount { get; set; }
}
