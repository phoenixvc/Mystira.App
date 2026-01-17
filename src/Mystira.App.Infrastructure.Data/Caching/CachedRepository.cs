using System.Text.Json;
using Ardalis.Specification;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mystira.App.Infrastructure.Data.Caching;

/// <summary>
/// Interface for entities with a string ID.
/// Implement this interface to enable reliable cache key generation.
/// </summary>
public interface IHasId
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// Repository decorator that adds caching using the cache-aside pattern.
/// Wraps an existing IRepositoryBase and adds Redis caching.
///
/// Cache-aside pattern:
/// - Read: Check cache first, if miss, read from DB and populate cache
/// - Write: Write to DB, then invalidate/update cache
///
/// Usage:
///   services.Decorate{IRepositoryBase{Account}, CachedRepository{Account}}();
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class CachedRepository<T> : IRepositoryBase<T> where T : class
{
    private readonly IRepositoryBase<T> _inner;
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<CachedRepository<T>> _logger;
    private readonly string _entityTypeName;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CachedRepository(
        IRepositoryBase<T> inner,
        IDistributedCache cache,
        IOptions<CacheOptions> options,
        ILogger<CachedRepository<T>> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? new CacheOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _entityTypeName = typeof(T).Name.ToLowerInvariant();
    }

    #region Read Operations (Cache-Aside)

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        // Convert TId to string for cache key
        var idString = id.ToString();
        if (string.IsNullOrEmpty(idString))
        {
            return await _inner.GetByIdAsync(id, cancellationToken);
        }

        if (!_options.Enabled)
        {
            return await _inner.GetByIdAsync(id, cancellationToken);
        }

        var cacheKey = GetCacheKey(idString);

        // Try to get from cache
        var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedValue != null)
        {
            _logger.LogDebug("Cache hit for {EntityType} with key {CacheKey}", _entityTypeName, cacheKey);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }

        // Cache miss - get from database
        _logger.LogDebug("Cache miss for {EntityType} with key {CacheKey}", _entityTypeName, cacheKey);
        var entity = await _inner.GetByIdAsync(id, cancellationToken);

        if (entity != null)
        {
            await SetCacheAsync(cacheKey, entity, cancellationToken);
        }

        return entity;
    }

    /// <inheritdoc />
    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.SingleOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.SingleOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        // List operations are not cached by default due to invalidation complexity
        return await _inner.ListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.ListAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.ListAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.CountAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _inner.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.AnyAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _inner.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
    {
        return _inner.AsAsyncEnumerable(specification);
    }

    #endregion

    #region Write Operations (Cache Invalidation)

    /// <inheritdoc />
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddAsync(entity, cancellationToken);

        if (_options.EnableWriteThrough)
        {
            await InvalidateOrUpdateCacheAsync(entity, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddRangeAsync(entities, cancellationToken);

        if (_options.EnableWriteThrough)
        {
            foreach (var entity in entities)
            {
                await InvalidateOrUpdateCacheAsync(entity, cancellationToken);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateAsync(entity, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            await InvalidateOrUpdateCacheAsync(entity, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _inner.UpdateRangeAsync(entities, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            foreach (var entity in entities)
            {
                await InvalidateOrUpdateCacheAsync(entity, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(entity, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            await InvalidateCacheAsync(entity, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        await _inner.DeleteRangeAsync(entityList, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            foreach (var entity in entityList)
            {
                await InvalidateCacheAsync(entity, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        // Get entities to delete for cache invalidation
        var entities = await _inner.ListAsync(specification, cancellationToken);

        await _inner.DeleteRangeAsync(specification, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            foreach (var entity in entities)
            {
                await InvalidateCacheAsync(entity, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _inner.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Private Helpers

    private string GetCacheKey(string id) => $"{_options.KeyPrefix}{_entityTypeName}:{id}";

    private string? GetEntityId(T entity)
    {
        // Strategy 1: Check if entity implements IHasId interface
        if (entity is IHasId hasId)
        {
            return hasId.Id;
        }

        // Strategy 2: Try common ID property names via reflection
        var idProperty = typeof(T).GetProperty("Id")
            ?? typeof(T).GetProperty($"{typeof(T).Name}Id")
            ?? typeof(T).GetProperty("Key");

        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            if (value != null)
            {
                return value.ToString();
            }
        }

        // Log warning if entity has no Id property
        _logger.LogWarning(
            "Unable to determine ID property for entity type {EntityType}. " +
            "Consider implementing IHasId interface or using a standard naming convention (Id, EntityTypeId, Key). " +
            "Cache operations will be skipped for this entity.",
            _entityTypeName);

        return null;
    }

    private async Task SetCacheAsync(string cacheKey, T entity, CancellationToken cancellationToken)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(entity, _jsonOptions);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_options.DefaultSlidingExpirationMinutes),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.DefaultAbsoluteExpirationMinutes)
            };

            await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
            _logger.LogDebug("Cached {EntityType} with key {CacheKey}", _entityTypeName, cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache {EntityType} with key {CacheKey}", _entityTypeName, cacheKey);
            // Don't fail the operation if caching fails
        }
    }

    private async Task InvalidateOrUpdateCacheAsync(T entity, CancellationToken cancellationToken)
    {
        var id = GetEntityId(entity);
        if (id == null) return;

        var cacheKey = GetCacheKey(id);

        if (_options.EnableWriteThrough)
        {
            // Update cache with new value
            await SetCacheAsync(cacheKey, entity, cancellationToken);
        }
        else
        {
            // Just invalidate
            await InvalidateCacheAsync(entity, cancellationToken);
        }
    }

    private async Task InvalidateCacheAsync(T entity, CancellationToken cancellationToken)
    {
        var id = GetEntityId(entity);
        if (id == null) return;

        var cacheKey = GetCacheKey(id);

        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogDebug("Invalidated cache for {EntityType} with key {CacheKey}", _entityTypeName, cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for {EntityType} with key {CacheKey}", _entityTypeName, cacheKey);
            // Don't fail the operation if cache invalidation fails
        }
    }

    #endregion
}
