using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.App.Infrastructure.Discord.Services;

namespace Mystira.App.Infrastructure.Discord.HealthChecks;

/// <summary>
/// Health check to verify Discord bot connectivity.
/// Uses concrete DiscordBotService to ensure correct service is injected in multi-platform setups.
/// </summary>
public class DiscordBotHealthCheck : IHealthCheck
{
    private readonly DiscordBotService _discordBotService;

    // FIX: Inject concrete DiscordBotService instead of IChatBotService
    // to avoid incorrect service resolution in multi-platform DI container
    public DiscordBotHealthCheck(DiscordBotService discordBotService)
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
                ["ServerCount"] = status.ServerCount
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
