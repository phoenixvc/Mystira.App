using Mystira.App.Domain.Models;

namespace Mystira.App.Contracts.Responses.Contributors;

/// <summary>
/// Response containing contributor information
/// </summary>
public class ContributorResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WalletAddress { get; set; } = string.Empty;
    public ContributorRole Role { get; set; }
    public decimal ContributionPercentage { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response containing Story Protocol metadata for a content piece
/// </summary>
public class StoryProtocolResponse
{
    public string? IpAssetId { get; set; }
    public string? RegistrationTxHash { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public string? RoyaltyModuleId { get; set; }
    public bool IsRegistered { get; set; }
    public List<ContributorResponse> Contributors { get; set; } = new();
    public int ContributorCount { get; set; }
    public decimal TotalPercentage { get; set; }
}
