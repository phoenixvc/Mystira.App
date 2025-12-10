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
        // Register configuration with default values
        services.Configure<StoryProtocolOptions>(config =>
        {
            // Bind configuration section if it exists, otherwise use defaults
            var section = configuration.GetSection(StoryProtocolOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(config);
            }
            // If section doesn't exist, StoryProtocolOptions defaults will be used:
            // Enabled = false, UseMockImplementation = true
        });

        // Determine which implementation to register based on configuration
        var section = configuration.GetSection(StoryProtocolOptions.SectionName);
        var options = new StoryProtocolOptions();
        
        if (section.Exists())
        {
            section.Bind(options);
        }
        // If section doesn't exist, use default values (Enabled = false, UseMockImplementation = true)

        // Always register a service implementation to prevent DI errors
        // Default to mock implementation for safety
        if (!options.Enabled || options.UseMockImplementation)
        {
            // Register mock implementation (disabled or explicitly set to use mock)
            services.AddSingleton<IStoryProtocolService, MockStoryProtocolService>();
        }
        else
        {
            // Register real blockchain implementation (enabled and not using mock)
            services.AddSingleton<IStoryProtocolService, StoryProtocolService>();
        }

        return services;
    }
}
