using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Teams.Configuration;
using Mystira.App.Infrastructure.Teams.Services;

namespace Mystira.App.Infrastructure.Teams;

/// <summary>
/// Extension methods for registering Microsoft Teams bot services.
/// Follows clean/hexagonal architecture - registers as Application port interfaces.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Microsoft Teams bot services to the service collection.
    /// Registers the service as Application port interfaces:
    /// - IChatBotService (platform-agnostic chat bot operations)
    /// - IBotCommandService (platform-agnostic command support)
    ///
    /// Note: If you need both Discord and Teams, use keyed services or
    /// a factory pattern to resolve the correct implementation.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configureOptions">Optional action to configure Teams options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTeamsBot(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<TeamsOptions>? configureOptions = null)
    {
        // Register configuration
        services.Configure<TeamsOptions>(configuration.GetSection(TeamsOptions.SectionName));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register Teams bot service as singleton (maintains conversation references)
        services.AddSingleton<TeamsBotService>();

        return services;
    }

    /// <summary>
    /// Registers Teams bot as the IChatBotService implementation.
    /// Use this when Teams is your primary/only chat platform.
    /// FIX: Added IMessagingService registration for consistency with Discord.
    /// </summary>
    public static IServiceCollection AddTeamsBotAsDefault(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<TeamsOptions>? configureOptions = null)
    {
        services.AddTeamsBot(configuration, configureOptions);

        // Register as Application port interfaces
        services.AddSingleton<IMessagingService>(sp => sp.GetRequiredService<TeamsBotService>());
        services.AddSingleton<IChatBotService>(sp => sp.GetRequiredService<TeamsBotService>());
        services.AddSingleton<IBotCommandService>(sp => sp.GetRequiredService<TeamsBotService>());

        return services;
    }

    /// <summary>
    /// Adds Teams bot as a keyed service (for multi-platform scenarios).
    /// Use this when you have multiple chat platforms (Discord + Teams).
    /// FIX: Added IMessagingService registration for consistency with Discord.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="serviceKey">The key to use for the keyed service (default: "teams")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTeamsBotKeyed(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceKey = "teams")
    {
        services.AddTeamsBot(configuration);

        // Register as keyed services for multi-platform scenarios
        services.AddKeyedSingleton<IMessagingService>(serviceKey,
            (sp, _) => sp.GetRequiredService<TeamsBotService>());
        services.AddKeyedSingleton<IChatBotService>(serviceKey,
            (sp, _) => sp.GetRequiredService<TeamsBotService>());
        services.AddKeyedSingleton<IBotCommandService>(serviceKey,
            (sp, _) => sp.GetRequiredService<TeamsBotService>());

        return services;
    }
}
