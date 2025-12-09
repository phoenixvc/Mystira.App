namespace Mystira.App.Infrastructure.Teams.Configuration;

/// <summary>
/// Configuration options for Microsoft Teams bot integration.
/// </summary>
public class TeamsOptions
{
    public const string SectionName = "Teams";

    /// <summary>
    /// Whether the Teams bot is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Microsoft App ID for the bot (from Azure Bot registration)
    /// </summary>
    public string MicrosoftAppId { get; set; } = string.Empty;

    /// <summary>
    /// Microsoft App Password for the bot (from Azure Bot registration)
    /// </summary>
    public string MicrosoftAppPassword { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Tenant ID (for single-tenant bots)
    /// Leave empty for multi-tenant bots
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Whether to enable adaptive card support
    /// </summary>
    public bool EnableAdaptiveCards { get; set; } = true;

    /// <summary>
    /// Default timeout for bot operations in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to log all incoming activities
    /// </summary>
    public bool LogAllActivities { get; set; }
}
