namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents a contributor to content (scenarios, bundles) with their role and revenue share
/// </summary>
public class Contributor
{
    /// <summary>
    /// Unique identifier for the contributor
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the contributor
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Wallet address for Story Protocol royalty payments (blockchain address)
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Role of the contributor in content creation
    /// </summary>
    public ContributorRole Role { get; set; }

    /// <summary>
    /// Percentage of revenue/royalties (0-100)
    /// </summary>
    public decimal ContributionPercentage { get; set; }

    /// <summary>
    /// Email for notifications (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Additional notes about the contribution
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When this contributor was added
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the contributor data
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Contributor name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(WalletAddress))
        {
            errors.Add("Wallet address cannot be empty.");
        }
        else if (!IsValidWalletAddress(WalletAddress))
        {
            errors.Add("Wallet address format is invalid.");
        }

        if (ContributionPercentage < 0 || ContributionPercentage > 100)
        {
            errors.Add("Contribution percentage must be between 0 and 100.");
        }

        return !errors.Any();
    }

    /// <summary>
    /// Basic validation for Ethereum-style wallet addresses (0x followed by 40 hex chars)
    /// </summary>
    private bool IsValidWalletAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        // Basic Ethereum address validation: 0x followed by 40 hexadecimal characters
        if (!address.StartsWith("0x") || address.Length != 42)
        {
            return false;
        }

        return address.Substring(2).All(c => "0123456789abcdefABCDEF".Contains(c));
    }
}

/// <summary>
/// Roles that contributors can have in content creation
/// </summary>
public enum ContributorRole
{
    /// <summary>
    /// Primary story writer
    /// </summary>
    Writer = 0,

    /// <summary>
    /// Visual artist (character art, scene art, etc.)
    /// </summary>
    Artist = 1,

    /// <summary>
    /// Voice actor for character dialogue
    /// </summary>
    VoiceActor = 2,

    /// <summary>
    /// Music composer
    /// </summary>
    MusicComposer = 3,

    /// <summary>
    /// Sound effects designer
    /// </summary>
    SoundDesigner = 4,

    /// <summary>
    /// Editor who reviews and improves content
    /// </summary>
    Editor = 5,

    /// <summary>
    /// Game/scenario designer
    /// </summary>
    GameDesigner = 6,

    /// <summary>
    /// Quality assurance/tester
    /// </summary>
    QualityAssurance = 7,

    /// <summary>
    /// Other contributors not covered by specific roles
    /// </summary>
    Other = 99
}
