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
        // Helper method to get options from configuration with defaults
        var section = configuration.GetSection(StoryProtocolOptions.SectionName);
        var options = new StoryProtocolOptions();
        if (section.Exists())
        {
            section.Bind(options);
        }
        
        // Register configuration for DI injection
        services.Configure<StoryProtocolOptions>(config =>
        {
            // Bind configuration section if it exists, otherwise use defaults
            if (section.Exists())
            {
                section.Bind(config);
            }
            // If section doesn't exist, StoryProtocolOptions defaults will be used:
            // Enabled = false, UseMockImplementation = true
        });

        // Always register a service implementation to prevent DI errors
        // Default to mock implementation for safety when disabled or explicitly using mock
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
