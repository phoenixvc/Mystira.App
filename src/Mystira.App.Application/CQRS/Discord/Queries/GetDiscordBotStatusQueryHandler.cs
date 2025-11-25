using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Infrastructure.Discord.Services;

namespace Mystira.App.Application.CQRS.Discord.Queries;

/// <summary>
/// Handler for retrieving Discord bot status.
/// Checks configuration and bot service state.
/// </summary>
public class GetDiscordBotStatusQueryHandler
    : IQueryHandler<GetDiscordBotStatusQuery, DiscordBotStatusResponse>
{
    private readonly IDiscordBotService? _discordBotService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GetDiscordBotStatusQueryHandler> _logger;

    public GetDiscordBotStatusQueryHandler(
        IConfiguration configuration,
        ILogger<GetDiscordBotStatusQueryHandler> logger,
        IDiscordBotService? discordBotService = null)
    {
        _configuration = configuration;
        _logger = logger;
        _discordBotService = discordBotService;
    }

    public Task<DiscordBotStatusResponse> Handle(
        GetDiscordBotStatusQuery request,
        CancellationToken cancellationToken)
    {
        var enabled = _configuration.GetValue<bool>("Discord:Enabled", false);

        if (!enabled || _discordBotService == null)
        {
            _logger.LogDebug("Discord bot integration is disabled");
            return Task.FromResult(new DiscordBotStatusResponse(
                Enabled: false,
                Connected: false,
                BotUsername: null,
                BotId: null,
                Message: "Discord bot integration is disabled"
            ));
        }

        var isConnected = _discordBotService.IsConnected;
        var currentUser = _discordBotService.CurrentUser;

        _logger.LogDebug("Discord bot status: Connected={Connected}, Username={Username}",
            isConnected, currentUser?.Username);

        return Task.FromResult(new DiscordBotStatusResponse(
            Enabled: true,
            Connected: isConnected,
            BotUsername: currentUser?.Username,
            BotId: currentUser?.Id.ToString(),
            Message: isConnected ? "Discord bot is connected" : "Discord bot is not connected"
        ));
    }
}
