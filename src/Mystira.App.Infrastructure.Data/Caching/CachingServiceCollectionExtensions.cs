using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mystira.App.Infrastructure.Data.Caching;

/// <summary>
/// Extension methods for registering caching services.
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// Add Redis distributed caching with configuration from appsettings.
    /// </summary>
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure cache options
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = new CacheOptions();
        configuration.GetSection(CacheOptions.SectionName).Bind(cacheOptions);

        if (!cacheOptions.Enabled || string.IsNullOrEmpty(cacheOptions.ConnectionString))
        {
            // Use in-memory cache as fallback
            services.AddDistributedMemoryCache();
            return services;
        }

        // Configure Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = cacheOptions.ConnectionString;
            options.InstanceName = cacheOptions.InstanceName;
        });

        // Add health check (requires AspNetCore.HealthChecks.Redis package)
        // TODO: Add package reference: AspNetCore.HealthChecks.Redis
        // services.AddHealthChecks()
        //     .AddRedis(
        //         cacheOptions.ConnectionString,
        //         name: "redis",
        //         failureStatus: HealthStatus.Degraded,
        //         tags: new[] { "cache", "redis" });

        return services;
    }

    /// <summary>
    /// Decorate a repository with caching.
    /// Uses Scrutor-style decoration pattern.
    /// </summary>
    /// <example>
    /// services.AddCachedRepository{Account}();
    /// </example>
    public static IServiceCollection AddCachedRepository<TEntity>(
        this IServiceCollection services)
        where TEntity : class
    {
        // Register the cached decorator
        // This requires the base ISpecRepository to be registered first
        services.Decorate<Application.Ports.Data.ISpecRepository<TEntity>, CachedRepository<TEntity>>();
        return services;
    }
}

/// <summary>
/// Simple decorator extension (if Scrutor is not available).
/// </summary>
public static class DecoratorExtensions
{
    /// <summary>
    /// Decorate a service with a decorator implementation.
    /// </summary>
    public static IServiceCollection Decorate<TInterface, TDecorator>(this IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var wrappedDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface));

        if (wrappedDescriptor == null)
        {
            throw new InvalidOperationException($"Service of type {typeof(TInterface).Name} not registered");
        }

        var objectFactory = ActivatorUtilities.CreateFactory(
            typeof(TDecorator),
            new[] { typeof(TInterface) });

        // Remove the original descriptor to avoid ambiguity
        services.Remove(wrappedDescriptor);

        // Add the decorated descriptor
        services.Add(ServiceDescriptor.Describe(
            typeof(TInterface),
            sp => (TInterface)objectFactory(sp, new[] { CreateInstance(sp, wrappedDescriptor) }),
            wrappedDescriptor.Lifetime));

        return services;
    }

    private static object CreateInstance(IServiceProvider services, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
        {
            return descriptor.ImplementationInstance;
        }

        if (descriptor.ImplementationFactory != null)
        {
            return descriptor.ImplementationFactory(services);
        }

        return ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType!);
    }
}
