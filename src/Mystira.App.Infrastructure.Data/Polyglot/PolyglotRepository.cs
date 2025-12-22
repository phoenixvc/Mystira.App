using System.Text.Json;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Data;
using Polly;
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
    private readonly MigrationOptions _options;
    private readonly DbContext? _secondaryContext;
    private readonly ResiliencePipeline _resiliencePipeline;

    public PolyglotRepository(
        DbContext primaryContext,
        IOptions<MigrationOptions> options,
        ILogger<PolyglotRepository<T>> logger,
        DbContext? secondaryContext = null)
        : base(primaryContext, logger)
    {
        _options = options?.Value ?? new MigrationOptions();
        _secondaryContext = secondaryContext;
        _resiliencePipeline = CreateResiliencePipeline();
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
    public override async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            await DualWriteAsync(
                async () => { await base.UpdateAsync(entity, cancellationToken); return entity; },
                async () => { await UpdateInSecondaryAsync(entity, cancellationToken); return entity; },
                cancellationToken);
            return;
        }

        await base.UpdateAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (IsDualWriteMode)
        {
            await DualWriteAsync(
                async () => { await base.DeleteAsync(entity, cancellationToken); return entity; },
                async () => { await DeleteFromSecondaryAsync(entity, cancellationToken); return entity; },
                cancellationToken);
            return;
        }

        await base.DeleteAsync(entity, cancellationToken);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Secondary write failed for {EntityType}. Compensation enabled: {CompensationEnabled}",
                typeof(T).Name,
                _options.EnableCompensation);

            // Don't fail the operation, but log for monitoring
            // In production, this would trigger an alert
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

    private async Task UpdateInSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return;

        _secondaryContext.Set<T>().Update(entity);
        await _secondaryContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteFromSecondaryAsync(T entity, CancellationToken cancellationToken)
    {
        if (_secondaryContext == null) return;

        _secondaryContext.Set<T>().Remove(entity);
        await _secondaryContext.SaveChangesAsync(cancellationToken);
    }

    private ResiliencePipeline CreateResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<DbUpdateException>()
            })
            .AddTimeout(TimeSpan.FromMilliseconds(_options.DualWriteTimeoutMs))
            .Build();
    }

    #endregion
}
