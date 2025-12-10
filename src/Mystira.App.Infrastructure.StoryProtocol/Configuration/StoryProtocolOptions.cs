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
    /// Story Protocol network to use (e.g., "mainnet", "testnet", "odyssey")
    /// </summary>
    public string Network { get; set; } = "odyssey";

    /// <summary>
    /// RPC endpoint URL for blockchain interactions
    /// Default is Story Protocol Odyssey testnet
    /// </summary>
    public string RpcUrl { get; set; } = "https://odyssey.storyrpc.io";

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

    /// <summary>
    /// Maximum retry attempts for blockchain transactions
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 2000;

    /// <summary>
    /// Transaction confirmation timeout in seconds
    /// </summary>
    public int TransactionTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Default gas limit for transactions
    /// </summary>
    public long DefaultGasLimit { get; set; } = 500000;

    /// <summary>
    /// Story Protocol Explorer base URL for generating links
    /// </summary>
    public string ExplorerBaseUrl { get; set; } = "https://odyssey.storyscan.xyz";
}

/// <summary>
/// Story Protocol smart contract addresses
/// Default values are for Odyssey testnet (as of 2024)
/// See: https://docs.story.foundation/docs/deployed-smart-contracts
/// </summary>
public class StoryProtocolContracts
{
    /// <summary>
    /// IP Asset Registry contract address
    /// </summary>
    public string IpAssetRegistry { get; set; } = "0x1a9d0d28a0422F26D31Be72Edc6f13ea4371E11B";

    /// <summary>
    /// Royalty Module contract address
    /// </summary>
    public string RoyaltyModule { get; set; } = "0x3C27b2D7d30131D4b58C3584FD7c86e3358744de";

    /// <summary>
    /// License Registry contract address
    /// </summary>
    public string LicenseRegistry { get; set; } = "0x4f4b1bf7135C7ff1462826CCA81B048Ed19562ed";

    /// <summary>
    /// Licensing Module contract address
    /// </summary>
    public string LicensingModule { get; set; } = "0x04fbd8a2e56dd85CFD5500A4A4DfA955B9f1dE6f";

    /// <summary>
    /// SPG NFT contract address (for minting Story Protocol NFTs)
    /// </summary>
    public string SpgNft { get; set; } = "0xc32A8a0FF3beDDDa58393d022aF433e78739FAbc";

    /// <summary>
    /// PIL (Programmable IP License) Template address
    /// </summary>
    public string PilTemplate { get; set; } = "0x0752f61E59fD2D39193a74610F1bd9a6Ade2E3f9";

    /// <summary>
    /// WIP (Wrapped IP) Token address for royalty payments
    /// </summary>
    public string WipToken { get; set; } = "0x1514000000000000000000000000000000000000";
}
