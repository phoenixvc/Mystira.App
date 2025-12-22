using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mystira.App.Infrastructure.Data.Caching;

/// <summary>
/// Health check for Redis cache connectivity.
/// Tests both read and write operations.
/// </summary>
public class RedisCacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisCacheHealthCheck> _logger;
    private const string HealthCheckKey = "__health_check__";

    public RedisCacheHealthCheck(
        IDistributedCache cache,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheHealthCheck> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? new CacheOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return HealthCheckResult.Healthy("Caching is disabled");
        }

        try
        {
            var testKey = $"{_options.KeyPrefix}{HealthCheckKey}";
            var testValue = DateTime.UtcNow.ToString("O");

            // Test write
            await _cache.SetStringAsync(
                testKey,
                testValue,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                },
                cancellationToken);

            // Test read
            var retrieved = await _cache.GetStringAsync(testKey, cancellationToken);

            if (retrieved != testValue)
            {
                return HealthCheckResult.Degraded(
                    "Cache read/write mismatch",
                    data: new Dictionary<string, object>
                    {
                        { "expected", testValue },
                        { "actual", retrieved ?? "null" }
                    });
            }

            // Cleanup
            await _cache.RemoveAsync(testKey, cancellationToken);

            return HealthCheckResult.Healthy(
                "Redis cache is healthy",
                data: new Dictionary<string, object>
                {
                    { "instance", _options.InstanceName },
                    { "keyPrefix", _options.KeyPrefix }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");

            return HealthCheckResult.Unhealthy(
                "Redis cache is unhealthy",
                ex,
                data: new Dictionary<string, object>
                {
                    { "instance", _options.InstanceName },
                    { "error", ex.Message }
                });
        }
    }
}
