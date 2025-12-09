using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Application.CQRS.Discord.Queries;

/// <summary>
/// Handler for retrieving Discord bot status.
/// Checks bot service state via the platform-agnostic IChatBotService.
/// </summary>
public class GetDiscordBotStatusQueryHandler
    : IQueryHandler<GetDiscordBotStatusQuery, DiscordBotStatusResponse>
{
    // FIX: Remove optional dependency anti-pattern - require the service
    private readonly IChatBotService _chatBotService;
    private readonly ILogger<GetDiscordBotStatusQueryHandler> _logger;

    public GetDiscordBotStatusQueryHandler(
        ILogger<GetDiscordBotStatusQueryHandler> logger,
        IChatBotService chatBotService)
    {
        _logger = logger;
        _chatBotService = chatBotService ?? throw new ArgumentNullException(nameof(chatBotService));
    }

    public Task<DiscordBotStatusResponse> Handle(
        GetDiscordBotStatusQuery request,
        CancellationToken cancellationToken)
    {
        var status = _chatBotService.GetStatus();

        _logger.LogDebug("Discord bot status: Enabled={Enabled}, Connected={Connected}, Username={Username}",
            status.IsEnabled, status.IsConnected, status.BotName);

        // FIX: Include BotId from status instead of always returning null
        return Task.FromResult(new DiscordBotStatusResponse(
            Enabled: status.IsEnabled,
            Connected: status.IsConnected,
            BotUsername: status.BotName,
            BotId: status.BotId,
            Message: status.IsConnected ? "Discord bot is connected" : "Discord bot is not connected"
        ));
    }
}
