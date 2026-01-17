using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Teams.Configuration;

namespace Mystira.App.Infrastructure.Teams.Services;

/// <summary>
/// Implementation of chat bot service using Microsoft Bot Framework for Teams.
/// Implements the Application port interfaces for clean architecture compliance.
/// FIX: Added IMessagingService for consistency with DiscordBotService.
/// </summary>
public class TeamsBotService : IMessagingService, IChatBotService, IBotCommandService, IDisposable
{
    private readonly ILogger<TeamsBotService> _logger;
    private readonly TeamsOptions _options;
    private readonly MicrosoftAppCredentials _credentials;
    private bool _disposed;
    private bool _isConnected;

    // Thread-safe bidirectional mapping for conversation references
    // FIX: Use ConcurrentDictionary for thread safety (Phase 2)
    // FIX: Use bidirectional mapping to avoid hash collision issues (Phase 1)
    private readonly ConcurrentDictionary<ulong, string> _idToKey = new();
    private readonly ConcurrentDictionary<string, ConversationReference> _keyToRef = new();
    // FIX: Add reverse lookup dictionary for O(1) key-to-id lookups
    private readonly ConcurrentDictionary<string, ulong> _keyToId = new();
    private readonly object _idLock = new();

    public TeamsBotService(
        IOptions<TeamsOptions> options,
        ILogger<TeamsBotService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Initialize credentials for Bot Framework
        _credentials = new MicrosoftAppCredentials(
            _options.MicrosoftAppId,
            _options.MicrosoftAppPassword);
    }

    public bool IsConnected => _isConnected && !string.IsNullOrWhiteSpace(_options.MicrosoftAppId);

    public bool IsEnabled => _options.Enabled;

    /// <summary>
    /// Gets the platform identifier for this service.
    /// </summary>
    public ChatPlatform Platform => ChatPlatform.Teams;

    public int RegisteredModuleCount => 0; // Teams uses different command registration

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Teams bot is disabled in configuration");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_options.MicrosoftAppId))
        {
            throw new InvalidOperationException("Teams bot MicrosoftAppId is not configured. Set Teams:MicrosoftAppId in configuration.");
        }

