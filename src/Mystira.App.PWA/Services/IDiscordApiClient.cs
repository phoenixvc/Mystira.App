namespace Mystira.App.PWA.Services;

/// <summary>
/// Interface for Discord API operations
/// </summary>
public interface IDiscordApiClient
{
    /// <summary>
    /// Get Discord bot status
    /// </summary>
    Task<DiscordStatusResponse?> GetStatusAsync();

    /// <summary>
    /// Send a message to Discord channel
    /// </summary>
    Task<bool> SendMessageAsync(ulong channelId, string message);

    /// <summary>
    /// Send a rich embed to Discord channel
    /// </summary>
    Task<bool> SendEmbedAsync(DiscordEmbedRequest request);
}

public class DiscordStatusResponse
{
    public bool Enabled { get; set; }
    public bool Connected { get; set; }
    public string? BotUsername { get; set; }
    public string? BotId { get; set; }
    public string? Message { get; set; }
}

public class DiscordMessageRequest
{
    public ulong ChannelId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DiscordEmbedRequest
{
    public ulong ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte ColorRed { get; set; } = 52;
    public byte ColorGreen { get; set; } = 152;
    public byte ColorBlue { get; set; } = 219;
    public string? Footer { get; set; }
    public List<DiscordEmbedField>? Fields { get; set; }
}

public class DiscordEmbedField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Inline { get; set; }
}
