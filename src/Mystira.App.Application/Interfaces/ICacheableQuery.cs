namespace Mystira.App.Application.Interfaces;

/// <summary>
/// Marker interface for queries that should be cached.
/// Implement this interface on queries where caching is appropriate.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Gets the cache key for this query.
    /// Should be unique based on query parameters.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache duration in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    int CacheDurationSeconds => 300;
}
