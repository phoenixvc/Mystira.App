namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Defines the current phase of database migration.
/// Used by PolyglotRepository to determine read/write behavior.
///
/// Migration flow:
/// 1. CosmosOnly - All operations go to Cosmos DB (current state)
/// 2. DualWriteCosmosRead - Write to both, read from Cosmos
/// 3. DualWritePostgresRead - Write to both, read from PostgreSQL
/// 4. PostgresOnly - All operations go to PostgreSQL
/// </summary>
public enum MigrationPhase
{
    /// <summary>
    /// All operations use Cosmos DB only (initial state).
    /// </summary>
    CosmosOnly = 0,

    /// <summary>
    /// Write to both databases, read from Cosmos DB.
    /// Use this phase to backfill PostgreSQL with existing data.
    /// </summary>
    DualWriteCosmosRead = 1,

    /// <summary>
    /// Write to both databases, read from PostgreSQL.
    /// Use this phase to validate PostgreSQL data before cutover.
    /// </summary>
    DualWritePostgresRead = 2,

    /// <summary>
    /// All operations use PostgreSQL only (target state).
    /// </summary>
    PostgresOnly = 3
}

/// <summary>
/// Configuration options for polyglot persistence migration.
/// </summary>
public class MigrationOptions
{
    public const string SectionName = "PolyglotPersistence";

    /// <summary>
    /// Current migration phase for this entity type.
    /// </summary>
    public MigrationPhase Phase { get; set; } = MigrationPhase.CosmosOnly;

    /// <summary>
    /// Enable dual-write compensation on failure.
    /// If write to secondary fails, attempt compensation.
    /// </summary>
    public bool EnableCompensation { get; set; } = true;

    /// <summary>
    /// Timeout for dual-write operations in milliseconds.
    /// </summary>
    public int DualWriteTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Enable validation of data consistency between stores.
    /// </summary>
    public bool EnableConsistencyValidation { get; set; } = false;
}
