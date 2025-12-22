using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Application.Configuration.StoryProtocol;
using Mystira.App.Application.Ports;
using Mystira.App.Infrastructure.StoryProtocol.HealthChecks;
using Mystira.App.Infrastructure.StoryProtocol.Services;

namespace Mystira.App.Infrastructure.StoryProtocol;

/// <summary>
/// Extension methods for registering Story Protocol services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Story Protocol services to the dependency injection container.
    /// Supports three implementation modes:
    /// 1. Mock (default) - For development and testing
    /// 2. gRPC - Communicates with Mystira.Chain Python service (recommended for production)
    /// 3. Direct - Uses Nethereum for direct blockchain access (fallback)
    /// </summary>
    /// <remarks>
    /// See ADR-0013 for architectural rationale on gRPC adoption.
    /// </remarks>
    public static IServiceCollection AddStoryProtocolServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get StoryProtocol options
        var storyProtocolSection = configuration.GetSection(StoryProtocolOptions.SectionName);
        var storyProtocolOptions = new StoryProtocolOptions();
        if (storyProtocolSection.Exists())
        {
            storyProtocolSection.Bind(storyProtocolOptions);
        }

        // Get ChainService (gRPC) options
        var chainServiceSection = configuration.GetSection(ChainServiceOptions.SectionName);
        var chainServiceOptions = new ChainServiceOptions();
        if (chainServiceSection.Exists())
        {
            chainServiceSection.Bind(chainServiceOptions);
        }

        // Register configuration for DI injection
        services.Configure<StoryProtocolOptions>(config =>
        {
            if (storyProtocolSection.Exists())
            {
                storyProtocolSection.Bind(config);
            }
        });

        services.Configure<ChainServiceOptions>(config =>
        {
            if (chainServiceSection.Exists())
            {
                chainServiceSection.Bind(config);
            }
        });

        // Determine which implementation to use
        // Priority: Mock > gRPC > Direct
        if (!storyProtocolOptions.Enabled || storyProtocolOptions.UseMockImplementation)
        {
            // Mock implementation for development/testing
            services.AddSingleton<IStoryProtocolService, MockStoryProtocolService>();
        }
        else if (chainServiceOptions.UseGrpc)
        {
            // gRPC implementation - communicates with Mystira.Chain (Python)
            // Recommended for production (see ADR-0013)
            // Register as both interface and concrete type (for health checks)
            services.AddSingleton<GrpcChainServiceAdapter>();
            services.AddSingleton<IStoryProtocolService>(sp => sp.GetRequiredService<GrpcChainServiceAdapter>());
        }
        else
        {
            // Direct Nethereum implementation - fallback for direct blockchain access
            services.AddSingleton<IStoryProtocolService, StoryProtocolService>();
        }

        return services;
    }

    /// <summary>
    /// Adds Story Protocol services with gRPC enabled by default.
    /// Use this when Mystira.Chain service is available.
    /// </summary>
    public static IServiceCollection AddStoryProtocolWithGrpc(
        this IServiceCollection services,
        IConfiguration configuration,
        string grpcEndpoint)
    {
        // Configure ChainService with gRPC enabled
        services.Configure<ChainServiceOptions>(options =>
        {
            options.UseGrpc = true;
            options.GrpcEndpoint = grpcEndpoint;
        });

        // Configure StoryProtocol as enabled
        services.Configure<StoryProtocolOptions>(options =>
        {
            options.Enabled = true;
            options.UseMockImplementation = false;
        });

        // Register the gRPC adapter (as both interface and concrete type for health checks)
        services.AddSingleton<GrpcChainServiceAdapter>();
        services.AddSingleton<IStoryProtocolService>(sp => sp.GetRequiredService<GrpcChainServiceAdapter>());

        return services;
    }

    /// <summary>
    /// Adds Chain service (gRPC) health checks.
    /// </summary>
    /// <param name="builder">The health checks builder</param>
    /// <param name="name">Optional name for the health check (default: "chain_service")</param>
    /// <param name="tags">Optional tags for the health check</param>
    /// <returns>The health checks builder for chaining</returns>
    public static IHealthChecksBuilder AddChainServiceHealthCheck(
        this IHealthChecksBuilder builder,
        string? name = null,
        string[]? tags = null)
    {
        name ??= "chain_service";
        tags ??= ["grpc", "chain", "blockchain", "ready"];

        return builder.AddCheck<ChainServiceHealthCheck>(name, tags: tags);
    }
}
