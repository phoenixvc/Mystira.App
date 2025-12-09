using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Infrastructure.Discord.HealthChecks;

/// <summary>
/// Health check to verify Discord bot connectivity.
/// Uses the Application port interface for clean architecture compliance.
/// </summary>
public class DiscordBotHealthCheck : IHealthCheck
{
    private readonly IDiscordBotService _discordBotService;

    public DiscordBotHealthCheck(IDiscordBotService discordBotService)
    {
        _discordBotService = discordBotService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = _discordBotService.GetStatus();

            if (!status.IsConnected)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        "Discord bot is not connected",
                        data: new Dictionary<string, object>
                        {
                            ["IsConnected"] = false,
                            ["IsEnabled"] = status.IsEnabled
                        }));
            }

            if (string.IsNullOrEmpty(status.BotName))
            {
                return Task.FromResult(
                    HealthCheckResult.Degraded(
                        "Discord bot is connected but user information is not available",
                        data: new Dictionary<string, object>
                        {
                            ["IsConnected"] = true,
                            ["HasUserInfo"] = false
                        }));
            }

            var data = new Dictionary<string, object>
            {
                ["IsConnected"] = true,
                ["BotUsername"] = status.BotName,
                ["GuildCount"] = status.GuildCount
            };

            if (status.BotId.HasValue)
            {
                data["BotId"] = status.BotId.Value;
            }

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "Discord bot is connected and operational",
                    data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Discord bot health check failed",
                    exception: ex));
        }
    }
}
