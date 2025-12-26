using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Mystira.App.Application.Services;

/// <summary>
/// Service for invalidating query caches.
/// Use this after commands that modify data to ensure cache consistency.
/// </summary>
public interface IQueryCacheInvalidationService
{
    /// <summary>
    /// Removes a specific cache entry by key.
    /// </summary>
    void InvalidateCache(string cacheKey);

    /// <summary>
    /// Removes all cache entries matching a prefix.
    /// Example: InvalidateCacheByPrefix("Scenario") removes all scenario-related caches.
    /// </summary>
    void InvalidateCacheByPrefix(string prefix);

    /// <summary>
    /// Tracks a cache key for prefix-based invalidation.
    /// </summary>
    void TrackCacheKey(string cacheKey);

    /// <summary>
    /// Clears all tracked cache keys.
    /// </summary>
    void ClearTrackedKeys();
}

public class QueryCacheInvalidationService : IQueryCacheInvalidationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QueryCacheInvalidationService> _logger;
    private readonly HashSet<string> _cacheKeys = new();
    private readonly object _lock = new();

    public QueryCacheInvalidationService(
        IMemoryCache cache,
        ILogger<QueryCacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void InvalidateCache(string cacheKey)
    {
        _cache.Remove(cacheKey);

        lock (_lock)
        {
            _cacheKeys.Remove(cacheKey);
        }

        _logger.LogDebug("Invalidated cache entry: {CacheKey}", cacheKey);
    }

    public void InvalidateCacheByPrefix(string prefix)
    {
        HashSet<string> keysToRemove;

        lock (_lock)
        {
            keysToRemove = _cacheKeys
                .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToHashSet();
        }

        foreach (var key in keysToRemove)
        {
            InvalidateCache(key);
        }

        _logger.LogDebug("Invalidated {Count} cache entries with prefix: {Prefix}",
            keysToRemove.Count, prefix);
    }

    /// <summary>
    /// Internal method to track cache keys for prefix-based invalidation.
    /// Called by QueryCachingBehavior when caching items.
    /// </summary>
    public void TrackCacheKey(string cacheKey)
    {
        lock (_lock)
        {
            _cacheKeys.Add(cacheKey);
        }
    }

    /// <summary>
    /// Clears all tracked cache keys. Should only be used for testing.
    /// </summary>
    public void ClearTrackedKeys()
    {
        lock (_lock)
        {
            _cacheKeys.Clear();
        }
    }
}
