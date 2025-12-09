using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.WhatsApp.Configuration;
using Mystira.App.Infrastructure.WhatsApp.Services;

namespace Mystira.App.Infrastructure.WhatsApp;

/// <summary>
/// Extension methods for registering WhatsApp services via Azure Communication Services.
/// Follows clean/hexagonal architecture - registers as Application port interfaces.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds WhatsApp services to the service collection.
    /// Registers the service as Application port interfaces:
    /// - IChatBotService (platform-agnostic chat bot operations)
    /// - IBotCommandService (platform-agnostic command support)
    ///
    /// Note: If you need multiple chat platforms, use keyed services or
    /// a factory pattern to resolve the correct implementation.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configureOptions">Optional action to configure WhatsApp options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWhatsAppBot(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<WhatsAppOptions>? configureOptions = null)
    {
        // Register configuration
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register WhatsApp bot service as singleton (maintains conversation state)
        services.AddSingleton<WhatsAppBotService>();

        return services;
    }

    /// <summary>
    /// Registers WhatsApp bot as the IChatBotService implementation.
    /// Use this when WhatsApp is your primary/only chat platform.
    /// FIX: Added IMessagingService registration for consistency with Discord.
    /// </summary>
    public static IServiceCollection AddWhatsAppBotAsDefault(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<WhatsAppOptions>? configureOptions = null)
    {
        services.AddWhatsAppBot(configuration, configureOptions);

        // Register as Application port interfaces
        services.AddSingleton<IMessagingService>(sp => sp.GetRequiredService<WhatsAppBotService>());
        services.AddSingleton<IChatBotService>(sp => sp.GetRequiredService<WhatsAppBotService>());
        services.AddSingleton<IBotCommandService>(sp => sp.GetRequiredService<WhatsAppBotService>());

        return services;
    }

    /// <summary>
    /// Adds WhatsApp bot as a keyed service (for multi-platform scenarios).
    /// Use this when you have multiple chat platforms (Discord + Teams + WhatsApp).
    /// FIX: Added IMessagingService registration for consistency with Discord.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="serviceKey">The key to use for the keyed service (default: "whatsapp")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWhatsAppBotKeyed(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceKey = "whatsapp")
    {
        services.AddWhatsAppBot(configuration);

        // Register as keyed services for multi-platform scenarios
        services.AddKeyedSingleton<IMessagingService>(serviceKey,
            (sp, _) => sp.GetRequiredService<WhatsAppBotService>());
        services.AddKeyedSingleton<IChatBotService>(serviceKey,
            (sp, _) => sp.GetRequiredService<WhatsAppBotService>());
        services.AddKeyedSingleton<IBotCommandService>(serviceKey,
            (sp, _) => sp.GetRequiredService<WhatsAppBotService>());

        return services;
    }
}
