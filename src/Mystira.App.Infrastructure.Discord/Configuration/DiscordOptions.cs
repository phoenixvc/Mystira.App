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
    /// Primary guild (server) ID for slash command registration.
    /// If set, commands are registered to this guild (faster updates during development).
    /// If 0, commands are registered globally (takes up to 1 hour to propagate).
    /// </summary>
    public ulong GuildId { get; set; }

    /// <summary>
    /// Comma-separated list of guild (server) IDs the bot should operate in.
    /// If empty, bot will operate in all guilds it has access to.
    /// </summary>
    public string GuildIds { get; set; } = string.Empty;

    /// <summary>
    /// Whether to register slash commands globally if no GuildId is specified.
    /// Default is false for safety during development.
    /// </summary>
    public bool RegisterCommandsGlobally { get; set; }

    /// <summary>
    /// Whether to enable slash command (interaction) support.
    /// </summary>
    public bool EnableSlashCommands { get; set; } = true;

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

    // ─────────────────────────────────────────────────────────────────
    // Ticketing/Support Configuration
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Role ID for support staff who can manage tickets.
    /// </summary>
    public ulong SupportRoleId { get; set; }

    /// <summary>
    /// Category ID where ticket channels should be created.
    /// </summary>
    public ulong SupportCategoryId { get; set; }

    /// <summary>
    /// Category ID where closed/archived tickets should be moved.
    /// </summary>
    public ulong SupportArchiveCategoryId { get; set; }

    /// <summary>
    /// Channel ID for ticket intake notifications (optional).
    /// </summary>
    public ulong SupportIntakeChannelId { get; set; }

    /// <summary>
    /// Whether to post a sample ticket on bot startup (for testing).
    /// </summary>
    public bool PostSampleTicketOnStartup { get; set; }
}
