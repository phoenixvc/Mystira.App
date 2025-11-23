using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Application.Ports;
using Mystira.App.Infrastructure.StoryProtocol.Configuration;
using Mystira.App.Infrastructure.StoryProtocol.Services;

namespace Mystira.App.Infrastructure.StoryProtocol;

/// <summary>
/// Extension methods for registering Story Protocol services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Story Protocol services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddStoryProtocolServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<StoryProtocolOptions>(
            configuration.GetSection(StoryProtocolOptions.SectionName));

        // Register the appropriate implementation based on configuration
        var options = configuration
            .GetSection(StoryProtocolOptions.SectionName)
            .Get<StoryProtocolOptions>() ?? new StoryProtocolOptions();

        // For now, always use mock implementation until blockchain integration is ready
        // TODO: Implement actual blockchain service and add conditional logic
        if (options.Enabled && !options.UseMockImplementation)
        {
            // Placeholder for future blockchain implementation
            // services.AddSingleton<IStoryProtocolService, StoryProtocolService>();
            throw new NotImplementedException("Blockchain Story Protocol integration not yet implemented. Please set UseMockImplementation to true.");
        }

        // Register mock implementation (default)
        services.AddSingleton<IStoryProtocolService, MockStoryProtocolService>();

        return services;
    }
}
