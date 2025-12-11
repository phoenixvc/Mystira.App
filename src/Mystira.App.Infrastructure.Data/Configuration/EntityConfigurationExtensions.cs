using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mystira.App.Infrastructure.Data.Configuration;

/// <summary>
/// Extension methods to reduce duplication in DbContext OnModelCreating.
/// Consolidates common patterns for List&lt;string&gt; conversions and Cosmos DB container configuration.
/// </summary>
public static class EntityConfigurationExtensions
{
    /// <summary>
    /// Standard value comparer for List&lt;string&gt; properties.
    /// Reused across all entity configurations to ensure consistent behavior.
    /// </summary>
    public static readonly ValueComparer<List<string>> StringListComparer = new(
        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
        c => c.ToList()
    );

    /// <summary>
    /// Configures a List&lt;string&gt; property with comma-separated string conversion.
    /// DRY pattern extracted from 10+ occurrences in MystiraAppDbContext.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="propertyBuilder">The property builder</param>
    /// <returns>The property builder for chaining</returns>
    public static PropertyBuilder<List<string>> HasStringListConversion<TEntity>(
        this PropertyBuilder<List<string>> propertyBuilder)
        where TEntity : class
    {
        return propertyBuilder
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(StringListComparer)
            .GetProperty()
            .Builder as PropertyBuilder<List<string>> ?? propertyBuilder;
    }

    /// <summary>
    /// Configures standard Cosmos DB container settings for an entity.
    /// Applies only when not using in-memory database (for testing compatibility).
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="containerName">The Cosmos DB container name</param>
    /// <param name="isInMemoryDatabase">Whether using in-memory database</param>
    /// <returns>The entity type builder for chaining</returns>
    public static EntityTypeBuilder<TEntity> ConfigureCosmosContainer<TEntity>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        string containerName,
        bool isInMemoryDatabase)
        where TEntity : class
    {
        if (!isInMemoryDatabase)
        {
            // Map Id property to lowercase 'id' to match container partition key path /id
            entityBuilder.Property("Id").ToJsonProperty("id");

            // Configure container with standard partition key
            entityBuilder.ToContainer(containerName)
                         .HasPartitionKey("Id");
        }

        return entityBuilder;
    }

    /// <summary>
    /// Configures Cosmos DB container with a custom partition key.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entityBuilder">The entity type builder</param>
    /// <param name="containerName">The Cosmos DB container name</param>
    /// <param name="partitionKeyPropertyName">The property to use as partition key</param>
    /// <param name="isInMemoryDatabase">Whether using in-memory database</param>
    /// <returns>The entity type builder for chaining</returns>
    public static EntityTypeBuilder<TEntity> ConfigureCosmosContainerWithPartitionKey<TEntity>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        string containerName,
        string partitionKeyPropertyName,
        bool isInMemoryDatabase)
        where TEntity : class
    {
        if (!isInMemoryDatabase)
        {
            // Map Id property to lowercase 'id'
            entityBuilder.Property("Id").ToJsonProperty("id");

            // Map partition key to lowercase for Cosmos DB
            entityBuilder.Property(partitionKeyPropertyName)
                         .ToJsonProperty(partitionKeyPropertyName.ToLowerInvariant());

            // Configure container with custom partition key
            entityBuilder.ToContainer(containerName)
                         .HasPartitionKey(partitionKeyPropertyName);
        }

        return entityBuilder;
    }

    /// <summary>
    /// Creates a standard ValueComparer for List&lt;T&gt; with custom hash code computation.
    /// </summary>
    /// <typeparam name="T">The list element type</typeparam>
    /// <returns>A ValueComparer instance</returns>
    public static ValueComparer<List<T>> CreateListComparer<T>()
    {
        return new ValueComparer<List<T>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v?.GetHashCode() ?? 0)),
            c => c.ToList()
        );
    }
}
