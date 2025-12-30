using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mystira.Domain.Models;
using Mystira.Shared.Polyglot;

namespace Mystira.App.Infrastructure.Data.Polyglot;

/// <summary>
/// Service for backfilling data from Cosmos DB (primary) to PostgreSQL (secondary).
/// Used when enabling DualWrite mode to sync existing data.
/// </summary>
public interface IPolyglotBackfillService
{
    /// <summary>
    /// Backfill all accounts from Cosmos to PostgreSQL
    /// </summary>
    Task<BackfillResult> BackfillAccountsAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Backfill all game sessions from Cosmos to PostgreSQL
    /// </summary>
    Task<BackfillResult> BackfillGameSessionsAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Backfill all player scenario scores from Cosmos to PostgreSQL
    /// </summary>
    Task<BackfillResult> BackfillPlayerScenarioScoresAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Backfill all entity types
    /// </summary>
    Task<BackfillSummary> BackfillAllAsync(int batchSize = 100, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a backfill operation for a single entity type
/// </summary>
public class BackfillResult
{
    public string EntityType { get; set; } = string.Empty;
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int SkippedCount { get; set; } // Already existed in secondary
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Summary of backfill operation for all entity types
/// </summary>
public class BackfillSummary
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<BackfillResult> Results { get; set; } = new();
    public bool IsSuccess => Results.All(r => r.FailureCount == 0);
}

/// <summary>
/// Implementation of polyglot backfill service
/// </summary>
public class PolyglotBackfillService : IPolyglotBackfillService
{
    private readonly MystiraAppDbContext _cosmosContext;
    private readonly PostgresDbContext _postgresContext;
    private readonly ILogger<PolyglotBackfillService> _logger;

    public PolyglotBackfillService(
        MystiraAppDbContext cosmosContext,
        PostgresDbContext postgresContext,
        ILogger<PolyglotBackfillService> logger)
    {
        _cosmosContext = cosmosContext;
        _postgresContext = postgresContext;
        _logger = logger;
    }

    public async Task<BackfillResult> BackfillAccountsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await BackfillEntityAsync<Account>(
            _cosmosContext.Accounts,
            _postgresContext.Accounts,
            batchSize,
            cancellationToken);
    }

    public async Task<BackfillResult> BackfillGameSessionsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await BackfillEntityAsync<GameSession>(
            _cosmosContext.GameSessions,
            _postgresContext.GameSessions,
            batchSize,
            cancellationToken);
    }

    public async Task<BackfillResult> BackfillPlayerScenarioScoresAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await BackfillEntityAsync<PlayerScenarioScore>(
            _cosmosContext.PlayerScenarioScores,
            _postgresContext.PlayerScenarioScores,
            batchSize,
            cancellationToken);
    }

    public async Task<BackfillSummary> BackfillAllAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var summary = new BackfillSummary
        {
            StartedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Starting polyglot backfill for all entities. BatchSize: {BatchSize}", batchSize);

        summary.Results.Add(await BackfillAccountsAsync(batchSize, cancellationToken));
        summary.Results.Add(await BackfillGameSessionsAsync(batchSize, cancellationToken));
        summary.Results.Add(await BackfillPlayerScenarioScoresAsync(batchSize, cancellationToken));

        summary.CompletedAt = DateTime.UtcNow;
        summary.TotalDuration = summary.CompletedAt - summary.StartedAt;

        _logger.LogInformation(
            "Polyglot backfill completed. Duration: {Duration}. Success: {Success}",
            summary.TotalDuration,
            summary.IsSuccess);

        return summary;
    }

    private async Task<BackfillResult> BackfillEntityAsync<T>(
        DbSet<T> sourceSet,
        DbSet<T> targetSet,
        int batchSize,
        CancellationToken cancellationToken) where T : class
    {
        var result = new BackfillResult
        {
            EntityType = typeof(T).Name
        };

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting backfill for {EntityType}", typeof(T).Name);

            // Get all IDs from source
            var sourceEntities = await sourceSet.AsNoTracking().ToListAsync(cancellationToken);
            result.TotalProcessed = sourceEntities.Count;

            _logger.LogInformation("Found {Count} {EntityType} entities in Cosmos", sourceEntities.Count, typeof(T).Name);

            // Process in batches
            foreach (var batch in sourceEntities.Chunk(batchSize))
            {
                foreach (var entity in batch)
                {
                    try
                    {
                        // Check if already exists in target using the Id property
                        var idProperty = typeof(T).GetProperty("Id");
                        if (idProperty == null)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Entity type {typeof(T).Name} does not have an Id property");
                            continue;
                        }

                        var entityId = idProperty.GetValue(entity)?.ToString();
                        if (string.IsNullOrEmpty(entityId))
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Entity has null or empty Id");
                            continue;
                        }

                        var exists = await targetSet.FindAsync(new object[] { entityId }, cancellationToken);
                        if (exists != null)
                        {
                            result.SkippedCount++;
                            _postgresContext.Entry(exists).State = EntityState.Detached;
                            continue;
                        }

                        // Add to target
                        targetSet.Add(entity);
                        await _postgresContext.SaveChangesAsync(cancellationToken);
                        _postgresContext.Entry(entity).State = EntityState.Detached;

                        result.SuccessCount++;

                        // Log sync to audit table
                        await LogBackfillSync(typeof(T).Name, entityId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Failed to backfill entity: {ex.Message}");
                        _logger.LogError(ex, "Failed to backfill {EntityType} entity", typeof(T).Name);
                    }
                }

                _logger.LogDebug(
                    "Processed batch for {EntityType}. Success: {Success}, Skipped: {Skipped}, Failed: {Failed}",
                    typeof(T).Name,
                    result.SuccessCount,
                    result.SkippedCount,
                    result.FailureCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill failed for {EntityType}", typeof(T).Name);
            result.Errors.Add($"Backfill failed: {ex.Message}");
        }

        result.Duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Backfill completed for {EntityType}. Total: {Total}, Success: {Success}, Skipped: {Skipped}, Failed: {Failed}, Duration: {Duration}",
            typeof(T).Name,
            result.TotalProcessed,
            result.SuccessCount,
            result.SkippedCount,
            result.FailureCount,
            result.Duration);

        return result;
    }

    private async Task LogBackfillSync(string entityType, string entityId, CancellationToken cancellationToken)
    {
        try
        {
            var syncLog = new PolyglotSyncLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Operation = "BACKFILL",
                SourceBackend = "cosmos",
                SyncStatus = SyncStatus.Synced,
                CosmosTimestamp = DateTime.UtcNow,
                PostgresTimestamp = DateTime.UtcNow
            };

            _postgresContext.SyncLogs.Add(syncLog);
            await _postgresContext.SaveChangesAsync(cancellationToken);
            _postgresContext.Entry(syncLog).State = EntityState.Detached;
        }
        catch
        {
            // Don't fail the backfill if logging fails
        }
    }
}
