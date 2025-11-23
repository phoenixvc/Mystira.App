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
                // Check if entity has both Id property and PartitionKeyId shadow property
                var idProperty = entry.Property("Id");
                var partitionKeyIdProperty = entry.Metadata.FindProperty("PartitionKeyId");

                if (idProperty != null && partitionKeyIdProperty != null)
                {
                    var idValue = idProperty.CurrentValue?.ToString();
                    if (!string.IsNullOrEmpty(idValue))
                    {
                        entry.Property("PartitionKeyId").CurrentValue = idValue;
                    }
                }

                // Special handling for CompassTracking which uses Axis as key
                // Sync the PartitionKeyAxis shadow property with Axis
                if (entry.Entity.GetType().Name == "CompassTracking")
                {
                    var axisProperty = entry.Property("Axis");
                    var partitionKeyAxisProperty = entry.Metadata.FindProperty("PartitionKeyAxis");

                    if (axisProperty != null && partitionKeyAxisProperty != null)
                    {
                        var axisValue = axisProperty.CurrentValue?.ToString();
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

