using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mystira.App.Infrastructure.Data.Polyglot;

/// <summary>
/// Entity for tracking dual-write sync operations between Cosmos DB and PostgreSQL.
/// Stored in PostgreSQL _polyglot_sync_log table.
/// </summary>
[Table("_polyglot_sync_log")]
public class PolyglotSyncLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(36)]
    [Column("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("operation")]
    public string Operation { get; set; } = string.Empty; // INSERT, UPDATE, DELETE

    [Required]
    [MaxLength(20)]
    [Column("source_backend")]
    public string SourceBackend { get; set; } = "cosmos"; // cosmos, postgres

    [MaxLength(20)]
    [Column("sync_status")]
    public string SyncStatus { get; set; } = "pending"; // pending, synced, failed

    [Column("cosmos_timestamp")]
    public DateTime? CosmosTimestamp { get; set; }

    [Column("postgres_timestamp")]
    public DateTime PostgresTimestamp { get; set; } = DateTime.UtcNow;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sync operation types for polyglot persistence logging.
/// </summary>
public static class SyncOperation
{
    public const string Insert = "INSERT";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
}

/// <summary>
/// Sync status values for polyglot persistence logging.
/// </summary>
public static class SyncStatus
{
    public const string Pending = "pending";
    public const string Synced = "synced";
    public const string Failed = "failed";
}
