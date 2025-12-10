namespace Mystira.App.Contracts.Responses.Attribution;

/// <summary>
/// Customer-facing response containing content attribution information.
/// This shows who created the content without exposing sensitive wallet/payment details.
/// </summary>
public class ContentAttributionResponse
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
    public bool IsIpRegistered { get; set; }

    /// <summary>
    /// The Story Protocol IP Asset ID (if registered)
    /// </summary>
    public string? IpAssetId { get; set; }

    /// <summary>
    /// When the content was registered on Story Protocol
    /// </summary>
    public DateTime? RegisteredAt { get; set; }

    /// <summary>
    /// List of contributors/creators
    /// </summary>
    public List<CreatorCreditResponse> Credits { get; set; } = new();
}

/// <summary>
/// Customer-facing credit for a content creator.
/// Excludes sensitive information like wallet addresses.
/// </summary>
public class CreatorCreditResponse
{
    /// <summary>
    /// Creator's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The creator's role (e.g., "Writer", "Artist", "Voice Actor")
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Optional: contribution percentage (for transparency)
    /// </summary>
    public decimal? ContributionPercentage { get; set; }
}
