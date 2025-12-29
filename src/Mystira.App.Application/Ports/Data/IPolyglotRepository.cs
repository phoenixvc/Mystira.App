using Ardalis.Specification;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Polyglot repository interface supporting multiple database backends.
/// Implements permanent dual-write pattern per ADR-0013/0014.
///
/// Architecture:
/// - Primary Store (Cosmos DB): Reads/writes, document data, global distribution
/// - Secondary Store (PostgreSQL): Analytics, reporting, relational queries
///
/// Features:
/// - Ardalis.Specification support for queries
/// - Health checks per backend
/// - Resilience with Polly policies
///
/// Usage:
///   // Reads always from primary store
///   var account = await _repository.FirstOrDefaultAsync(new AccountByEmailSpec(email));
///
///   // Writes go to both stores (when DualWrite mode enabled)
///   await _repository.AddAsync(newAccount);
///   await _repository.SaveChangesAsync();
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IPolyglotRepository<T> : ISpecRepository<T> where T : class
{
    /// <summary>
    /// Get the current operational mode for this repository.
    /// </summary>
    PolyglotMode CurrentMode { get; }

    /// <summary>
    /// Check if the primary backend is healthy.
    /// </summary>
    Task<bool> IsPrimaryHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the secondary backend is healthy.
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
    /// <summary>Primary backend (Cosmos DB)</summary>
    Primary,

    /// <summary>Secondary backend (PostgreSQL)</summary>
    Secondary,

    /// <summary>Cosmos DB - primary document store</summary>
    CosmosDb,

    /// <summary>PostgreSQL - secondary analytics store</summary>
    PostgreSql,

    /// <summary>Redis cache layer</summary>
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
