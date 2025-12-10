namespace Mystira.App.Contracts.Responses.Attribution;

/// <summary>
/// Response containing IP registration verification status.
/// Used to verify if content is registered on Story Protocol blockchain.
/// </summary>
public class IpVerificationResponse
{
    /// <summary>
    /// The content ID (scenario or bundle)
    /// </summary>
    public string ContentId { get; set; } = string.Empty;

    /// <summary>
    /// The content title
    /// </summary>
    public string ContentTitle { get; set; } = string.Empty;

    /// <summary>
    /// Whether this content is registered as an IP Asset on Story Protocol
    /// </summary>
    public bool IsRegistered { get; set; }

    /// <summary>
    /// The Story Protocol IP Asset ID (if registered)
    /// </summary>
    public string? IpAssetId { get; set; }

    /// <summary>
    /// When the content was registered on Story Protocol
    /// </summary>
    public DateTime? RegisteredAt { get; set; }

    /// <summary>
    /// The blockchain transaction hash that registered this IP asset
    /// </summary>
    public string? RegistrationTxHash { get; set; }

    /// <summary>
    /// The Story Protocol royalty module ID (if configured)
    /// </summary>
    public string? RoyaltyModuleId { get; set; }

    /// <summary>
    /// Number of contributors associated with this IP asset
    /// </summary>
    public int ContributorCount { get; set; }

    /// <summary>
    /// URL to view this IP asset on Story Protocol explorer (if registered)
    /// </summary>
    public string? ExplorerUrl { get; set; }
}
