using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Infrastructure.Data.Polyglot;

/// <summary>
/// Extension methods for registering polyglot persistence services.
/// </summary>
public static class PolyglotServiceCollectionExtensions
{
    /// <summary>
    /// Add polyglot persistence services with the specified DbContext as primary.
    /// </summary>
    /// <typeparam name="TContext">The primary DbContext type</typeparam>
    public static IServiceCollection AddPolyglotPersistence<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        // Configure polyglot options
        services.Configure<PolyglotOptions>(
            configuration.GetSection(PolyglotOptions.SectionName));

        // Register the EF specification repository for standard use
        services.AddScoped(typeof(ISpecRepository<>), typeof(EfSpecificationRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfSpecificationRepository<>));

        // Register the polyglot repository for entities that need migration support
        services.AddScoped(typeof(IPolyglotRepository<>), typeof(PolyglotRepository<>));

        return services;
    }

    /// <summary>
    /// Add polyglot persistence with dual-write support between two DbContexts.
    /// </summary>
    /// <typeparam name="TPrimaryContext">The primary DbContext (Cosmos DB)</typeparam>
    /// <typeparam name="TSecondaryContext">The secondary DbContext (PostgreSQL)</typeparam>
    public static IServiceCollection AddPolyglotPersistenceWithDualWrite<TPrimaryContext, TSecondaryContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TPrimaryContext : DbContext
        where TSecondaryContext : DbContext
    {
        // Configure polyglot options
        services.Configure<PolyglotOptions>(
            configuration.GetSection(PolyglotOptions.SectionName));

        // Register both contexts
        // Note: Individual DbContexts should be registered separately with their connection strings

        // Register repositories with awareness of both contexts
        services.AddScoped(typeof(ISpecRepository<>), typeof(EfSpecificationRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfSpecificationRepository<>));
        services.AddScoped(typeof(IPolyglotRepository<>), typeof(PolyglotRepository<>));

        return services;
    }

    /// <summary>
    /// Add a specific entity type to use polyglot repository with dual-write.
    /// </summary>
    public static IServiceCollection AddPolyglotEntity<TEntity, TContext>(
        this IServiceCollection services)
        where TEntity : class
        where TContext : DbContext
    {
        services.AddScoped<IPolyglotRepository<TEntity>>(sp =>
        {
            var context = sp.GetRequiredService<TContext>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PolyglotOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PolyglotRepository<TEntity>>>();

            return new PolyglotRepository<TEntity>(context, options, logger);
        });

        return services;
    }
}
