namespace Mystira.App.Application.Ports.Messaging;

/// <summary>
/// Identifies the chat platform for platform-specific logic.
/// </summary>
public enum ChatPlatform
{
    Discord,
    Teams,
    WhatsApp,
    Slack
}

/// <summary>
/// Port interface for chat bot operations.
/// Platform-agnostic interface that can be implemented by Discord, Teams, Slack, etc.
/// </summary>
public interface IChatBotService
{
    /// <summary>
    /// Gets the platform identifier for this service.
    /// </summary>
    ChatPlatform Platform { get; }

    /// <summary>
    /// Start the chat bot connection
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the chat bot connection
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to a specific channel
    /// </summary>
    Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an embed message to a specific channel
    /// </summary>
    Task SendEmbedAsync(ulong channelId, EmbedData embed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reply to a specific message
    /// </summary>
    Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the bot is connected and ready
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Get bot status information
    /// </summary>
    BotStatus GetStatus();

    // ─────────────────────────────────────────────────────────────────
    // Broadcast / First Responder Pattern
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Send a message to multiple channels and await the first response.
    /// Useful for support escalation, load balancing, or failover scenarios.
    /// </summary>
    /// <param name="channelIds">Channels to broadcast to</param>
    /// <param name="message">Message content</param>
    /// <param name="timeout">How long to wait for a response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing first responder info, or TimedOut=true if no response</returns>
    Task<FirstResponderResult> SendAndAwaitFirstResponseAsync(
        IEnumerable<ulong> channelIds,
        string message,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an embed to multiple channels and await the first response.
    /// Useful for rich formatted messages in support escalation or announcements.
    /// </summary>
    /// <param name="channelIds">Channels to broadcast to</param>
    /// <param name="embed">Rich embed content (title, description, fields, etc.)</param>
    /// <param name="timeout">How long to wait for a response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing first responder info, or TimedOut=true if no response</returns>
    /// <remarks>
    /// Platform support varies:
    /// - Discord: Full embed support with colors, fields, and footer
    /// - Teams: Converts to Hero Card (no color support)
    /// - WhatsApp: Converts to formatted text
    /// </remarks>
    Task<FirstResponderResult> SendEmbedAndAwaitFirstResponseAsync(
        IEnumerable<ulong> channelIds,
        EmbedData embed,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast to multiple channels with a response handler callback.
    /// Handler returns true to stop listening for more responses.
    /// </summary>
    /// <param name="channelIds">Channels to broadcast to</param>
    /// <param name="message">Message content</param>
    /// <param name="onResponse">Callback for each response; return true to stop listening</param>
    /// <param name="timeout">Maximum time to listen for responses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastWithResponseHandlerAsync(
        IEnumerable<ulong> channelIds,
        string message,
        Func<ResponseEvent, Task<bool>> onResponse,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Platform-agnostic embed data for rich messages
/// </summary>
public class EmbedData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ColorRed { get; set; }
    public int ColorGreen { get; set; }
    public int ColorBlue { get; set; }
    public string? Footer { get; set; }
    public List<EmbedFieldData>? Fields { get; set; }
}

/// <summary>
/// Platform-agnostic embed field data
/// </summary>
public class EmbedFieldData
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Inline { get; set; }
}

/// <summary>
/// Bot status information (platform-agnostic)
/// </summary>
public class BotStatus
{
    public bool IsEnabled { get; set; }
    public bool IsConnected { get; set; }
    public string? BotName { get; set; }
    public ulong? BotId { get; set; }
    /// <summary>
    /// Number of servers/guilds/workspaces the bot is connected to
    /// </summary>
    public int ServerCount { get; set; }
}

// ─────────────────────────────────────────────────────────────────
// Broadcast / First Responder Types
// ─────────────────────────────────────────────────────────────────

/// <summary>
/// Result from a broadcast-and-await-first-response operation.
/// </summary>
public class FirstResponderResult
{
    /// <summary>
    /// Whether the operation timed out without receiving a response.
    /// </summary>
    public bool TimedOut { get; set; }

    /// <summary>
    /// The channel ID that responded first.
    /// </summary>
    public ulong RespondingChannelId { get; set; }

    /// <summary>
    /// The channel name that responded first.
    /// </summary>
    public string? RespondingChannelName { get; set; }

    /// <summary>
    /// The message ID of the response.
    /// </summary>
    public ulong ResponseMessageId { get; set; }

    /// <summary>
    /// The content of the response message.
    /// </summary>
    public string ResponseContent { get; set; } = string.Empty;

    /// <summary>
    /// The user ID of the responder.
    /// </summary>
    public ulong ResponderId { get; set; }

    /// <summary>
    /// The username of the responder.
    /// </summary>
    public string? ResponderName { get; set; }

    /// <summary>
    /// Time elapsed from broadcast to first response.
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// IDs of messages sent to channels (for cleanup/reference).
    /// </summary>
    public List<SentMessage> SentMessages { get; set; } = new();
}

/// <summary>
/// Represents a message sent during a broadcast operation.
/// </summary>
public class SentMessage
{
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
}

/// <summary>
/// Event data for response handler callbacks.
/// </summary>
public class ResponseEvent
{
    /// <summary>
    /// The channel ID where the response was received.
    /// </summary>
    public ulong ChannelId { get; set; }

    /// <summary>
    /// The channel name where the response was received.
    /// </summary>
    public string? ChannelName { get; set; }

    /// <summary>
    /// The message ID of the response.
    /// </summary>
    public ulong MessageId { get; set; }

    /// <summary>
    /// The content of the response.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The user ID of the responder.
    /// </summary>
    public ulong ResponderId { get; set; }

    /// <summary>
    /// The username of the responder.
    /// </summary>
    public string? ResponderName { get; set; }

    /// <summary>
    /// Time elapsed since the broadcast was sent.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// The original message ID this is responding to (if it's a reply).
    /// </summary>
    public ulong? ReplyToMessageId { get; set; }

    /// <summary>
    /// Whether this response is a direct reply to the broadcast message.
    /// </summary>
    public bool IsDirectReply { get; set; }
}
