namespace Mystira.App.Domain.Models;

/// <summary>
/// Story Protocol blockchain integration metadata for IP assets
/// </summary>
public class StoryProtocolMetadata
{
    /// <summary>
    /// Story Protocol IP Asset ID (assigned after registration on blockchain)
    /// </summary>
    public string? IpAssetId { get; set; }

    /// <summary>
    /// Blockchain transaction hash of the IP registration
    /// </summary>
    public string? RegistrationTxHash { get; set; }

    /// <summary>
    /// When the IP was registered on Story Protocol
    /// </summary>
    public DateTime? RegisteredAt { get; set; }

    /// <summary>
    /// Story Protocol royalty module configuration ID
    /// </summary>
    public string? RoyaltyModuleId { get; set; }

    /// <summary>
    /// Whether this content is registered on Story Protocol
    /// </summary>
    public bool IsRegistered => !string.IsNullOrEmpty(IpAssetId);

    /// <summary>
    /// List of contributors with their royalty splits
    /// </summary>
    public List<Contributor> Contributors { get; set; } = new();

    /// <summary>
    /// Validates that contributor percentages sum to 100%
    /// </summary>
    public bool ValidateContributorSplits(out List<string> errors)
    {
        errors = new List<string>();

        if (Contributors == null || !Contributors.Any())
        {
            errors.Add("At least one contributor is required.");
            return false;
        }

        // Validate each contributor
        foreach (var contributor in Contributors)
        {
            if (!contributor.Validate(out var contributorErrors))
            {
                errors.AddRange(contributorErrors);
            }
        }

        // Check if percentages sum to 100%
        var totalPercentage = Contributors.Sum(c => c.ContributionPercentage);
        if (Math.Abs(totalPercentage - 100m) > 0.01m) // Allow small floating point differences
        {
            errors.Add($"Contributor percentages must sum to 100%. Current sum: {totalPercentage}%");
        }

        // Check for duplicate wallet addresses
        var duplicateWallets = Contributors
            .GroupBy(c => c.WalletAddress.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateWallets.Any())
        {
            errors.Add($"Duplicate wallet addresses found: {string.Join(", ", duplicateWallets)}");
        }

        return !errors.Any();
    }

    /// <summary>
    /// Gets total contributor count
    /// </summary>
    public int ContributorCount => Contributors?.Count ?? 0;
}
