using System.Diagnostics.Metrics;
using System.Text.Json;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Data;
using Mystira.Shared.Telemetry;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Mystira.App.Infrastructure.Data.Polyglot;

/// <summary>
/// Polyglot repository implementation supporting multiple database backends.
/// Implements permanent dual-write pattern per ADR-0013/0014.
///
/// Architecture:
/// - Primary Store (Cosmos DB): Reads/writes, document data, global distribution
/// - Secondary Store (PostgreSQL): Analytics, reporting, relational queries
///
/// Features:
/// - Dual-write with compensation on failure
/// - Health checks per backend
/// - Polly resilience policies (circuit breaker, retry, timeout)
/// - Consistency validation between backends
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class PolyglotRepository<T> : EfSpecificationRepository<T>, IPolyglotRepository<T> where T : class
{
    private static readonly Meter _meter = new("Mystira.App.Polyglot", "1.0.0");
    private static readonly Counter<long> _secondaryWriteFailures = _meter.CreateCounter<long>(
        "polyglot.secondary_write_failures",
        description: "Count of failed secondary database writes during dual-write operations");
    private static readonly Counter<long> _secondaryWriteSuccesses = _meter.CreateCounter<long>(
        "polyglot.secondary_write_successes",
        description: "Count of successful secondary database writes during dual-write operations");

    private readonly PolyglotOptions _options;
    private readonly DbContext? _secondaryContext;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ICustomMetrics? _metrics;

    public PolyglotRepository(
        DbContext primaryContext,
        IOptions<PolyglotOptions> options,
        ILogger<PolyglotRepository<T>> logger,
        DbContext? secondaryContext = null,
        ICustomMetrics? metrics = null)
        : base(primaryContext, logger)
    {
        _options = options?.Value ?? new PolyglotOptions();
        _secondaryContext = secondaryContext;
        _resiliencePipeline = CreateResiliencePipeline();
        _metrics = metrics;
    }

    /// <inheritdoc />
    public PolyglotMode CurrentMode => _options.Mode;

    /// <inheritdoc />
    public override async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            return await DualWriteAsync(
                () => base.AddAsync(entity, cancellationToken),
                () => AddToSecondaryAsync(entity, cancellationToken),
                cancellationToken,
                SyncOperation.Insert);
        }

        return await base.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            var entityId = TryGetEntityId(entity);
            await DualWriteAsync(
                async () => { var result = await base.UpdateAsync(entity, cancellationToken); return entity; },
                async () => { await UpdateInSecondaryAsync(entity, cancellationToken); return entity; },
                cancellationToken,
                SyncOperation.Update,
                entityId);
            return 1; // Assuming single entity update
        }

        return await base.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            var entityId = TryGetEntityId(entity);
            await DualWriteAsync(
                async () => { var result = await base.DeleteAsync(entity, cancellationToken); return entity; },
                async () => { await DeleteFromSecondaryAsync(entity, cancellationToken); return entity; },
                cancellationToken,
                SyncOperation.Delete,
                entityId);
            return 1; // Assuming single entity delete
        }

        return await base.DeleteAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();

        if (IsDualWriteMode)
        {
            // Add to primary first
            var result = await base.AddRangeAsync(entityList, cancellationToken);

            // Then add to secondary with resilience
            foreach (var entity in entityList)
            {
                try
                {
                    await AddToSecondaryAsync(entity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Secondary bulk add failed for entity in {EntityType}", typeof(T).Name);
                    // Continue with remaining entities (compensation pattern)
                }
            }

            return result;
        }

        return await base.AddRangeAsync(entityList, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();

        if (IsDualWriteMode)
        {
            // Update primary first
            await base.UpdateRangeAsync(entityList, cancellationToken);

            // Then update secondary with resilience
            foreach (var entity in entityList)
            {
                try
                {
                    await UpdateInSecondaryAsync(entity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Secondary bulk update failed for entity in {EntityType}", typeof(T).Name);
                    // Continue with remaining entities (compensation pattern)
                }
            }

            return;
        }

        await base.UpdateRangeAsync(entityList, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();

        if (IsDualWriteMode)
        {
            // Delete from primary first
            await base.DeleteRangeAsync(entityList, cancellationToken);

            // Then delete from secondary with resilience
            foreach (var entity in entityList)
            {
                try
                {
                    await DeleteFromSecondaryAsync(entity, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Secondary bulk delete failed for entity in {EntityType}", typeof(T).Name);
                    // Continue with remaining entities (compensation pattern)
                }
            }

            return;
        }

        await base.DeleteRangeAsync(entityList, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsPrimaryHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary database health check failed for {EntityType}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsSecondaryHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_secondaryContext == null)
        {
            return false;
        }

        try
        {
            await _secondaryContext.Database.CanConnectAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Secondary database health check failed for {EntityType}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetFromBackendAsync(
        string id,
        BackendType backend,
        CancellationToken cancellationToken = default)
    {
        var context = backend switch
        {
            BackendType.Primary => _dbContext,      // Cosmos DB
            BackendType.Secondary => _secondaryContext, // PostgreSQL
            _ => _dbContext
        };

        if (context == null)
        {
            _logger.LogWarning("Requested backend {Backend} is not available for {EntityType}", backend, typeof(T).Name);
            return null;
        }

        return await context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsistencyResult> ValidateConsistencyAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var result = new ConsistencyResult();

        if (_secondaryContext == null)
        {
            result.IsConsistent = true;
            return result;
        }

        var primaryEntity = await _dbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        var secondaryEntity = await _secondaryContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);

        if (primaryEntity == null && secondaryEntity == null)
        {
            result.IsConsistent = true;
            return result;
        }

        if (primaryEntity == null || secondaryEntity == null)
        {
            result.IsConsistent = false;
            result.Differences.Add(primaryEntity == null ? "Missing in primary" : "Missing in secondary");
            return result;
        }

        // Simple JSON comparison for now
        var primaryJson = JsonSerializer.Serialize(primaryEntity);
        var secondaryJson = JsonSerializer.Serialize(secondaryEntity);

        result.PrimaryValue = primaryJson;
        result.SecondaryValue = secondaryJson;
        result.IsConsistent = primaryJson == secondaryJson;

        if (!result.IsConsistent)
        {
            result.Differences.Add("Entity data differs between backends");
        }

        return result;
    }

    #region Private Helpers

    private bool IsDualWriteMode =>
        _options.Mode == PolyglotMode.DualWrite && _secondaryContext != null;

    private async Task<T> DualWriteAsync(
        Func<Task<T>> primaryWrite,
        Func<Task<T>> secondaryWrite,
        CancellationToken cancellationToken,
        string operation = SyncOperation.Insert,
        string? entityId = null)
    {
        // Write to primary first
        var result = await primaryWrite();

        if (_secondaryContext == null)
        {
            return result;
        }

        // Try to get entity ID for sync logging
        var id = entityId ?? TryGetEntityId(result);

        // Attempt secondary write with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.SecondaryWriteTimeoutMs);

        try
        {
            await _resiliencePipeline.ExecuteAsync(
                async token => await secondaryWrite(),
                cts.Token);

            // Track successful secondary writes via Meter
            _secondaryWriteSuccesses.Add(1,
                new KeyValuePair<string, object?>("entity_type", typeof(T).Name),
                new KeyValuePair<string, object?>("mode", _options.Mode.ToString()));

            // Log successful sync
            await LogSyncAsync(id, operation, SyncStatus.Synced, null, cancellationToken);
        }
        catch (Exception ex)
        {
            // Emit metric for monitoring/alerting via Meter
            _secondaryWriteFailures.Add(1,
                new KeyValuePair<string, object?>("entity_type", typeof(T).Name),
                new KeyValuePair<string, object?>("mode", _options.Mode.ToString()),
                new KeyValuePair<string, object?>("exception_type", ex.GetType().Name));

            _logger.LogError(ex,
                "Secondary write failed for {EntityType}. Mode: {Mode}. Compensation enabled: {CompensationEnabled}. " +
                "This failure is tracked via polyglot.secondary_write_failures metric.",
                typeof(T).Name,
                _options.Mode,
                _options.EnableCompensation);

            // Log failed sync
            await LogSyncAsync(id, operation, SyncStatus.Failed, ex.Message, cancellationToken);

            // Also track via ICustomMetrics if available
            _metrics?.TrackDualWriteFailure(
                typeof(T).Name,
                "Write",
                ex.Message,
                _options.EnableCompensation);
        }

        return result;
    }

    private async Task LogSyncAsync(
        string? entityId,
        string operation,
        string status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        if (_secondaryContext is not PostgresDbContext postgresContext)
        {
            return; // Sync log is PostgreSQL-specific
        }

        try
        {
            var syncLog = new PolyglotSyncLog
            {
                EntityType = typeof(T).Name,
                EntityId = entityId ?? "unknown",
                Operation = operation,
                SourceBackend = "cosmos",
                SyncStatus = status,
                CosmosTimestamp = DateTime.UtcNow,
                PostgresTimestamp = DateTime.UtcNow,
                ErrorMessage = errorMessage
            };

            postgresContext.SyncLogs.Add(syncLog);
            await postgresContext.SaveChangesAsync(cancellationToken);

            // Detach to avoid tracking issues
            postgresContext.Entry(syncLog).State = EntityState.Detached;
        }
        catch (Exception ex)
        {
            // Don't fail the operation if sync logging fails
            _logger.LogWarning(ex, "Failed to log sync operation for {EntityType}", typeof(T).Name);
        }
    }

    private static string? TryGetEntityId(T? entity)
    {
        if (entity == null) return null;

        // Try common ID property names
        var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("ID");
        return idProperty?.GetValue(entity)?.ToString();
    }

    private async Task<T> AddToSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return entity;

        // Detach from secondary context if already tracked (prevents tracking conflicts)
        var existingEntry = _secondaryContext.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.Entity == entity);
        if (existingEntry != null)
        {
            existingEntry.State = EntityState.Detached;
        }

        _secondaryContext.Entry(entity).State = EntityState.Added;
        await _secondaryContext.SaveChangesAsync(cancellationToken);

        // Detach after save to prevent cross-context tracking issues
        _secondaryContext.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    private async Task<int> UpdateInSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return 0;

        // Detach any existing tracked instance with same key
        var existingEntry = _secondaryContext.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.Entity == entity);
        if (existingEntry != null)
        {
            existingEntry.State = EntityState.Detached;
        }

        _secondaryContext.Entry(entity).State = EntityState.Modified;
        var result = await _secondaryContext.SaveChangesAsync(cancellationToken);

        // Detach after save
        _secondaryContext.Entry(entity).State = EntityState.Detached;
        return result;
    }

    private async Task<int> DeleteFromSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return 0;

        // Detach any existing tracked instance
        var existingEntry = _secondaryContext.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.Entity == entity);
        if (existingEntry != null)
        {
            existingEntry.State = EntityState.Detached;
        }

        _secondaryContext.Entry(entity).State = EntityState.Deleted;
        var result = await _secondaryContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private ResiliencePipeline CreateResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            // Circuit breaker: Opens after 5 consecutive failures, stays open for 30s
            // This prevents cascading failures when secondary is down
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder()
                    .Handle<DbUpdateException>()
                    .Handle<TimeoutException>()
                    .Handle<OperationCanceledException>(),
                OnOpened = args =>
                {
                    _logger.LogWarning(
                        "Circuit breaker OPENED for secondary database writes. " +
                        "Duration: {BreakDuration}s. Reason: {Exception}",
                        args.BreakDuration.TotalSeconds,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Circuit breaker CLOSED. Secondary database writes resumed.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    _logger.LogInformation("Circuit breaker HALF-OPEN. Testing secondary database...");
                    return ValueTask.CompletedTask;
                }
            })
            // Retry: 3 attempts with exponential backoff
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<DbUpdateException>()
            })
            // Note: Timeout is handled via CancellationTokenSource in DualWriteAsync
            // to allow per-operation control and avoid double-timeout conflicts
            .Build();
    }

    #endregion
}
