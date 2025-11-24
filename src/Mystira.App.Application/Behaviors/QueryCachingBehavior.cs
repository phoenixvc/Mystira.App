using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.Behaviors;

/// <summary>
/// Pipeline behavior that caches query results for queries implementing ICacheableQuery.
/// Uses in-memory caching with configurable expiration per query.
/// </summary>
/// <typeparam name="TRequest">The request type (query)</typeparam>
/// <typeparam name="TResponse">The response type (query result)</typeparam>
public class QueryCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger;

    public QueryCachingBehavior(
        IMemoryCache cache,
        ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache if request implements ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        var cacheKey = cacheableQuery.CacheKey;

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogDebug("Cache hit for query {QueryType} with key {CacheKey}",
                typeof(TRequest).Name, cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for query {QueryType} with key {CacheKey}",
            typeof(TRequest).Name, cacheKey);

        // Execute query
        var response = await next();

        // Cache the response
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheableQuery.CacheDurationSeconds),
            Size = 1 // For size-limited caches
        };

        _cache.Set(cacheKey, response, cacheOptions);

        // Track cache key for prefix-based invalidation
        Services.QueryCacheInvalidationService.TrackCacheKey(cacheKey);

        _logger.LogDebug("Cached query {QueryType} with key {CacheKey} for {Duration} seconds",
            typeof(TRequest).Name, cacheKey, cacheableQuery.CacheDurationSeconds);

        return response;
    }
}
