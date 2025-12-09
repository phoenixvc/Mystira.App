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
    private readonly IChatBotService _chatBotService;
    private readonly ILogger<DiscordBotHostedService> _logger;

    public DiscordBotHostedService(
        IChatBotService chatBotService,
        ILogger<DiscordBotHostedService> logger)
    {
        _chatBotService = chatBotService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Chat bot hosted service is starting");

        try
        {
            await _chatBotService.StartAsync(stoppingToken);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat bot hosted service is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat bot hosted service encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Chat bot hosted service is stopping");

        try
        {
            await _chatBotService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping chat bot");
        }

        await base.StopAsync(cancellationToken);
    }
}