        _logger.LogInformation("Teams bot service started");
        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Teams bot service stopped");
        _isConnected = false;
        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Teams bot is not connected");
        }

        var conversationRef = FindConversationReference(channelId);
        if (conversationRef == null)
        {
            throw new InvalidOperationException($"No conversation reference found for channel {channelId}. Teams requires prior interaction to send proactive messages.");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds));

            using var connectorClient = new ConnectorClient(
                new Uri(conversationRef.ServiceUrl),
                _credentials);

            var activity = MessageFactory.Text(message);
            activity.Conversation = conversationRef.Conversation;

            await connectorClient.Conversations.SendToConversationAsync(
                conversationRef.Conversation.Id,
                activity,
                timeoutCts.Token);

            _logger.LogDebug("Sent message to Teams conversation {ConversationId}", conversationRef.Conversation.Id);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout sending message to Teams conversation after {Timeout}s", _options.DefaultTimeoutSeconds);
            throw new TimeoutException($"Teams API request timed out after {_options.DefaultTimeoutSeconds} seconds");
        }
        catch (HttpOperationException ex)
        {
            _logger.LogError(ex, "Teams API HTTP operation failed while sending message. Status: {StatusCode}", ex.Response?.StatusCode);
            throw new InvalidOperationException($"Teams API error: {ex.Message}", ex);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error while sending message to Teams conversation");
            throw new InvalidOperationException($"Teams API validation error: {ex.Message}", ex);
        }
    }

    public async Task SendEmbedAsync(ulong channelId, EmbedData embed, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Teams bot is not connected");
        }

        var conversationRef = FindConversationReference(channelId);
        if (conversationRef == null)
        {
            throw new InvalidOperationException($"No conversation reference found for channel {channelId}");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds));

            using var connectorClient = new ConnectorClient(
                new Uri(conversationRef.ServiceUrl),
                _credentials);

            // Convert EmbedData to Teams Hero Card (color info logged as warning since HeroCard doesn't support it)
            if (embed.ColorRed != 0 || embed.ColorGreen != 0 || embed.ColorBlue != 0)
            {
                _logger.LogDebug("Teams HeroCard does not support embed colors. Color information will be ignored.");
            }

            var card = CreateHeroCardFromEmbed(embed);
            var activity = MessageFactory.Attachment(card);
            activity.Conversation = conversationRef.Conversation;

            await connectorClient.Conversations.SendToConversationAsync(
                conversationRef.Conversation.Id,
                (Activity)activity,  // Cast IMessageActivity to Activity
                timeoutCts.Token);

            _logger.LogDebug("Sent embed to Teams conversation {ConversationId}", conversationRef.Conversation.Id);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout sending embed to Teams conversation after {Timeout}s", _options.DefaultTimeoutSeconds);
            throw new TimeoutException($"Teams API request timed out after {_options.DefaultTimeoutSeconds} seconds");
        }
        catch (HttpOperationException ex)
        {
            _logger.LogError(ex, "Teams API HTTP operation failed while sending embed. Status: {StatusCode}", ex.Response?.StatusCode);
            throw new InvalidOperationException($"Teams API error: {ex.Message}", ex);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error while sending embed to Teams conversation");
            throw new InvalidOperationException($"Teams API validation error: {ex.Message}", ex);
        }
    }

    // FIX: Actually create threaded reply using ReplyToId (Phase 4)
    public async Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Teams bot is not connected");
        }

        var conversationRef = FindConversationReference(channelId);
        if (conversationRef == null)
        {
            throw new InvalidOperationException($"No conversation reference found for channel {channelId}");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds));

            using var connectorClient = new ConnectorClient(
                new Uri(conversationRef.ServiceUrl),
                _credentials);

            var activity = MessageFactory.Text(reply);
            activity.Conversation = conversationRef.Conversation;

            // Set ReplyToId to create a threaded reply if messageId is provided
            if (messageId != 0)
            {
                activity.ReplyToId = messageId.ToString();
            }

            await connectorClient.Conversations.SendToConversationAsync(
                conversationRef.Conversation.Id,
                activity,
                timeoutCts.Token);

            _logger.LogDebug("Sent reply to message {MessageId} in Teams conversation {ConversationId}",
                messageId, conversationRef.Conversation.Id);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Teams API request timed out after {_options.DefaultTimeoutSeconds} seconds");
        }
        catch (HttpOperationException ex)
        {
            _logger.LogError(ex, "Teams API HTTP operation failed while sending reply. Status: {StatusCode}", ex.Response?.StatusCode);
            throw new InvalidOperationException($"Teams API error: {ex.Message}", ex);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error while sending reply to Teams conversation");
            throw new InvalidOperationException($"Teams API validation error: {ex.Message}", ex);
        }
    }

    public BotStatus GetStatus()
    {
        return new BotStatus
        {
            IsEnabled = _options.Enabled,
            IsConnected = IsConnected,
            BotName = "Teams Bot",
            BotId = null, // Teams doesn't expose a numeric bot ID
            ServerCount = _keyToRef.Count
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Broadcast / First Responder (Limited support for Teams)
    // ─────────────────────────────────────────────────────────────────

    public async Task<FirstResponderResult> SendAndAwaitFirstResponseAsync(
        IEnumerable<ulong> channelIds,
        string message,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        // Teams doesn't support real-time message monitoring the same way Discord does
        // This is a simplified implementation that sends to all channels
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
                _logger.LogWarning(ex, "Failed to send broadcast to Teams channel {ChannelId}", channelId);
            }
        }

        _logger.LogWarning("Teams does not support real-time response monitoring. Messages sent but no response tracking available.");

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
                _logger.LogWarning(ex, "Failed to send broadcast embed to Teams channel {ChannelId}", channelId);
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
                _logger.LogWarning(ex, "Failed to send broadcast to Teams channel {ChannelId}", channelId);
            }
        }

        _logger.LogWarning("Teams does not support real-time response monitoring via this method.");
    }

    // ─────────────────────────────────────────────────────────────────
    // IBotCommandService Implementation
    // ─────────────────────────────────────────────────────────────────

    public Task RegisterCommandsAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        // Teams bot commands are typically handled via Adaptive Cards or message extensions
        // Command registration is different from Discord slash commands
        _logger.LogInformation("Teams bot command registration is not supported in the same way as Discord. Use Adaptive Cards or message extensions instead.");
        return Task.CompletedTask;
    }

    public Task RegisterCommandsToServerAsync(ulong serverId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Teams does not support server-specific command registration");
        return Task.CompletedTask;
    }

    public Task RegisterCommandsGloballyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Teams commands are registered via the Bot Framework portal, not programmatically");
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────
    // Teams-specific methods
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Store a conversation reference for proactive messaging.
    /// Call this when receiving a message from a user.
    /// Returns the channel ID that can be used to send messages to this conversation.
    /// </summary>
    public ulong AddOrUpdateConversationReference(Activity activity)
    {
        var conversationRef = activity.GetConversationReference();
        var key = GetConversationKey(conversationRef);

        // Update or add the conversation reference
        _keyToRef[key] = conversationRef;

        // Get or create a stable channel ID for this conversation
        var channelId = GetOrCreateChannelId(key);

        _logger.LogDebug("Stored conversation reference for {Key} with channelId {ChannelId}", key, channelId);
        return channelId;
    }

    /// <summary>
    /// Get a stable channel ID for a conversation reference.
    /// FIX: No longer uses GetHashCode() which could cause collisions (Phase 1)
    /// </summary>
    public ulong GetChannelIdForConversation(ConversationReference conversationRef)
    {
        var key = GetConversationKey(conversationRef);
        return GetOrCreateChannelId(key);
    }

    /// <summary>
    /// Get a stable channel ID for a conversation key, creating one if necessary.
    /// Uses deterministic ID generation to avoid hash collisions.
    /// FIX: Use reverse lookup dictionary for O(1) lookup instead of O(n) iteration
    /// FIX: Use double-checked locking to prevent race condition
    /// </summary>
    private ulong GetOrCreateChannelId(string key)
    {
        // FIX: O(1) lookup using reverse dictionary instead of O(n) iteration
        // First check without lock for fast path
        if (_keyToId.TryGetValue(key, out var existingId))
        {
            return existingId;
        }

        // FIX: Use lock for creation to prevent race condition where two threads
        // both pass the TryGetValue check and try to create the same mapping
        lock (_idLock)
        {
            // Double-check inside lock in case another thread already created it
            if (_keyToId.TryGetValue(key, out existingId))
            {
                return existingId;
            }

            // Generate a new stable ID using deterministic hash
            // FIX: Use SHA256 for deterministic, collision-resistant ID generation (Phase 1)
            var channelId = GenerateDeterministicId(key);

            // FIX: Handle potential (unlikely) collision with bounded retry
            const int maxCollisionAttempts = 100;
            var attempts = 0;

            while (!_idToKey.TryAdd(channelId, key))
            {
                attempts++;
                if (attempts >= maxCollisionAttempts)
                {
                    throw new InvalidOperationException($"Failed to generate unique channel ID after {maxCollisionAttempts} attempts. This indicates a serious hash collision problem.");
                }
                // Use a different hash seed for retry
                channelId = GenerateDeterministicId($"{key}:{attempts}");
            }

            // Also add to reverse lookup
            _keyToId[key] = channelId;

            return channelId;
        }
    }

    /// <summary>
    /// Generate a deterministic ulong ID from a string key using SHA256.
    /// This avoids the issues with GetHashCode() returning negative values
    /// and potential collisions.
    /// </summary>
    private static ulong GenerateDeterministicId(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        // Take first 8 bytes and convert to ulong
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static string GetConversationKey(ConversationReference conversationRef)
    {
        return $"{conversationRef.ChannelId}:{conversationRef.Conversation.Id}";
    }

    private ConversationReference? FindConversationReference(ulong channelId)
    {
        // FIX: Use bidirectional mapping for O(1) lookup without hash collision risk (Phase 1)
        if (_idToKey.TryGetValue(channelId, out var key) && _keyToRef.TryGetValue(key, out var conversationRef))
        {
            return conversationRef;
        }
        return null;
    }

    private Attachment CreateHeroCardFromEmbed(EmbedData embed)
    {
        var card = new HeroCard
        {
            Title = embed.Title,
            Text = embed.Description
        };

        if (embed.Fields != null && embed.Fields.Any())
        {
            // Add fields as formatted text
            var fieldsText = string.Join("\n\n",
                embed.Fields.Select(f => $"**{f.Name}**\n{f.Value}"));
            card.Text = $"{embed.Description}\n\n{fieldsText}";
        }

        if (!string.IsNullOrEmpty(embed.Footer))
        {
            card.Subtitle = embed.Footer;
        }

        return card.ToAttachment();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _isConnected = false;
        _idToKey.Clear();
        _keyToRef.Clear();
        _keyToId.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
