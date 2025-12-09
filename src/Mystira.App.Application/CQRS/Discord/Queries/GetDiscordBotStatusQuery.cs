namespace Mystira.App.Application.CQRS.Discord.Queries;

/// <summary>
/// Query to retrieve Discord bot connection status and information.
/// </summary>
public record GetDiscordBotStatusQuery : IQuery<DiscordBotStatusResponse>;

/// <summary>
/// Response containing Discord bot status information.
/// </summary>
public record DiscordBotStatusResponse(
    bool Enabled,
    bool Connected,
    string? BotUsername,
    // FIX: BotId type changed from string? to ulong? to match BotStatus.BotId
    ulong? BotId,
    string? Message
);
