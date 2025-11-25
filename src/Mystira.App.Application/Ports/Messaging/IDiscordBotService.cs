namespace Mystira.App.Application.Ports.Messaging;

/// <summary>
/// Port interface for Discord bot operations.
/// Implementations handle Discord-specific functionality.
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
/// Bot status information
/// </summary>
public class BotStatus
{
    public bool IsEnabled { get; set; }
    public bool IsConnected { get; set; }
    public string? BotName { get; set; }
    public int GuildCount { get; set; }
}
