namespace Mystira.App.Infrastructure.Data.Caching;

/// <summary>
/// Configuration options for Redis caching.
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Enable/disable caching globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Redis connection string.
    /// Example: "localhost:6379" or "mystira-cache.redis.cache.windows.net:6380,password=xxx,ssl=True"
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Default cache entry sliding expiration in minutes.
    /// </summary>
    public int DefaultSlidingExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Default cache entry absolute expiration in minutes.
    /// </summary>
    public int DefaultAbsoluteExpirationMinutes { get; set; } = 120;

    /// <summary>
    /// Cache key prefix for all entries.
    /// Useful for multi-tenant or environment isolation.
    /// </summary>
    public string KeyPrefix { get; set; } = "mystira:";

    /// <summary>
    /// Instance name for Redis.
    /// </summary>
    public string InstanceName { get; set; } = "mystira-app";

    /// <summary>
    /// Enable cache-through writes (update cache on write).
    /// </summary>
    public bool EnableWriteThrough { get; set; } = true;

    /// <summary>
    /// Enable cache invalidation on entity changes.
    /// </summary>
    public bool EnableInvalidationOnChange { get; set; } = true;
}
