using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Discord.Configuration;

namespace Mystira.App.Infrastructure.Discord.Services;

/// <summary>
/// Implementation of Discord bot service using Discord.NET.
/// Implements the Application port interfaces for clean architecture compliance.
/// Supports both messaging and slash commands (interactions).
/// </summary>
public class DiscordBotService : IMessagingService, IChatBotService, IBotCommandService, IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly DiscordOptions _options;
    private bool _disposed;
    private bool _commandsRegistered;

    public DiscordBotService(
        IOptions<DiscordOptions> options,
        ILogger<DiscordBotService> logger,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Configure Discord client with appropriate intents
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages,
            AlwaysDownloadUsers = false,
            MessageCacheSize = 100,
            DefaultRetryMode = RetryMode.AlwaysRetry,
            ConnectionTimeout = _options.DefaultTimeoutSeconds * 1000
        };

        // Add message content intent if enabled (required for reading message content)
        if (_options.EnableMessageContentIntent)
        {
            config.GatewayIntents |= GatewayIntents.MessageContent;
        }

        // Add guild members intent if enabled
        if (_options.EnableGuildMembersIntent)
        {
            config.GatewayIntents |= GatewayIntents.GuildMembers;
        }

        _client = new DiscordSocketClient(config);
        _interactions = new InteractionService(_client.Rest);

        // Wire up event handlers
        _client.Log += LogAsync;
        _interactions.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.Disconnected += DisconnectedAsync;
        _client.InteractionCreated += HandleInteractionAsync;
    }

    public bool IsConnected => _client.ConnectionState == ConnectionState.Connected;

    /// <summary>
    /// Gets the platform identifier for this service.
    /// </summary>
    public ChatPlatform Platform => ChatPlatform.Discord;

    /// <summary>
    /// Internal access to Discord client for Infrastructure layer use only.
    /// Do not expose Discord.NET types outside this assembly.
    /// </summary>
    internal DiscordSocketClient Client => _client;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            throw new InvalidOperationException("Discord bot token is not configured. Set Discord:BotToken in configuration.");
        }

        try
        {
            _logger.LogInformation("Starting Discord bot...");

            await _client.LoginAsync(TokenType.Bot, _options.BotToken);
            await _client.StartAsync();

            _logger.LogInformation("Discord bot started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Discord bot");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping Discord bot...");

            await _client.StopAsync();
            await _client.LogoutAsync();

            _logger.LogInformation("Discord bot stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Discord bot");
            throw;
        }
    }

    public async Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken = default)
    {
        var channel = await GetMessageChannelAsync(channelId);

        await ExecuteWithRetryAsync(
            async () =>
            {
                await channel.SendMessageAsync(message);
                _logger.LogDebug("Sent message to channel {ChannelId}", channelId);
            },
            $"sending message to channel {channelId}",
            cancellationToken);
    }

    public async Task SendEmbedAsync(ulong channelId, Embed embed, CancellationToken cancellationToken = default)
    {
        var channel = await GetMessageChannelAsync(channelId);

        await ExecuteWithRetryAsync(
            async () =>
            {
                await channel.SendMessageAsync(embed: embed);
                _logger.LogDebug("Sent embed to channel {ChannelId}", channelId);
            },
            $"sending embed to channel {channelId}",
            cancellationToken);
    }

    public async Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default)
    {
        var channel = await GetMessageChannelAsync(channelId);
        var message = await channel.GetMessageAsync(messageId);

        if (message == null)
        {
            throw new InvalidOperationException($"Message {messageId} not found in channel {channelId}");
        }

        await ExecuteWithRetryAsync(
            async () =>
            {
                await channel.SendMessageAsync(reply, messageReference: new MessageReference(messageId));
                _logger.LogDebug("Replied to message {MessageId} in channel {ChannelId}", messageId, channelId);
            },
            $"replying to message {messageId} in channel {channelId}",
            cancellationToken);
    }

    /// <summary>
    /// Executes an action with retry logic for Discord rate limiting.
    /// DRY: Extracted from SendMessageAsync, SendEmbedAsync, and ReplyToMessageAsync.
    /// </summary>
    private async Task ExecuteWithRetryAsync(
        Func<Task> action,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        var maxAttempts = _options.MaxRetryAttempts > 0 ? _options.MaxRetryAttempts : 3;
        var attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                await action();
                return;
            }
            catch (global::Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.TooManyRequests && attempt < maxAttempts)
            {
                // Discord.Net 3.17.1+ doesn't expose RetryAfter, use exponential backoff
                var retryAfter = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Rate limited during {Operation}, retrying in {RetryAfter}s (attempt {Attempt}/{MaxAttempts})",
                    operationDescription, retryAfter.TotalSeconds, attempt, maxAttempts);
                await Task.Delay(retryAfter, cancellationToken);
            }
            catch (global::Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(ex, "Rate limited during {Operation} after {Attempts} attempts", operationDescription, attempt);
                throw new InvalidOperationException($"Rate limited after {attempt} attempts: {ex.Message}", ex);
            }
            catch (global::Discord.Net.HttpException ex)
            {
                _logger.LogError(ex, "HTTP error during {Operation}: {StatusCode}", operationDescription, ex.HttpCode);
                throw new InvalidOperationException($"Discord API error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {Operation}", operationDescription);
                throw;
            }
        }
    }

    private Task<IMessageChannel> GetMessageChannelAsync(ulong channelId)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Discord bot is not connected");
        }

        var channel = _client.GetChannel(channelId) as IMessageChannel;
        if (channel == null)
        {
            throw new InvalidOperationException($"Channel {channelId} not found or is not a message channel");
        }

        return Task.FromResult(channel);
    }

    private Task LogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, log.Exception, "[Discord.NET] {Source}: {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        _logger.LogInformation("Discord bot is ready! Logged in as {Username}",
            _client.CurrentUser?.Username);

        // Auto-register commands if configured
        if (_options.EnableSlashCommands && !_commandsRegistered)
        {
            try
            {
                if (_options.GuildId != 0)
                {
                    await RegisterCommandsToServerAsync(_options.GuildId);
                    _logger.LogInformation("Slash commands registered to server {ServerId}", _options.GuildId);
                }
                else if (_options.RegisterCommandsGlobally)
                {
                    await RegisterCommandsGloballyAsync();
                    _logger.LogInformation("Slash commands registered globally");
                }
                else
                {
                    _logger.LogWarning("Slash commands enabled but no GuildId/ServerId configured and RegisterCommandsGlobally is false");
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException && ex is not AccessViolationException)
            {
                _logger.LogError(ex, "Failed to register slash commands");
            }
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(context, _serviceProvider);
        }
        catch (Exception ex) when (
            ex is not OutOfMemoryException &&
            ex is not StackOverflowException
        )
        {
            _logger.LogError(ex, "Error handling interaction");

            // If the interaction hasn't been responded to, send an error message
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                try
                {
                    await interaction.RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
                catch (Exception innerEx) when (
                    innerEx is not OutOfMemoryException &&
                    innerEx is not StackOverflowException &&
                    innerEx is not AccessViolationException
                )
                {
                    // Interaction may have already been responded to
                    _logger.LogError(innerEx, "Failed to send error response to interaction");
                }
            }
        }
    }

    private Task MessageReceivedAsync(SocketMessage message)
    {
        // Ignore messages from bots (including self)
        if (message.Author.IsBot)
        {
            return Task.CompletedTask;
        }

        if (_options.LogAllMessages)
        {
            _logger.LogDebug("Message received from {Author} in {Channel}: {Content}",
                message.Author.Username,
                message.Channel.Name,
                message.Content);
        }

        // Message handling logic can be extended here or in derived classes
        // For now, this is a hook for future message processing

        return Task.CompletedTask;
    }

    private Task DisconnectedAsync(Exception exception)
    {
        _logger.LogWarning(exception, "Discord bot disconnected");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _interactions?.Dispose();
        _client?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Send an embed using platform-agnostic EmbedData (implements IChatBotService)
    /// </summary>
    public async Task SendEmbedAsync(ulong channelId, EmbedData embedData, CancellationToken cancellationToken = default)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle(embedData.Title)
            .WithDescription(embedData.Description)
            .WithColor(new Color(embedData.ColorRed, embedData.ColorGreen, embedData.ColorBlue));

        if (!string.IsNullOrEmpty(embedData.Footer))
        {
            embedBuilder.WithFooter(embedData.Footer);
        }

        if (embedData.Fields != null)
        {
            foreach (var field in embedData.Fields)
            {
                embedBuilder.AddField(field.Name, field.Value, field.Inline);
            }
        }

        await SendEmbedAsync(channelId, embedBuilder.Build(), cancellationToken);
    }

    /// <summary>
    /// Get bot status information (implements IChatBotService)
    /// </summary>
    public BotStatus GetStatus()
    {
        return new BotStatus
        {
            IsEnabled = !string.IsNullOrWhiteSpace(_options.BotToken),
            IsConnected = IsConnected,
            BotName = _client.CurrentUser?.Username,
            BotId = _client.CurrentUser?.Id,
            ServerCount = _client.Guilds.Count
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // IBotCommandService Implementation
    // ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public bool IsEnabled => _options.EnableSlashCommands;

    /// <inheritdoc />
    public int RegisteredModuleCount => _interactions.Modules.Count;

    /// <inheritdoc />
    public async Task RegisterCommandsAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableSlashCommands)
        {
            _logger.LogWarning("Slash commands are disabled in configuration");
            return;
        }

        try
        {
            await _interactions.AddModulesAsync(assembly, _serviceProvider);
            _logger.LogInformation("Registered command modules from assembly {Assembly}", assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register command modules from assembly {Assembly}", assembly.GetName().Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RegisterCommandsToServerAsync(ulong serverId, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableSlashCommands)
        {
            _logger.LogWarning("Slash commands are disabled in configuration");
            return;
        }

        try
        {
            // Discord uses "guild" terminology, but the interface uses "server" for platform-agnosticism
            await _interactions.RegisterCommandsToGuildAsync(serverId);
            _commandsRegistered = true;
            _logger.LogInformation("Registered {Count} slash commands to server {ServerId}",
                _interactions.SlashCommands.Count, serverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register commands to server {ServerId}", serverId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RegisterCommandsGloballyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableSlashCommands)
        {
            _logger.LogWarning("Slash commands are disabled in configuration");
            return;
        }

        try
        {
            await _interactions.RegisterCommandsGloballyAsync();
            _commandsRegistered = true;
            _logger.LogInformation("Registered {Count} slash commands globally",
                _interactions.SlashCommands.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register commands globally");
            throw;
        }
    }

    /// <summary>
    /// Internal access to InteractionService for Infrastructure layer use.
    /// </summary>
    internal InteractionService Interactions => _interactions;

    // ─────────────────────────────────────────────────────────────────
    // Broadcast / First Responder Implementation
    // ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<FirstResponderResult> SendAndAwaitFirstResponseAsync(
        IEnumerable<ulong> channelIds,
        string message,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Discord bot is not connected");
        }

        var tcs = new TaskCompletionSource<FirstResponderResult>();
        var sentMessages = new List<SentMessage>();
        var startTime = DateTime.UtcNow;
        var channelList = channelIds.ToList();

        _logger.LogInformation("Broadcasting message to {ChannelCount} channels", channelList.Count);

        // Send messages to all channels
        foreach (var channelId in channelList)
        {
            try
            {
                var channel = _client.GetChannel(channelId) as IMessageChannel;
                if (channel != null)
                {
                    var sentMsg = await channel.SendMessageAsync(message);
                    sentMessages.Add(new SentMessage { ChannelId = channelId, MessageId = sentMsg.Id });
                    _logger.LogDebug("Broadcast sent to channel {ChannelId}", channelId);
                }
                else
                {
                    _logger.LogWarning("Channel {ChannelId} not found or not a message channel", channelId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send broadcast to channel {ChannelId}", channelId);
            }
        }

        if (sentMessages.Count == 0)
        {
            return new FirstResponderResult
            {
                TimedOut = true,
                SentMessages = sentMessages
            };
        }

        var sentChannelIds = sentMessages.Select(m => m.ChannelId).ToHashSet();

        // Handler for incoming messages
        Task OnMessageReceived(SocketMessage msg)
        {
            // Ignore bot messages
            if (msg.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            // Check if message is in one of our broadcast channels
            if (!sentChannelIds.Contains(msg.Channel.Id))
            {
                return Task.CompletedTask;
            }

            // Accept either direct replies or any message in the broadcast channels
            tcs.TrySetResult(new FirstResponderResult
            {
                TimedOut = false,
                RespondingChannelId = msg.Channel.Id,
                RespondingChannelName = msg.Channel.Name,
                ResponseMessageId = msg.Id,
                ResponseContent = msg.Content,
                ResponderId = msg.Author.Id,
                ResponderName = msg.Author.Username,
                ResponseTime = DateTime.UtcNow - startTime,
                SentMessages = sentMessages
            });

            return Task.CompletedTask;
        }

        _client.MessageReceived += OnMessageReceived;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(Timeout.Infinite, cts.Token)
            ).ConfigureAwait(false);

            if (completedTask == tcs.Task)
            {
                var result = await tcs.Task;
                _logger.LogInformation("First response received from {Responder} in channel {Channel} after {Time}ms",
                    result.ResponderName, result.RespondingChannelName, result.ResponseTime.TotalMilliseconds);
                return result;
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation
        }
        finally
        {
            _client.MessageReceived -= OnMessageReceived;
        }

        _logger.LogInformation("Broadcast timed out after {Timeout}ms with no response", timeout.TotalMilliseconds);
        return new FirstResponderResult
        {
            TimedOut = true,
            SentMessages = sentMessages
        };
    }

    /// <inheritdoc />
    public async Task<FirstResponderResult> SendEmbedAndAwaitFirstResponseAsync(
        IEnumerable<ulong> channelIds,
        EmbedData embedData,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Discord bot is not connected");
        }

        var embed = new EmbedBuilder()
            .WithTitle(embedData.Title)
            .WithDescription(embedData.Description)
            .WithColor(new Color(embedData.ColorRed, embedData.ColorGreen, embedData.ColorBlue));

        if (!string.IsNullOrEmpty(embedData.Footer))
        {
            embed.WithFooter(embedData.Footer);
        }

        if (embedData.Fields != null)
        {
            foreach (var field in embedData.Fields)
            {
                embed.AddField(field.Name, field.Value, field.Inline);
            }
        }

        var builtEmbed = embed.Build();
        var tcs = new TaskCompletionSource<FirstResponderResult>();
        var sentMessages = new List<SentMessage>();
        var startTime = DateTime.UtcNow;
        var channelList = channelIds.ToList();

        _logger.LogInformation("Broadcasting embed to {ChannelCount} channels", channelList.Count);

        // Send embeds to all channels
        foreach (var channelId in channelList)
        {
            try
            {
                var channel = _client.GetChannel(channelId) as IMessageChannel;
                if (channel != null)
                {
                    var sentMsg = await channel.SendMessageAsync(embed: builtEmbed);
                    sentMessages.Add(new SentMessage { ChannelId = channelId, MessageId = sentMsg.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send broadcast embed to channel {ChannelId}", channelId);
            }
        }

        if (sentMessages.Count == 0)
        {
            return new FirstResponderResult { TimedOut = true, SentMessages = sentMessages };
        }

        var sentChannelIds = sentMessages.Select(m => m.ChannelId).ToHashSet();

        Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Author.IsBot || !sentChannelIds.Contains(msg.Channel.Id))
            {
                return Task.CompletedTask;
            }

            tcs.TrySetResult(new FirstResponderResult
            {
                TimedOut = false,
                RespondingChannelId = msg.Channel.Id,
                RespondingChannelName = msg.Channel.Name,
                ResponseMessageId = msg.Id,
                ResponseContent = msg.Content,
                ResponderId = msg.Author.Id,
                ResponderName = msg.Author.Username,
                ResponseTime = DateTime.UtcNow - startTime,
                SentMessages = sentMessages
            });
            return Task.CompletedTask;
        }

        _client.MessageReceived += OnMessageReceived;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(Timeout.Infinite, cts.Token)
            ).ConfigureAwait(false);

            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Broadcast operation was canceled (timeout or cancellation requested).");
        }
        finally
        {
            _client.MessageReceived -= OnMessageReceived;
        }

        return new FirstResponderResult { TimedOut = true, SentMessages = sentMessages };
    }

    /// <inheritdoc />
    public async Task BroadcastWithResponseHandlerAsync(
        IEnumerable<ulong> channelIds,
        string message,
        Func<ResponseEvent, Task<bool>> onResponse,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Discord bot is not connected");
        }

        var sentMessages = new List<SentMessage>();
        var startTime = DateTime.UtcNow;
        var channelList = channelIds.ToList();
        // FIX: Use int with Interlocked for thread-safe flag (Phase 3)
        var stopListening = 0; // 0 = false, 1 = true

        _logger.LogInformation("Broadcasting with handler to {ChannelCount} channels", channelList.Count);

        // Send messages to all channels
        foreach (var channelId in channelList)
        {
            try
            {
                var channel = _client.GetChannel(channelId) as IMessageChannel;
                if (channel != null)
                {
                    var sentMsg = await channel.SendMessageAsync(message);
                    sentMessages.Add(new SentMessage { ChannelId = channelId, MessageId = sentMsg.Id });
                }
            }
            catch (global::Discord.Net.HttpException ex)
            {
                _logger.LogWarning(ex, "Discord API error sending broadcast to channel {ChannelId}: {StatusCode}", channelId, ex.HttpCode);
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout sending broadcast to channel {ChannelId}", channelId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation sending broadcast to channel {ChannelId}", channelId);
            }
        }

        if (sentMessages.Count == 0)
        {
            return;
        }

        var sentMessageIds = sentMessages.Select(m => m.MessageId).ToHashSet();
        var sentChannelIds = sentMessages.Select(m => m.ChannelId).ToHashSet();

        async Task OnMessageReceived(SocketMessage msg)
        {
            // FIX: Use Interlocked.CompareExchange to prevent race condition (Phase 3)
            // Only process if we haven't stopped listening yet
            if (Interlocked.CompareExchange(ref stopListening, 0, 0) == 1 || msg.Author.IsBot || !sentChannelIds.Contains(msg.Channel.Id))
            {
                return;
            }

            var isDirectReply = msg.Reference?.MessageId is { } replyToId && sentMessageIds.Contains(replyToId.Value);

            var responseEvent = new ResponseEvent
            {
                ChannelId = msg.Channel.Id,
                ChannelName = msg.Channel.Name,
                MessageId = msg.Id,
                Content = msg.Content,
                ResponderId = msg.Author.Id,
                ResponderName = msg.Author.Username,
                ElapsedTime = DateTime.UtcNow - startTime,
                ReplyToMessageId = msg.Reference?.MessageId.GetValueOrDefault(),
                IsDirectReply = isDirectReply
            };

            try
            {
                var shouldStop = await onResponse(responseEvent);
                if (shouldStop)
                {
                    // FIX: Atomically set stopListening to prevent multiple handler invocations (Phase 3)
                    Interlocked.Exchange(ref stopListening, 1);
                    _logger.LogDebug("Handler requested to stop listening after response from {Responder}",
                        responseEvent.ResponderName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in response handler");
            }
        }

        _client.MessageReceived += OnMessageReceived;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            // Wait for timeout or cancellation while handler processes responses
            // FIX: Use Interlocked read for thread-safe check (Phase 3)
            while (Interlocked.CompareExchange(ref stopListening, 0, 0) == 0 && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(100, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Broadcast handler finished (timeout or cancellation)");
        }
        finally
        {
            _client.MessageReceived -= OnMessageReceived;
        }
    }
}
