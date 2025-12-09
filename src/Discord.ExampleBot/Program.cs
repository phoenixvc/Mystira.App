using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Discord;
using Mystira.App.Infrastructure.Discord.Services;

namespace Discord.ExampleBot;

/// <summary>
/// Example Discord bot using the Infrastructure.Discord layer.
/// Demonstrates proper integration with the Mystira.App stack.
///
/// This is now a thin host that uses the shared Infrastructure.Discord components:
/// - DiscordBotService (messaging + slash commands)
/// - TicketModule (from Infrastructure.Discord.Modules)
/// - SampleTicketStartupService (for testing)
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddUserSecrets<Program>(optional: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Register Discord bot using Infrastructure layer
                // This registers: IMessagingService, IDiscordBotService, ISlashCommandService
                services.AddDiscordBot(context.Configuration);

                // Add the hosted service that manages bot lifecycle
                services.AddDiscordBotHostedService();

                // Add ticket support services
                services.AddDiscordTicketSupport();

                // Register the startup orchestrator for command registration
                services.AddHostedService<BotStartupService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        await host.RunAsync();
    }
}

/// <summary>
/// Hosted service that handles bot startup tasks after connection:
/// - Registering slash command modules from Infrastructure.Discord
/// - Running sample ticket creation if enabled
/// </summary>
public class BotStartupService : BackgroundService
{
    private readonly ISlashCommandService _slashCommandService;
    private readonly SampleTicketStartupService _sampleTicketService;
    private readonly IDiscordBotService _botService;
    private readonly ILogger<BotStartupService> _logger;

    public BotStartupService(
        ISlashCommandService slashCommandService,
        SampleTicketStartupService sampleTicketService,
        IDiscordBotService botService,
        ILogger<BotStartupService> logger)
    {
        _slashCommandService = slashCommandService;
        _sampleTicketService = sampleTicketService;
        _botService = botService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for bot to connect
        _logger.LogInformation("Waiting for Discord bot to connect...");

        var maxWait = TimeSpan.FromSeconds(30);
        var waited = TimeSpan.Zero;
        while (!_botService.IsConnected && waited < maxWait)
        {
            await Task.Delay(500, stoppingToken);
            waited += TimeSpan.FromMilliseconds(500);
        }

        if (!_botService.IsConnected)
        {
            _logger.LogWarning("Bot did not connect within timeout. Startup tasks may fail.");
        }

        try
        {
            // Register command modules from Infrastructure.Discord assembly
            // This includes the TicketModule (/ticket, /ticket-close)
            var infrastructureAssembly = typeof(Mystira.App.Infrastructure.Discord.Modules.TicketModule).Assembly;
            await _slashCommandService.RegisterCommandsAsync(infrastructureAssembly, stoppingToken);

            _logger.LogInformation("Registered {Count} command modules from Infrastructure.Discord",
                _slashCommandService.RegisteredModuleCount);

            // Post sample ticket if enabled in configuration
            await _sampleTicketService.PostSampleTicketIfEnabledAsync();

            _logger.LogInformation("Bot startup complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bot startup");
        }
    }
}
