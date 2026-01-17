using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Communication.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.WhatsApp.Configuration;

namespace Mystira.App.Infrastructure.WhatsApp.Services;

/// <summary>
/// Implementation of chat bot service using Azure Communication Services for WhatsApp.
/// Implements the Application port interfaces for clean architecture compliance.
/// FIX: Added IMessagingService for consistency with DiscordBotService.
///
/// Note: WhatsApp has specific limitations:
/// - Messages can only be sent to users who have initiated a conversation (24-hour window)
/// - Outside the 24-hour window, only template messages can be sent
/// - No concept of "channels" like Discord/Teams - messages are to phone numbers
/// </summary>
public class WhatsAppBotService : IMessagingService, IChatBotService, IBotCommandService, IDisposable
{
    private readonly ILogger<WhatsAppBotService> _logger;
    private readonly WhatsAppOptions _options;
    private NotificationMessagesClient? _client;
    private bool _disposed;
    private bool _isConnected;

    // Track active conversations (phone numbers that have messaged us recently)
    // FIX: Use ConcurrentDictionary for thread safety (Phase 2)
    private readonly ConcurrentDictionary<ulong, string> _activeConversations = new();

    public WhatsAppBotService(
        IOptions<WhatsAppOptions> options,
        ILogger<WhatsAppBotService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected => _isConnected && _client != null;

    public bool IsEnabled => _options.Enabled;

    /// <summary>
    /// Gets the platform identifier for this service.
    /// </summary>
    public ChatPlatform Platform => ChatPlatform.WhatsApp;

    public int RegisteredModuleCount => 0; // WhatsApp doesn't have command modules like Discord

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("WhatsApp integration is disabled in configuration");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("WhatsApp ConnectionString is not configured. Set WhatsApp:ConnectionString in configuration.");
        }

        if (string.IsNullOrWhiteSpace(_options.ChannelRegistrationId))
        {
            throw new InvalidOperationException("WhatsApp ChannelRegistrationId is not configured. Set WhatsApp:ChannelRegistrationId in configuration.");
        }

        try
        {
            _client = new NotificationMessagesClient(_options.ConnectionString);
            _isConnected = true;
            _logger.LogInformation("WhatsApp bot service started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize WhatsApp client");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp bot service stopped");
        _isConnected = false;
        _client = null;
        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _client == null)
        {
            throw new InvalidOperationException("WhatsApp bot is not connected");
        }

        var phoneNumber = GetPhoneNumberFromChannelId(channelId);
        if (string.IsNullOrEmpty(phoneNumber))
        {
            throw new InvalidOperationException($"No phone number found for channel ID {channelId}. WhatsApp requires a registered conversation.");
        }

        // Azure.Communication.Messages 1.1.0+ requires Guid for channel registration ID
        if (!Guid.TryParse(_options.ChannelRegistrationId, out var channelGuid))
        {
            throw new InvalidOperationException($"Invalid channel registration ID format: {_options.ChannelRegistrationId}");
        }

        var textContent = new TextNotificationContent(
            channelGuid,
            new[] { phoneNumber },
            message);

        // FIX: Add retry logic for transient failures (Phase 6)
        var attempt = 0;
        var maxAttempts = _options.MaxRetryAttempts > 0 ? _options.MaxRetryAttempts : 3;

