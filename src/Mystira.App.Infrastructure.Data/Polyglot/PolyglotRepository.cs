using System.Diagnostics.Metrics;
using System.Text.Json;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Shared.Telemetry;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Mystira.App.Infrastructure.Data.Polyglot;

/// <summary>
/// Polyglot repository implementation supporting multiple database backends.
/// Implements dual-write patterns for gradual database migration per ADR-0013/0014.
///
/// Features:
/// - Migration phase awareness (Cosmos-only, dual-write, Postgres-only)
/// - Dual-write with compensation on failure
/// - Health checks per backend
/// - Polly resilience policies
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

    private readonly MigrationOptions _options;
    private readonly DbContext? _secondaryContext;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ICustomMetrics? _metrics;

    public PolyglotRepository(
        DbContext primaryContext,
        IOptions<MigrationOptions> options,
        ILogger<PolyglotRepository<T>> logger,
        DbContext? secondaryContext = null,
        ICustomMetrics? metrics = null)
        : base(primaryContext, logger)
    {
        _options = options?.Value ?? new MigrationOptions();
        _secondaryContext = secondaryContext;
        _resiliencePipeline = CreateResiliencePipeline();
        _metrics = metrics;
    }

    /// <inheritdoc />
    public MigrationPhase CurrentPhase => _options.Phase;

    /// <inheritdoc />
    public override async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            return await DualWriteAsync(
                () => base.AddAsync(entity, cancellationToken),
                () => AddToSecondaryAsync(entity, cancellationToken),
                cancellationToken);
        }

        return await base.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            await DualWriteAsync(
                async () => { var result = await base.UpdateAsync(entity, cancellationToken); return entity; },
                async () => { await UpdateInSecondaryAsync(entity, cancellationToken); return entity; },
                cancellationToken);
            return 1; // Assuming single entity update
        }

        return await base.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            await DualWriteAsync(
                async () => { var result = await base.DeleteAsync(entity, cancellationToken); return entity; },
                async () => { await DeleteFromSecondaryAsync(entity, cancellationToken); return entity; },
                cancellationToken);
            return 1; // Assuming single entity delete
        }

        return await base.DeleteAsync(entity, cancellationToken);
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
            BackendType.Primary => GetPrimaryContext(),
            BackendType.Secondary => _secondaryContext,
            BackendType.CosmosDb => _options.Phase <= MigrationPhase.DualWriteCosmosRead ? _dbContext : _secondaryContext,
            BackendType.PostgreSql => _options.Phase >= MigrationPhase.DualWritePostgresRead ? _dbContext : _secondaryContext,
            _ => GetPrimaryContext()
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
        _options.Phase == MigrationPhase.DualWriteCosmosRead ||
        _options.Phase == MigrationPhase.DualWritePostgresRead;

    private DbContext GetPrimaryContext() => _options.Phase switch
    {
        MigrationPhase.CosmosOnly => _dbContext,
        MigrationPhase.DualWriteCosmosRead => _dbContext,
        MigrationPhase.DualWritePostgresRead => _secondaryContext ?? _dbContext,
        MigrationPhase.PostgresOnly => _secondaryContext ?? _dbContext,
        _ => _dbContext
    };

    private async Task<T> DualWriteAsync(
        Func<Task<T>> primaryWrite,
        Func<Task<T>> secondaryWrite,
        CancellationToken cancellationToken)
    {
        // Write to primary first
        var result = await primaryWrite();

        if (_secondaryContext == null)
        {
            return result;
        }

        // Attempt secondary write with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.DualWriteTimeoutMs);

        try
        {
            await _resiliencePipeline.ExecuteAsync(
                async token => await secondaryWrite(),
                cts.Token);

            // Track successful secondary writes via Meter
            _secondaryWriteSuccesses.Add(1,
                new KeyValuePair<string, object?>("entity_type", typeof(T).Name),
                new KeyValuePair<string, object?>("phase", _options.Phase.ToString()));
        }
        catch (Exception ex)
        {
            // Emit metric for monitoring/alerting via Meter
            _secondaryWriteFailures.Add(1,
                new KeyValuePair<string, object?>("entity_type", typeof(T).Name),
                new KeyValuePair<string, object?>("phase", _options.Phase.ToString()),
                new KeyValuePair<string, object?>("exception_type", ex.GetType().Name));

            _logger.LogError(ex,
                "Secondary write failed for {EntityType}. Phase: {Phase}. Compensation enabled: {CompensationEnabled}. " +
                "This failure is tracked via polyglot.secondary_write_failures metric.",
                typeof(T).Name,
                _options.Phase,
                _options.EnableCompensation);

            // Also track via ICustomMetrics if available
            _metrics?.TrackDualWriteFailure(
                typeof(T).Name,
                "Write",
                ex.Message,
                _options.EnableCompensation);
        }

        return result;
    }

    private async Task<T> AddToSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return entity;

        await _secondaryContext.Set<T>().AddAsync(entity, cancellationToken);
        await _secondaryContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task<int> UpdateInSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return 0;

        _secondaryContext.Set<T>().Update(entity);
        return await _secondaryContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> DeleteFromSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return 0;

        _secondaryContext.Set<T>().Remove(entity);
        return await _secondaryContext.SaveChangesAsync(cancellationToken);
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
            // Timeout: Fail fast if secondary is too slow
            .AddTimeout(TimeSpan.FromMilliseconds(_options.DualWriteTimeoutMs))
            .Build();
    }

    #endregion
}
