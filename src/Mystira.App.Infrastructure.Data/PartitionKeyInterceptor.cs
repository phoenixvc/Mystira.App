using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mystira.App.Infrastructure.Data;

/// <summary>
/// Interceptor to sync shadow properties for partition keys in Cosmos DB
/// Ensures JSON documents have both 'id' (document ID) and 'Id' (partition key) properties
/// </summary>
public class PartitionKeyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is MystiraAppDbContext dbContext)
        {
            SyncPartitionKeyIds(dbContext);
        }
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is MystiraAppDbContext dbContext)
        {
            SyncPartitionKeyIds(dbContext);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SyncPartitionKeyIds(MystiraAppDbContext dbContext)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                // Skip owned/value types; they don't have their own identity/partition key
                var entityType = entry.Metadata;
                if (entityType.IsOwned())
                {
                    continue;
                }

                // Check if entity has both Id property and PartitionKeyId shadow property
                var idTypeProperty = entityType.FindProperty("Id");
                var partitionKeyIdProperty = entityType.FindProperty("PartitionKeyId");

                if (idTypeProperty != null && partitionKeyIdProperty != null)
                {
                    var idValue = entry.Property("Id").CurrentValue?.ToString();
                    if (!string.IsNullOrEmpty(idValue))
                    {
                        entry.Property("PartitionKeyId").CurrentValue = idValue;
                    }
                }

                // Special handling for CompassTracking which uses Axis as key
                // Sync the PartitionKeyAxis shadow property with Axis
                if (entry.Entity.GetType().Name == "CompassTracking")
                {
                    var axisTypeProperty = entityType.FindProperty("Axis");
                    var partitionKeyAxisProperty = entityType.FindProperty("PartitionKeyAxis");

                    if (axisTypeProperty != null && partitionKeyAxisProperty != null)
                    {
                        var axisValue = entry.Property("Axis").CurrentValue?.ToString();
                        if (!string.IsNullOrEmpty(axisValue))
                        {
                            entry.Property("PartitionKeyAxis").CurrentValue = axisValue;
                        }
                    }
                }
            }
        }
    }
}

