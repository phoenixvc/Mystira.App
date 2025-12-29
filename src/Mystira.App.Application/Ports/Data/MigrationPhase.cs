namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Defines the operational mode for polyglot persistence.
/// Used by PolyglotRepository to determine read/write behavior.
///
/// Architecture:
/// - Cosmos DB: Primary store for document data, global distribution, flexible schema
/// - PostgreSQL: Secondary store for relational queries, analytics, reporting
///
/// Modes:
/// 1. CosmosOnly - All operations go to Cosmos DB only
/// 2. DualWrite - Write to both, read from Cosmos (recommended for production)
/// </summary>
public enum MigrationPhase
{
    /// <summary>
    /// All operations use Cosmos DB only.
    /// Use when PostgreSQL is not configured or during initial setup.
    /// </summary>
    CosmosOnly = 0,

    /// <summary>
    /// Write to both Cosmos DB and PostgreSQL, read from Cosmos DB.
    /// Recommended mode for production polyglot persistence.
    /// - Cosmos DB: Primary reads, document storage, global distribution
    /// - PostgreSQL: Analytics, reporting, relational joins
    /// </summary>
    DualWriteCosmosRead = 1
}

/// <summary>
/// Configuration options for polyglot persistence.
/// </summary>
public class MigrationOptions
{
    public const string SectionName = "PolyglotPersistence";

    /// <summary>
    /// Current operational mode for polyglot persistence.
    /// </summary>
    public MigrationPhase Phase { get; set; } = MigrationPhase.CosmosOnly;

    /// <summary>
    /// Enable dual-write compensation on failure.
    /// If write to secondary fails, log and continue (primary write succeeds).
    /// </summary>
    public bool EnableCompensation { get; set; } = true;

    /// <summary>
    /// Timeout for dual-write operations in milliseconds.
    /// </summary>
    public int DualWriteTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Enable validation of data consistency between stores.
    /// Useful for debugging but has performance overhead.
    /// </summary>
    public bool EnableConsistencyValidation { get; set; } = false;
}
