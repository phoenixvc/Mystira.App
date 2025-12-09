using System.Reflection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Teams.Configuration;

namespace Mystira.App.Infrastructure.Teams.Services;

/// <summary>
/// Implementation of chat bot service using Microsoft Bot Framework for Teams.
/// Implements the Application port interfaces for clean architecture compliance.
/// </summary>
public class TeamsBotService : IChatBotService, IBotCommandService, IDisposable
{
    private readonly ILogger<TeamsBotService> _logger;
    private readonly TeamsOptions _options;
    private readonly MicrosoftAppCredentials _credentials;
    private bool _disposed;
    private bool _isConnected;

    // Track conversation references for proactive messaging
    private readonly Dictionary<string, ConversationReference> _conversationReferences = new();

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

        // Teams uses conversation references for proactive messaging
        // The channelId is expected to be a hash of the conversation reference key
        var conversationRef = FindConversationReference(channelId);
        if (conversationRef == null)
        {
            throw new InvalidOperationException($"No conversation reference found for channel {channelId}. Teams requires prior interaction to send proactive messages.");
        }

        try
        {
            var connectorClient = new ConnectorClient(
                new Uri(conversationRef.ServiceUrl),
                _credentials);

            var activity = MessageFactory.Text(message);
            activity.Conversation = conversationRef.Conversation;

            await connectorClient.Conversations.SendToConversationAsync(
                conversationRef.Conversation.Id,
                activity,
                cancellationToken);

            _logger.LogDebug("Sent message to Teams conversation {ConversationId}", conversationRef.Conversation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Teams conversation");
            throw new InvalidOperationException($"Teams API error: {ex.Message}", ex);
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
            var connectorClient = new ConnectorClient(
                new Uri(conversationRef.ServiceUrl),
                _credentials);

            // Convert EmbedData to Teams Adaptive Card or Hero Card
            var card = CreateHeroCardFromEmbed(embed);
            var activity = MessageFactory.Attachment(card);
            activity.Conversation = conversationRef.Conversation;

            await connectorClient.Conversations.SendToConversationAsync(
                conversationRef.Conversation.Id,
                activity,
                cancellationToken);

            _logger.LogDebug("Sent embed to Teams conversation {ConversationId}", conversationRef.Conversation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send embed to Teams conversation");
            throw new InvalidOperationException($"Teams API error: {ex.Message}", ex);
        }
    }

    public async Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default)
    {
        // For Teams, we send a reply in the same conversation
        // The messageId can be used to set up a reply chain
        await SendMessageAsync(channelId, reply, cancellationToken);
    }

    public BotStatus GetStatus()
    {
        return new BotStatus
        {
            IsEnabled = _options.Enabled,
            IsConnected = IsConnected,
            BotName = "Teams Bot",
            BotId = null, // Teams doesn't expose a numeric bot ID
            ServerCount = _conversationReferences.Count
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
    /// </summary>
    public void AddOrUpdateConversationReference(Activity activity)
    {
        var conversationRef = activity.GetConversationReference();
        var key = GetConversationKey(conversationRef);
        _conversationReferences[key] = conversationRef;
        _logger.LogDebug("Stored conversation reference for {Key}", key);
    }

    /// <summary>
    /// Get a channel ID hash for a conversation reference.
    /// </summary>
    public ulong GetChannelIdForConversation(ConversationReference conversationRef)
    {
        var key = GetConversationKey(conversationRef);
        return (ulong)key.GetHashCode();
    }

    private string GetConversationKey(ConversationReference conversationRef)
    {
        return $"{conversationRef.ChannelId}:{conversationRef.Conversation.Id}";
    }

    private ConversationReference? FindConversationReference(ulong channelId)
    {
        // Find by hash match
        foreach (var kvp in _conversationReferences)
        {
            if ((ulong)kvp.Key.GetHashCode() == channelId)
            {
                return kvp.Value;
            }
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
        if (_disposed) return;
        _conversationReferences.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
