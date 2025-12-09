using System.Reflection;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Infrastructure.Discord.Services;

/// <summary>
/// No-op implementation of chat bot services used when a platform bot is disabled.
/// Satisfies DI for IChatBotService, IMessagingService, and IBotCommandService.
/// </summary>
public class NoOpChatBotService : IChatBotService, IMessagingService, IBotCommandService
{
    private readonly ILogger<NoOpChatBotService> _logger;

    public NoOpChatBotService(ILogger<NoOpChatBotService> logger)
    {
        _logger = logger;
    }

    // IChatBotService
    public ChatPlatform Platform => ChatPlatform.Discord; // default placeholder

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NoOpChatBotService.StartAsync called – bot integration is disabled.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NoOpChatBotService.StopAsync called – bot integration is disabled.");
        return Task.CompletedTask;
    }

    public Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] SendMessageAsync to {ChannelId}: {Message}", channelId, message);
        return Task.CompletedTask;
    }

    public Task SendEmbedAsync(ulong channelId, EmbedData embed, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] SendEmbedAsync to {ChannelId}: {Title}", channelId, embed.Title);
        return Task.CompletedTask;
    }

    public Task ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] ReplyToMessageAsync to {ChannelId}/{MessageId}: {Reply}", channelId, messageId, reply);
        return Task.CompletedTask;
    }

    public bool IsConnected => false;

    public BotStatus GetStatus()
        => new BotStatus
        {
            IsEnabled = false,
            IsConnected = false,
            BotName = "NoOp Bot",
            BotId = 0,
            ServerCount = 0
        };

    public Task<FirstResponderResult> SendAndAwaitFirstResponseAsync(IEnumerable<ulong> channelIds, string message, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] Broadcast message to {Count} channels. No responses will be awaited.", channelIds?.Count() ?? 0);
        return Task.FromResult(new FirstResponderResult { TimedOut = true });
    }

    public Task<FirstResponderResult> SendEmbedAndAwaitFirstResponseAsync(IEnumerable<ulong> channelIds, EmbedData embed, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] Broadcast embed to {Count} channels. No responses will be awaited.", channelIds?.Count() ?? 0);
        return Task.FromResult(new FirstResponderResult { TimedOut = true });
    }

    public Task BroadcastWithResponseHandlerAsync(IEnumerable<ulong> channelIds, string message, Func<ResponseEvent, Task<bool>> onResponse, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] BroadcastWithResponseHandlerAsync called. No responses will be produced.");
        return Task.CompletedTask;
    }

    // IMessagingService explicit members map to IChatBotService implementations
    Task IMessagingService.StartAsync(CancellationToken cancellationToken) => StartAsync(cancellationToken);
    Task IMessagingService.StopAsync(CancellationToken cancellationToken) => StopAsync(cancellationToken);
    Task IMessagingService.SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken) => SendMessageAsync(channelId, message, cancellationToken);
    Task IMessagingService.ReplyToMessageAsync(ulong messageId, ulong channelId, string reply, CancellationToken cancellationToken) => ReplyToMessageAsync(messageId, channelId, reply, cancellationToken);
    bool IMessagingService.IsConnected => IsConnected;

    // IBotCommandService
    public Task RegisterCommandsAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] RegisterCommandsAsync called for assembly {Assembly}", assembly.FullName);
        return Task.CompletedTask;
    }

    public Task RegisterCommandsToServerAsync(ulong serverId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] RegisterCommandsToServerAsync called for server {ServerId}", serverId);
        return Task.CompletedTask;
    }

    public Task RegisterCommandsGloballyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOp] RegisterCommandsGloballyAsync called.");
        return Task.CompletedTask;
    }

    public bool IsEnabled => false;
    public int RegisteredModuleCount => 0;
}
