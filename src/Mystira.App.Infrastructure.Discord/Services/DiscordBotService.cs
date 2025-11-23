using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Infrastructure.Discord.Configuration;

namespace Mystira.App.Infrastructure.Discord.Services;

/// <summary>
/// Implementation of Discord bot service using Discord.NET
/// </summary>
public class DiscordBotService : IDiscordBotService, IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly DiscordOptions _options;
    private bool _disposed;

    public DiscordBotService(
        IOptions<DiscordOptions> options,
        ILogger<DiscordBotService> logger)
    {
        _options = options.Value;
        _logger = logger;

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

        // Wire up event handlers
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.Disconnected += DisconnectedAsync;
    }

    public bool IsConnected => _client.ConnectionState == ConnectionState.Connected;

    public SocketSelfUser? CurrentUser => _client.CurrentUser;

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

    private Task ReadyAsync()
    {
        _logger.LogInformation("Discord bot is ready! Logged in as {Username}", 
            _client.CurrentUser?.Username);
        
        return Task.CompletedTask;
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

        _client?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
