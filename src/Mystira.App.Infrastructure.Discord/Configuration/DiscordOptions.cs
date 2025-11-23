namespace Mystira.App.Infrastructure.Discord.Configuration;

/// <summary>
/// Configuration options for Discord bot integration
/// </summary>
public class DiscordOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Discord";

    /// <summary>
    /// Discord bot token from Discord Developer Portal
    /// Should be stored securely in Azure Key Vault or User Secrets
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of guild (server) IDs the bot should operate in
    /// If empty, bot will operate in all guilds it has access to
    /// </summary>
    public string GuildIds { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable message content intent (required for reading message content)
    /// </summary>
    public bool EnableMessageContentIntent { get; set; } = true;

    /// <summary>
    /// Whether to enable guild members intent (required for member information)
    /// </summary>
    public bool EnableGuildMembersIntent { get; set; }

    /// <summary>
    /// Default timeout for Discord API operations in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to log all messages (for debugging purposes)
    /// Should be false in production to avoid excessive logging
    /// </summary>
    public bool LogAllMessages { get; set; }

    /// <summary>
    /// Command prefix for text-based commands (e.g., "!")
    /// Leave empty to disable text commands
    /// </summary>
    public string CommandPrefix { get; set; } = "!";
}
