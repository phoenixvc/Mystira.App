namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Defines the operational mode for polyglot persistence.
/// Used by PolyglotRepository to determine read/write behavior.
///
/// Architecture:
/// - Primary Store (Cosmos DB): Document data, global distribution, flexible schema
/// - Secondary Store (PostgreSQL): Analytics, reporting, relational queries
///
/// Modes:
/// - SingleStore: All operations go to primary store only
/// - DualWrite: Write to both stores, read from primary (recommended for production)
/// </summary>
public enum PolyglotMode
{
    /// <summary>
    /// All operations use primary store only (Cosmos DB).
    /// Use when secondary store is not configured or during initial setup.
    /// </summary>
    SingleStore = 0,

    /// <summary>
    /// Write to both primary and secondary stores, read from primary.
    /// Recommended mode for production polyglot persistence.
    /// - Primary (Cosmos DB): All reads, source of truth
    /// - Secondary (PostgreSQL): Analytics, reporting, relational joins
    /// </summary>
    DualWrite = 1
}

/// <summary>
/// Configuration options for polyglot persistence.
/// </summary>
public class PolyglotOptions
{
    public const string SectionName = "PolyglotPersistence";

    /// <summary>
    /// Current operational mode for polyglot persistence.
    /// </summary>
    public PolyglotMode Mode { get; set; } = PolyglotMode.SingleStore;

    /// <summary>
    /// Enable compensation on secondary write failure.
    /// If write to secondary fails, log error and continue (primary write succeeds).
    /// </summary>
    public bool EnableCompensation { get; set; } = true;

    /// <summary>
    /// Timeout for secondary write operations in milliseconds.
    /// </summary>
    public int SecondaryWriteTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Enable validation of data consistency between stores.
    /// Useful for debugging but has performance overhead.
    /// </summary>
    public bool EnableConsistencyValidation { get; set; } = false;
}
