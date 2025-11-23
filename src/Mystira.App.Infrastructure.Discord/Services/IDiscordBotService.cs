using Discord;
using Discord.WebSocket;

namespace Mystira.App.Infrastructure.Discord.Services;

/// <summary>
/// Interface for Discord bot operations
/// This is the port (hexagonal architecture) that external systems can use
/// </summary>
public interface IDiscordBotService
{
    /// <summary>
    /// Start the Discord bot connection
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the Discord bot connection
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to a specific channel
    /// </summary>
    /// <param name="channelId">Discord channel ID</param>
    /// <param name="message">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an embed message to a specific channel
    /// </summary>
    /// <param name="channelId">Discord channel ID</param>
    /// <param name="embed">Embed builder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmbedAsync(ulong channelId, Embed embed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reply to a specific message
    /// </summary>
    /// <param name="messageId">Message ID to reply to</param>
    /// <param name="channelId">Channel ID where the message is located</param>
    /// <param name="reply">Reply content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the bot is connected and ready
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Get the current bot user
    /// </summary>
    SocketSelfUser? CurrentUser { get; }
}
