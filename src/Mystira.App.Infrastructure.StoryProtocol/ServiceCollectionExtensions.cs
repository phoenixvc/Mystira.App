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

        if (!options.Enabled)
        {
            // When disabled, register mock implementation
            services.AddSingleton<IStoryProtocolService, MockStoryProtocolService>();
        }
        else if (options.UseMockImplementation)
        {
            // Register mock implementation for development/testing
            services.AddSingleton<IStoryProtocolService, MockStoryProtocolService>();
        }
        else
        {
            // Register real blockchain implementation for production
            services.AddSingleton<IStoryProtocolService, StoryProtocolService>();
        }

        return services;
    }
}
