using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Services;
using Wolverine;

namespace Mystira.App.Application.Behaviors;

/// <summary>
/// Wolverine middleware that caches query results for queries implementing ICacheableQuery.
/// Uses in-memory caching with configurable expiration per query.
/// This is invoked via Wolverine's handler chain middleware.
/// </summary>
public class QueryCachingMiddleware
{
    private readonly IMemoryCache _cache;
    private readonly IQueryCacheInvalidationService _cacheInvalidation;
    private readonly ILogger<QueryCachingMiddleware> _logger;

    public QueryCachingMiddleware(
        IMemoryCache cache,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger<QueryCachingMiddleware> logger)
    {
        _cache = cache;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    /// <summary>
    /// Wolverine middleware method that wraps handler execution with caching logic.
    /// Called Before in the handler chain for cacheable queries.
    /// </summary>
    public async Task<T?> TryGetFromCache<T>(ICacheableQuery query)
    {
        var cacheKey = query.CacheKey;

        if (_cache.TryGetValue(cacheKey, out T? cachedResponse) && cachedResponse != null)
        {
            _logger.LogDebug("Cache hit for query with key {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for query with key {CacheKey}", cacheKey);
        return default;
    }

    /// <summary>
    /// Caches the response after handler execution.
    /// Called After in the handler chain for cacheable queries.
    /// </summary>
    public void CacheResponse<T>(ICacheableQuery query, T response)
    {
        var cacheKey = query.CacheKey;

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(query.CacheDurationSeconds),
            Size = 1 // For size-limited caches
        };

        _cache.Set(cacheKey, response, cacheOptions);

        // Track cache key for prefix-based invalidation
        _cacheInvalidation.TrackCacheKey(cacheKey);

        _logger.LogDebug("Cached query with key {CacheKey} for {Duration} seconds",
            cacheKey, query.CacheDurationSeconds);
    }
}

/// <summary>
/// Static helper methods for query caching that can be used directly in handlers.
/// Alternative to middleware when direct control is needed.
/// </summary>
public static class QueryCacheHelper
{
    /// <summary>
    /// Executes a query with caching support.
    /// Use this in handlers for cacheable queries.
    /// </summary>
    public static async Task<T> ExecuteWithCache<T>(
        ICacheableQuery query,
        IMemoryCache cache,
        IQueryCacheInvalidationService cacheInvalidation,
        ILogger logger,
        Func<Task<T>> executeQuery)
    {
        var cacheKey = query.CacheKey;

        // Try to get from cache
        if (cache.TryGetValue(cacheKey, out T? cachedResponse) && cachedResponse != null)
        {
            logger.LogDebug("Cache hit for query with key {CacheKey}", cacheKey);
            return cachedResponse;
        }

        logger.LogDebug("Cache miss for query with key {CacheKey}", cacheKey);

        // Execute query
        var response = await executeQuery();

        // Cache the response
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(query.CacheDurationSeconds),
            Size = 1 // For size-limited caches
        };

        cache.Set(cacheKey, response, cacheOptions);

        // Track cache key for prefix-based invalidation
        cacheInvalidation.TrackCacheKey(cacheKey);

        logger.LogDebug("Cached query with key {CacheKey} for {Duration} seconds",
            cacheKey, query.CacheDurationSeconds);

        return response;
    }
}
