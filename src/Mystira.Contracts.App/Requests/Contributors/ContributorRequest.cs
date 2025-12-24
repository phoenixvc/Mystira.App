using System.ComponentModel.DataAnnotations;
using Mystira.App.Domain.Models;

namespace Mystira.Contracts.App.Requests.Contributors;

/// <summary>
/// Request to add or update a contributor for a scenario or bundle
/// </summary>
public class ContributorRequest
{
    /// <summary>
    /// Display name of the contributor
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Wallet address for Story Protocol royalty payments
    /// </summary>
    [Required]
    [StringLength(42, MinimumLength = 42)]
    [RegularExpression(@"^0x[a-fA-F0-9]{40}$", ErrorMessage = "Invalid Ethereum wallet address format")]
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Role of the contributor
    /// </summary>
    [Required]
    public ContributorRole Role { get; set; }

    /// <summary>
    /// Percentage of revenue/royalties (0-100)
    /// </summary>
    [Required]
    [Range(0.01, 100.0, ErrorMessage = "Contribution percentage must be between 0.01 and 100")]
    public decimal ContributionPercentage { get; set; }

    /// <summary>
    /// Optional email for notifications
    /// </summary>
    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// Optional notes about the contribution
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
