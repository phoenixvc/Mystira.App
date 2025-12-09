namespace Mystira.App.Infrastructure.WhatsApp.Configuration;

/// <summary>
/// Configuration options for WhatsApp integration via Azure Communication Services.
/// </summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>
    /// Whether the WhatsApp integration is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Azure Communication Services connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// WhatsApp Channel ID from Azure Communication Services
    /// </summary>
    public string ChannelRegistrationId { get; set; } = string.Empty;

    /// <summary>
    /// WhatsApp phone number ID (from Meta Business Suite)
    /// </summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>
    /// Default timeout for operations in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to log all incoming/outgoing messages
    /// </summary>
    public bool LogAllMessages { get; set; }

    /// <summary>
    /// WhatsApp Business Account ID
    /// </summary>
    public string BusinessAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Webhook URL for receiving incoming messages
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Webhook verification token
    /// </summary>
    public string WebhookVerifyToken { get; set; } = string.Empty;
}
