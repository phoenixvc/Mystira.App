using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Infrastructure.Discord.Services;

/// <summary>
/// Background service that manages the Discord bot lifecycle.
/// This can be used in Azure App Service WebJobs, Container Apps, or as a standalone service.
/// Uses the Application port interface for clean architecture compliance.
/// </summary>
public class DiscordBotHostedService : BackgroundService
{
    private readonly IDiscordBotService _discordBotService;
    private readonly ILogger<DiscordBotHostedService> _logger;

    public DiscordBotHostedService(
        IDiscordBotService discordBotService,
        ILogger<DiscordBotHostedService> logger)
    {
        _discordBotService = discordBotService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Discord bot hosted service is starting");

        try
        {
            await _discordBotService.StartAsync(stoppingToken);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Discord bot hosted service is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discord bot hosted service encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot hosted service is stopping");

        try
        {
            await _discordBotService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Discord bot");
        }

        await base.StopAsync(cancellationToken);
    }
}
