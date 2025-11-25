using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Queries;

/// <summary>
/// Handler for retrieving Discord bot status.
/// Checks bot service state.
/// </summary>
public class GetDiscordBotStatusQueryHandler
    : IQueryHandler<GetDiscordBotStatusQuery, DiscordBotStatusResponse>
{
    private readonly IDiscordBotService? _discordBotService;
    private readonly ILogger<GetDiscordBotStatusQueryHandler> _logger;

    public GetDiscordBotStatusQueryHandler(
        ILogger<GetDiscordBotStatusQueryHandler> logger,
        IDiscordBotService? discordBotService = null)
    {
        _logger = logger;
        _discordBotService = discordBotService;
    }

    public Task<DiscordBotStatusResponse> Handle(
        GetDiscordBotStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (_discordBotService == null)
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

        var status = _discordBotService.GetStatus();

        _logger.LogDebug("Discord bot status: Enabled={Enabled}, Connected={Connected}, Username={Username}",
            status.IsEnabled, status.IsConnected, status.BotName);

        return Task.FromResult(new DiscordBotStatusResponse(
            Enabled: status.IsEnabled,
            Connected: status.IsConnected,
            BotUsername: status.BotName,
            BotId: null,
            Message: status.IsConnected ? "Discord bot is connected" : "Discord bot is not connected"
        ));
    }
}
