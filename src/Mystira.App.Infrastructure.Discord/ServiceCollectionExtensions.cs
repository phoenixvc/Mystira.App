using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Infrastructure.Discord.Configuration;
using Mystira.App.Infrastructure.Discord.HealthChecks;
using Mystira.App.Infrastructure.Discord.Services;

namespace Mystira.App.Infrastructure.Discord;

/// <summary>
/// Extension methods for registering Discord services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Discord bot services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configureOptions">Optional action to configure Discord options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDiscordBot(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DiscordOptions>? configureOptions = null)
    {
        // Register configuration
        services.Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.SectionName));
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register Discord bot service as singleton (maintains persistent connection)
        services.AddSingleton<IDiscordBotService, DiscordBotService>();

        return services;
    }

    /// <summary>
    /// Adds Discord bot as a hosted service (background service)
    /// This is suitable for running the bot continuously in Azure App Service WebJobs,
    /// Container Apps, or as a standalone service
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDiscordBotHostedService(this IServiceCollection services)
    {
        services.AddHostedService<DiscordBotHostedService>();
        return services;
    }

    /// <summary>
    /// Adds Discord bot health checks
    /// </summary>
    /// <param name="builder">The health checks builder</param>
    /// <param name="name">Optional name for the health check</param>
    /// <param name="tags">Optional tags for the health check</param>
    /// <returns>The health checks builder for chaining</returns>
    public static IHealthChecksBuilder AddDiscordBotHealthCheck(
        this IHealthChecksBuilder builder,
        string? name = null,
        string[]? tags = null)
    {
        name ??= "discord_bot";
        tags ??= new[] { "discord", "bot", "ready" };

        return builder.AddCheck<DiscordBotHealthCheck>(name, tags: tags);
    }
}
