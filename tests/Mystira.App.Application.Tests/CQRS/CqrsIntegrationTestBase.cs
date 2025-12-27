using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mystira.Shared.CQRS;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Services;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using Wolverine;
using IUnitOfWork = Mystira.App.Application.Ports.Data.IUnitOfWork;

namespace Mystira.App.Application.Tests.CQRS;

/// <summary>
/// Adapter that provides MediatR-like Send syntax using Wolverine's IMessageBus.
/// This allows existing tests to work without code changes.
/// </summary>
public class MediatorAdapter
{
    private readonly IMessageBus _bus;

    public MediatorAdapter(IMessageBus bus) => _bus = bus;

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query)
        => _bus.InvokeAsync<TResponse>(query);

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command)
        => _bus.InvokeAsync<TResponse>(command);
}

/// <summary>
/// Base class for CQRS integration tests.
/// Provides in-memory database, repositories, and Wolverine message bus with caching.
/// </summary>
public abstract class CqrsIntegrationTestBase : IAsyncDisposable, IDisposable
{
    protected IHost Host { get; }
    protected IServiceProvider ServiceProvider => Host.Services;
    protected MystiraAppDbContext DbContext { get; }
    protected IMessageBus MessageBus { get; }
    protected MediatorAdapter Mediator { get; }
    protected IMemoryCache Cache { get; }
    protected IQueryCacheInvalidationService CacheInvalidation { get; }

    protected CqrsIntegrationTestBase()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        // Add in-memory database
        builder.Services.AddDbContext<MystiraAppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<MystiraAppDbContext>());

        // Add repositories
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
        builder.Services.AddScoped<IContentBundleRepository, ContentBundleRepository>();
        builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
        builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
        builder.Services.AddScoped<IAxisAchievementRepository, AxisAchievementRepository>();
        builder.Services.AddScoped<IBadgeImageRepository, BadgeImageRepository>();
        builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        builder.Services.AddScoped<IAgeGroupRepository, AgeGroupRepository>();
        builder.Services.AddScoped<ICompassAxisRepository, CompassAxisRepository>();
        builder.Services.AddScoped<IArchetypeRepository, ArchetypeRepository>();
        builder.Services.AddScoped<IFantasyThemeRepository, FantasyThemeRepository>();
        builder.Services.AddScoped<IEchoTypeRepository, EchoTypeRepository>();

        // Add Unit of Work (explicitly use Application port as service type)
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Memory Cache
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024;
            options.CompactionPercentage = 0.25;
        });

        // Add Cache Invalidation Service
        builder.Services.AddSingleton<IQueryCacheInvalidationService, QueryCacheInvalidationService>();

        // Add logging
        builder.Logging.AddDebug().SetMinimumLevel(LogLevel.Debug);

        // Configure Wolverine using AddWolverine (compatible with .NET 9 HostApplicationBuilder)
        builder.Services.AddWolverine(opts =>
        {
            // Discover handlers from Application assembly (not Mystira.Shared where IQuery<> is defined)
            // IQueryCacheInvalidationService is in Mystira.App.Application, same assembly as the handlers
            opts.Discovery.IncludeAssembly(typeof(IQueryCacheInvalidationService).Assembly);

            // Use durable local queues for testing
            opts.Policies.UseDurableLocalQueues();
        });

        Host = builder.Build();

        // Start the host to initialize Wolverine
        Host.Start();

        DbContext = ServiceProvider.GetRequiredService<MystiraAppDbContext>();
        MessageBus = ServiceProvider.GetRequiredService<IMessageBus>();
        Mediator = new MediatorAdapter(MessageBus);
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
        CacheInvalidation.ClearTrackedKeys();
    }

    public async ValueTask DisposeAsync()
    {
        CacheInvalidation?.ClearTrackedKeys();
        DbContext?.Dispose();
        await Host.StopAsync();
        Host.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