        while (true)
        {
            attempt++;
            try
            {
                var result = await _client.SendAsync(textContent, cancellationToken);

                _logger.LogDebug("Sent WhatsApp message to {PhoneNumber}, MessageId: {MessageId}",
                    phoneNumber, result.Value.Receipts.FirstOrDefault()?.MessageId);
                return;
            }
            catch (RequestFailedException ex) when (IsTransientError(ex) && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                _logger.LogWarning(ex, "Transient error sending WhatsApp message (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Communication Services error sending WhatsApp message: {Code}", ex.ErrorCode);
                throw new InvalidOperationException($"WhatsApp API error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending WhatsApp message");
                throw;
            }
        }
    }

    /// <summary>
    /// Determine if a RequestFailedException is transient and can be retried.
    /// </summary>
    private static bool IsTransientError(RequestFailedException ex)
    {
        // HTTP 429 (Too Many Requests), 5xx errors are typically transient
        return ex.Status == 429 || (ex.Status >= 500 && ex.Status < 600);
    }

    public async Task SendEmbedAsync(ulong channelId, EmbedData embed, CancellationToken cancellationToken = default)
    {
        // WhatsApp doesn't support rich embeds like Discord
        // FIX: Add warning for embed color being ignored (Phase 4)
        if (embed.ColorRed != 0 || embed.ColorGreen != 0 || embed.ColorBlue != 0)
        {
            _logger.LogDebug("WhatsApp does not support embed colors. Color information will be ignored.");
        }

        // Convert to formatted text message
        var messageText = FormatEmbedAsText(embed);
        await SendMessageAsync(channelId, messageText, cancellationToken);
    }

    public async Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default)
    {
        // WhatsApp via ACS doesn't support threaded replies in the same way
        // Just send a regular message
        await SendMessageAsync(channelId, reply, cancellationToken);
    }

    public BotStatus GetStatus()
    {
        return new BotStatus
        {
            IsEnabled = _options.Enabled,
            IsConnected = IsConnected,
            BotName = "WhatsApp Bot",
            BotId = null,
            ServerCount = _activeConversations.Count // Number of active conversations
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Broadcast / First Responder (Limited support for WhatsApp)
    // ─────────────────────────────────────────────────────────────────

    public async Task<FirstResponderResult> SendAndAwaitFirstResponseAsync(
        IEnumerable<ulong> channelIds,
        string message,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var sentMessages = new List<SentMessage>();

        foreach (var channelId in channelIds)
        {
            try
            {
                await SendMessageAsync(channelId, message, cancellationToken);
                sentMessages.Add(new SentMessage { ChannelId = channelId, MessageId = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send broadcast to WhatsApp number {ChannelId}", channelId);
            }
        }

        _logger.LogWarning("WhatsApp does not support real-time response monitoring via this method. Use webhooks instead.");

        return new FirstResponderResult
        {
            TimedOut = true,
            SentMessages = sentMessages
        };
    }

    public async Task<FirstResponderResult> SendEmbedAndAwaitFirstResponseAsync(
        IEnumerable<ulong> channelIds,
        EmbedData embed,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var sentMessages = new List<SentMessage>();

        foreach (var channelId in channelIds)
        {
            try
            {
                await SendEmbedAsync(channelId, embed, cancellationToken);
                sentMessages.Add(new SentMessage { ChannelId = channelId, MessageId = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send broadcast embed to WhatsApp number {ChannelId}", channelId);
            }
        }

        return new FirstResponderResult
        {
            TimedOut = true,
            SentMessages = sentMessages
        };
    }

    public async Task BroadcastWithResponseHandlerAsync(
        IEnumerable<ulong> channelIds,
        string message,
        Func<ResponseEvent, Task<bool>> onResponse,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        foreach (var channelId in channelIds)
        {
            try
            {
                await SendMessageAsync(channelId, message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send broadcast to WhatsApp number {ChannelId}", channelId);
            }
        }

        _logger.LogWarning("WhatsApp does not support real-time response monitoring via this method.");
    }

    // ─────────────────────────────────────────────────────────────────
    // IBotCommandService Implementation
    // ─────────────────────────────────────────────────────────────────

    public Task RegisterCommandsAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp does not support slash commands. Handle commands via message parsing.");
        return Task.CompletedTask;
    }

    public Task RegisterCommandsToServerAsync(ulong serverId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("WhatsApp does not support server-specific command registration");
        return Task.CompletedTask;
    }

    public Task RegisterCommandsGloballyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp commands should be handled via message content parsing");
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────
    // WhatsApp-specific methods
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Register an active conversation from an incoming webhook message.
    /// Call this when receiving a message to enable proactive messaging.
    /// </summary>
    /// <param name="phoneNumber">The phone number that sent the message (with country code)</param>
    /// <returns>A channel ID that can be used for sending messages</returns>
    public ulong RegisterConversation(string phoneNumber)
    {
        var channelId = GetChannelIdFromPhoneNumber(phoneNumber);
        _activeConversations[channelId] = phoneNumber;
        _logger.LogDebug("Registered WhatsApp conversation: {PhoneNumber} -> {ChannelId}", phoneNumber, channelId);
        return channelId;
    }

    /// <summary>
    /// Get a channel ID from a phone number.
    /// FIX: Use SHA256 for deterministic, collision-resistant ID generation (Phase 2)
    /// </summary>
    public static ulong GetChannelIdFromPhoneNumber(string phoneNumber)
    {
        // Create a consistent hash from the phone number
        var normalized = phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "");

        // Try to parse as numeric first (most phone numbers are all digits)
        if (ulong.TryParse(normalized, out var result))
        {
            return result;
        }

        // Fall back to SHA256 hash for non-numeric formats
        // FIX: Use SHA256 instead of GetHashCode() to avoid negative values and collisions
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return BitConverter.ToUInt64(bytes, 0);
    }

    /// <summary>
    /// Send a template message (required for messages outside 24-hour window).
    /// </summary>
    public async Task SendTemplateMessageAsync(
        string phoneNumber,
        string templateName,
        string templateLanguage = "en",
        IEnumerable<string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _client == null)
        {
            throw new InvalidOperationException("WhatsApp bot is not connected");
        }

        try
        {
            // Azure.Communication.Messages 1.1.0+ requires Guid for channel registration ID
            if (!Guid.TryParse(_options.ChannelRegistrationId, out var channelGuid))
            {
                throw new InvalidOperationException($"Invalid channel registration ID format: {_options.ChannelRegistrationId}");
            }

            // In version 1.1.0+, template API has changed
            var template = new MessageTemplate(templateName, templateLanguage == "en" ? "en_US" : templateLanguage);

            // Update: Template parameters handling for Azure SDK 1.1.0+
            if (parameters != null && parameters.Any())
            {
                foreach (var param in parameters)
                {
                    // Azure SDK 1.1.0+ uses MessageTemplateText for text parameters
                    template.Values.Add(new MessageTemplateText("body", param));
                }
            }

            var templateContent = new TemplateNotificationContent(
                channelGuid,
                new[] { phoneNumber },
                template);

            await _client.SendAsync(templateContent, cancellationToken);

            _logger.LogDebug("Sent WhatsApp template message to {PhoneNumber}", phoneNumber);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services error sending WhatsApp template: {Code}", ex.ErrorCode);
            throw new InvalidOperationException($"WhatsApp API error: {ex.Message}", ex);
        }
    }

    private string? GetPhoneNumberFromChannelId(ulong channelId)
    {
        return _activeConversations.TryGetValue(channelId, out var phoneNumber)
            ? phoneNumber
            : null;
    }

    private static string FormatEmbedAsText(EmbedData embed)
    {
        var lines = new List<string>();

        if (!string.IsNullOrEmpty(embed.Title))
        {
            lines.Add($"*{embed.Title}*");
        }

        if (!string.IsNullOrEmpty(embed.Description))
        {
            lines.Add(embed.Description);
        }

        if (embed.Fields != null)
        {
            lines.Add("");
            foreach (var field in embed.Fields)
            {
                lines.Add($"*{field.Name}*");
                lines.Add(field.Value);
                lines.Add("");
            }
        }

        if (!string.IsNullOrEmpty(embed.Footer))
        {
            lines.Add($"_{embed.Footer}_");
        }

        return string.Join("\n", lines);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _isConnected = false;
        _activeConversations.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
