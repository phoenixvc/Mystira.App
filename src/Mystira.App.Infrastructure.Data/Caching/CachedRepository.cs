using System.Text.Json;
using Ardalis.Specification;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Infrastructure.Data.Caching;

/// <summary>
/// Repository decorator that adds caching using the cache-aside pattern.
/// Wraps an existing ISpecRepository and adds Redis caching.
///
/// Cache-aside pattern:
/// - Read: Check cache first, if miss, read from DB and populate cache
/// - Write: Write to DB, then invalidate/update cache
///
/// Usage:
///   services.Decorate{ISpecRepository{Account}, CachedRepository{Account}}();
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class CachedRepository<T> : ISpecRepository<T> where T : class
{
    private readonly ISpecRepository<T> _inner;
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
        ISpecRepository<T> inner,
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
        if (!_options.Enabled)
        {
            return await _inner.GetByIdAsync(id, cancellationToken);
        }

        var cacheKey = GetCacheKey(id.ToString()!);

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
    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await _inner.GetByIdAsync(id, cancellationToken);
        }

        var cacheKey = GetCacheKey(id);

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

    public async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        return await GetByIdAsync(id?.ToString() ?? string.Empty, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> GetBySpecAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult?> GetBySpecAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await _inner.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await _inner.ExistsAsync(id, cancellationToken);
        }

        // Check cache first
        var cacheKey = GetCacheKey(id);
        var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedValue != null)
        {
            return true;
        }

        return await _inner.ExistsAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        // For single result specifications, try to use specification's cache key if available
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
    public async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateAsync(entity, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            await InvalidateOrUpdateCacheAsync(entity, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateRangeAsync(entities, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            foreach (var entity in entities)
            {
                await InvalidateOrUpdateCacheAsync(entity, cancellationToken);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeleteAsync(entity, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            await InvalidateCacheAsync(entity, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeleteRangeAsync(entities, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            foreach (var entity in entities)
            {
                await InvalidateCacheAsync(entity, cancellationToken);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        // Get entities to delete for cache invalidation
        var entities = await _inner.ListAsync(specification, cancellationToken);

        var result = await _inner.DeleteRangeAsync(specification, cancellationToken);

        if (_options.EnableInvalidationOnChange)
        {
            foreach (var entity in entities)
            {
                await InvalidateCacheAsync(entity, cancellationToken);
            }
        }

        return result;
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
        // Try to get Id property using reflection
        var idProperty = typeof(T).GetProperty("Id");
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
            "Entity type {EntityType} does not have an 'Id' property or it is null. " +
            "Cache operations will be skipped for this entity. " +
            "Consider implementing IEntity interface or using a different caching strategy for entities with composite keys.",
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
