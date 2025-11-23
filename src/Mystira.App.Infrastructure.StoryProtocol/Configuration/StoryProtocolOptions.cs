namespace Mystira.App.Infrastructure.StoryProtocol.Configuration;

/// <summary>
/// Configuration options for Story Protocol integration
/// </summary>
public class StoryProtocolOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "StoryProtocol";

    /// <summary>
    /// Story Protocol network to use (e.g., "mainnet", "testnet", "sepolia")
    /// </summary>
    public string Network { get; set; } = "testnet";

    /// <summary>
    /// RPC endpoint URL for blockchain interactions
    /// </summary>
    public string RpcUrl { get; set; } = string.Empty;

    /// <summary>
    /// Story Protocol contract addresses
    /// </summary>
    public StoryProtocolContracts Contracts { get; set; } = new();

    /// <summary>
    /// Private key for signing transactions (should be stored in Azure Key Vault)
    /// </summary>
    public string? PrivateKey { get; set; }

    /// <summary>
    /// Azure Key Vault name containing Story Protocol secrets
    /// </summary>
    public string? KeyVaultName { get; set; }

    /// <summary>
    /// Whether Story Protocol integration is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Whether to use mock/stub implementation (for development/testing)
    /// </summary>
    public bool UseMockImplementation { get; set; } = true;
}

/// <summary>
/// Story Protocol smart contract addresses
/// </summary>
public class StoryProtocolContracts
{
    /// <summary>
    /// IP Asset Registry contract address
    /// </summary>
    public string IpAssetRegistry { get; set; } = string.Empty;

    /// <summary>
    /// Royalty Module contract address
    /// </summary>
    public string RoyaltyModule { get; set; } = string.Empty;

    /// <summary>
    /// License Registry contract address
    /// </summary>
    public string LicenseRegistry { get; set; } = string.Empty;
}
