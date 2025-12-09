using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
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
public class DiscordBotService : IMessagingService, Application.Ports.Messaging.IDiscordBotService, ISlashCommandService, IDisposable
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
        try
        {
            var channel = await GetMessageChannelAsync(channelId);
            await channel.SendMessageAsync(message);
            _logger.LogDebug("Sent message to channel {ChannelId}", channelId);
        }
        catch (global::Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(ex, "Rate limited while sending message to channel {ChannelId}", channelId);
            throw new InvalidOperationException($"Rate limited: {ex.Message}", ex);
        }
        catch (global::Discord.Net.HttpException ex)
        {
            _logger.LogError(ex, "HTTP error sending message to channel {ChannelId}: {StatusCode}", channelId, ex.HttpCode);
            throw new InvalidOperationException($"Discord API error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending message to channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task SendEmbedAsync(ulong channelId, Embed embed, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await GetMessageChannelAsync(channelId);
            await channel.SendMessageAsync(embed: embed);
            _logger.LogDebug("Sent embed to channel {ChannelId}", channelId);
        }
        catch (global::Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(ex, "Rate limited while sending embed to channel {ChannelId}", channelId);
            throw new InvalidOperationException($"Rate limited: {ex.Message}", ex);
        }
        catch (global::Discord.Net.HttpException ex)
        {
            _logger.LogError(ex, "HTTP error sending embed to channel {ChannelId}: {StatusCode}", channelId, ex.HttpCode);
            throw new InvalidOperationException($"Discord API error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending embed to channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await GetMessageChannelAsync(channelId);
            var message = await channel.GetMessageAsync(messageId);
            
            if (message == null)
            {
                throw new InvalidOperationException($"Message {messageId} not found in channel {channelId}");
            }

            await channel.SendMessageAsync(reply, messageReference: new MessageReference(messageId));
            _logger.LogDebug("Replied to message {MessageId} in channel {ChannelId}", messageId, channelId);
        }
        catch (global::Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(ex, "Rate limited while replying to message {MessageId} in channel {ChannelId}", messageId, channelId);
            throw new InvalidOperationException($"Rate limited: {ex.Message}", ex);
        }
        catch (global::Discord.Net.HttpException ex)
        {
            _logger.LogError(ex, "HTTP error replying to message {MessageId} in channel {ChannelId}: {StatusCode}", messageId, channelId, ex.HttpCode);
            throw new InvalidOperationException($"Discord API error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error replying to message {MessageId} in channel {ChannelId}", messageId, channelId);
            throw;
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
                    await RegisterCommandsToGuildAsync(_options.GuildId);
                    _logger.LogInformation("Slash commands registered to guild {GuildId}", _options.GuildId);
                }
                else if (_options.RegisterCommandsGlobally)
                {
                    await RegisterCommandsGloballyAsync();
                    _logger.LogInformation("Slash commands registered globally");
                }
                else
                {
                    _logger.LogWarning("Slash commands enabled but no GuildId configured and RegisterCommandsGlobally is false");
                }
            }
            catch (Exception ex)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction");

            // If the interaction hasn't been responded to, send an error message
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                try
                {
                    await interaction.RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
                catch
                {
                    // Interaction may have already been responded to
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
    /// Send an embed using platform-agnostic EmbedData (implements Application.Ports.Messaging.IDiscordBotService)
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
    /// Get bot status information (implements Application.Ports.Messaging.IDiscordBotService)
    /// </summary>
    public BotStatus GetStatus()
    {
        return new BotStatus
        {
            IsEnabled = !string.IsNullOrWhiteSpace(_options.BotToken),
            IsConnected = IsConnected,
            BotName = _client.CurrentUser?.Username,
            BotId = _client.CurrentUser?.Id,
            GuildCount = _client.Guilds.Count
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // ISlashCommandService Implementation
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
    public async Task RegisterCommandsToGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableSlashCommands)
        {
            _logger.LogWarning("Slash commands are disabled in configuration");
            return;
        }

        try
        {
            await _interactions.RegisterCommandsToGuildAsync(guildId);
            _commandsRegistered = true;
            _logger.LogInformation("Registered {Count} slash commands to guild {GuildId}",
                _interactions.SlashCommands.Count, guildId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register commands to guild {GuildId}", guildId);
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
}
