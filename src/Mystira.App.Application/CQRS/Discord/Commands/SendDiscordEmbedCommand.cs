using Mystira.App.Application.CQRS.Common.Responses;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Command to send a rich embed to a Discord channel.
/// Requires Discord bot to be enabled and connected.
/// </summary>
public record SendDiscordEmbedCommand(
    ulong ChannelId,
    string Title,
    string Description,
    byte ColorRed,
    byte ColorGreen,
    byte ColorBlue,
    string? Footer,
    List<DiscordEmbedField>? Fields
) : ICommand<CommandResponse>;

/// <summary>
/// Represents a field in a Discord embed.
/// </summary>
public record DiscordEmbedField(
    string Name,
    string Value,
    bool Inline
);
