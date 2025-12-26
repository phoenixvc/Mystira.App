namespace Mystira.App.Application.Configuration.StoryProtocol;

/// <summary>
/// Configuration options for gRPC communication with Mystira.Chain service.
/// Used by GrpcChainServiceAdapter for polyglot integration.
/// </summary>
/// <remarks>
/// See ADR-0013 for architectural rationale on gRPC adoption.
/// </remarks>
public class ChainServiceOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "ChainService";

    /// <summary>
    /// gRPC endpoint URL for Mystira.Chain service
    /// </summary>
    public string GrpcEndpoint { get; set; } = "https://localhost:50051";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Whether to enable automatic retry with exponential backoff
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// WIP (Wrapped IP) Token address for royalty payments
    /// </summary>
    public string WipTokenAddress { get; set; } = "0x1514000000000000000000000000000000000000";

    /// <summary>
    /// Whether to use gRPC (true) or direct Nethereum (false) implementation
    /// </summary>
    public bool UseGrpc { get; set; } = false;

    /// <summary>
    /// Whether to enable TLS for gRPC connection
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// API key for service-to-service authentication (if required).
    /// Load from Key Vault in production.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Whether to enable health checks for the Chain service
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// API key header name for authentication
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "x-api-key";
}
