using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.Discord.Commands;

/// <summary>
/// Command to send a message to a Discord channel.
/// Requires Discord bot to be enabled and connected.
/// </summary>
public record SendDiscordMessageCommand(
    ulong ChannelId,
    string Message
) : ICommand<(bool Success, string Message)>;
