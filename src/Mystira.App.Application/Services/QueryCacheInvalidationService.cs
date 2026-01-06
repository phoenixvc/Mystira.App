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
}

public class QueryCacheInvalidationService : IQueryCacheInvalidationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QueryCacheInvalidationService> _logger;
    private static readonly HashSet<string> _cacheKeys = new();
    private static readonly object _lock = new();

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
    internal static void TrackCacheKey(string cacheKey)
    {
        lock (_lock)
        {
            _cacheKeys.Add(cacheKey);
        }
    }
}
