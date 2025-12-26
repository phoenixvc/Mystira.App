using Ardalis.Specification;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Polyglot repository interface supporting multiple database backends.
/// Implements dual-write patterns for gradual database migration per ADR-0013/0014.
///
/// Features:
/// - Ardalis.Specification support for queries
/// - Migration phase awareness (Cosmos-only, dual-write, Postgres-only)
/// - Health checks per backend
/// - Resilience with Polly policies
///
/// Usage:
///   // Read operations use the appropriate backend based on migration phase
///   var account = await _repository.FirstOrDefaultAsync(new AccountByEmailSpec(email));
///
///   // Write operations may go to multiple backends during migration
///   await _repository.AddAsync(newAccount);
///   await _repository.SaveChangesAsync();
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IPolyglotRepository<T> : ISpecRepository<T> where T : class
{
    /// <summary>
    /// Get the current migration phase for this repository.
    /// </summary>
    MigrationPhase CurrentPhase { get; }

    /// <summary>
    /// Check if the primary backend is healthy.
    /// </summary>
    Task<bool> IsPrimaryHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the secondary backend is healthy (when in dual-write mode).
    /// </summary>
    Task<bool> IsSecondaryHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Force read from a specific backend (for debugging/validation).
    /// </summary>
    Task<T?> GetFromBackendAsync(
        string id,
        BackendType backend,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate consistency between backends for an entity.
    /// Returns true if both backends have identical data.
    /// </summary>
    Task<ConsistencyResult> ValidateConsistencyAsync(
        string id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Backend type for explicit backend access.
/// </summary>
public enum BackendType
{
    /// <summary>Primary read backend (depends on migration phase)</summary>
    Primary,

    /// <summary>Secondary write backend (depends on migration phase)</summary>
    Secondary,

    /// <summary>Cosmos DB backend</summary>
    CosmosDb,

    /// <summary>PostgreSQL backend</summary>
    PostgreSql,

    /// <summary>Redis cache</summary>
    Cache
}

/// <summary>
/// Result of consistency validation between backends.
/// </summary>
public class ConsistencyResult
{
    public bool IsConsistent { get; set; }
    public string? PrimaryValue { get; set; }
    public string? SecondaryValue { get; set; }
    public List<string> Differences { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}
