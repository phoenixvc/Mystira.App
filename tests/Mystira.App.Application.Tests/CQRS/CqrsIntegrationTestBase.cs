using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Behaviors;
using Mystira.App.Application.CQRS;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using IUnitOfWork = Mystira.App.Application.Ports.Data.IUnitOfWork;

namespace Mystira.App.Application.Tests.CQRS;

/// <summary>
/// Base class for CQRS integration tests.
/// Provides in-memory database, repositories, and MediatR with caching.
/// </summary>
public abstract class CqrsIntegrationTestBase : IDisposable
{
    protected ServiceProvider ServiceProvider { get; }
    protected MystiraAppDbContext DbContext { get; }
    protected IMediator Mediator { get; }
    protected IMemoryCache Cache { get; }
    protected IQueryCacheInvalidationService CacheInvalidation { get; }

    protected CqrsIntegrationTestBase()
    {
        var services = new ServiceCollection();

        // Add in-memory database
        services.AddDbContext<MystiraAppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<MystiraAppDbContext>());

        // Add repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IScenarioRepository, ScenarioRepository>();
        services.AddScoped<IContentBundleRepository, ContentBundleRepository>();
        services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<IAxisAchievementRepository, AxisAchievementRepository>();
        services.AddScoped<IBadgeImageRepository, BadgeImageRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IAgeGroupRepository, AgeGroupRepository>();
        services.AddScoped<ICompassAxisRepository, CompassAxisRepository>();
        services.AddScoped<IArchetypeRepository, ArchetypeRepository>();

        // Add Unit of Work (explicitly use Application port as service type)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Memory Cache
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024;
            options.CompactionPercentage = 0.25;
        });

        // Add Cache Invalidation Service
        services.AddSingleton<IQueryCacheInvalidationService, QueryCacheInvalidationService>();

        // Add MediatR with caching behavior
        services.AddMediatR(cfg =>
        {
            // Register handlers from Application assembly; using IQuery<> marker type
            cfg.RegisterServicesFromAssembly(typeof(IQuery<>).Assembly);
            cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
        });

        // Add logging
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<MystiraAppDbContext>();
        Mediator = ServiceProvider.GetRequiredService<IMediator>();
        Cache = ServiceProvider.GetRequiredService<IMemoryCache>();
        CacheInvalidation = ServiceProvider.GetRequiredService<IQueryCacheInvalidationService>();
    }

    /// <summary>
    /// Seeds the database with initial test data.
    /// Override this in derived classes to add entity-specific test data.
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        // Override in derived classes
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clears all cached entries for testing cache behavior.
    /// </summary>
    protected void ClearCache()
    {
        if (Cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Remove 100% of entries
        }
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
