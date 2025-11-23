using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mystira.App.Infrastructure.Discord.Services;

namespace Mystira.App.Infrastructure.Discord.HealthChecks;

/// <summary>
/// Health check to verify Discord bot connectivity
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
            if (!_discordBotService.IsConnected)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        "Discord bot is not connected",
                        data: new Dictionary<string, object>
                        {
                            ["IsConnected"] = false
                        }));
            }

            var currentUser = _discordBotService.CurrentUser;
            if (currentUser == null)
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

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "Discord bot is connected and operational",
                    data: new Dictionary<string, object>
                    {
                        ["IsConnected"] = true,
                        ["BotUsername"] = currentUser.Username,
                        ["BotId"] = currentUser.Id
                    }));
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
