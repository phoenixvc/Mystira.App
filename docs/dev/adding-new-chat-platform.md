# Developer Guide: Adding a New Chat Platform

## Overview

This guide explains how to add support for a new chat platform (e.g., Slack, Telegram) to the Mystira.App multi-platform chat bot architecture.

## Architecture Overview

The chat bot integration follows hexagonal (ports & adapters) architecture:

```
Application Layer (Ports)
├── IChatBotService         - Core messaging interface
├── IBotCommandService      - Slash command interface
└── IMessagingService       - Legacy interface

Infrastructure Layer (Adapters)
├── Discord.DiscordBotService
├── Teams.TeamsBotService
├── WhatsApp.WhatsAppBotService
└── [Your Platform].YourPlatformBotService
```

## Step-by-Step Implementation

### 1. Create Infrastructure Project

```bash
mkdir src/Mystira.App.Infrastructure.Slack
```

Create `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mystira.App.Application\Mystira.App.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Add platform-specific SDK -->
    <PackageReference Include="SlackNet" Version="0.12.0" />
  </ItemGroup>
</Project>
```

### 2. Add Platform to ChatPlatform Enum

In `IChatBotService.cs`:

```csharp
public enum ChatPlatform
{
    Discord,
    Teams,
    WhatsApp,
    Slack,      // Add your platform
    Telegram    // etc.
}
```

### 3. Create Configuration Options

```csharp
// Configuration/SlackOptions.cs
namespace Mystira.App.Infrastructure.Slack.Configuration;

public class SlackOptions
{
    public const string SectionName = "Slack";

    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string AppToken { get; set; } = string.Empty;
    public string SigningSecret { get; set; } = string.Empty;
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
}
```

### 4. Implement IChatBotService

```csharp
// Services/SlackBotService.cs
using System.Collections.Concurrent;
using Mystira.App.Application.Ports.Messaging;

namespace Mystira.App.Infrastructure.Slack.Services;

public class SlackBotService : IMessagingService, IChatBotService, IBotCommandService, IDisposable
{
    private readonly ILogger<SlackBotService> _logger;
    private readonly SlackOptions _options;
    private readonly ConcurrentDictionary<ulong, string> _channelMapping = new();
    private bool _isConnected;
    private bool _disposed;

    public SlackBotService(
        IOptions<SlackOptions> options,
        ILogger<SlackBotService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // Required: Platform identifier
    public ChatPlatform Platform => ChatPlatform.Slack;

    public bool IsConnected => _isConnected;
    public bool IsEnabled => _options.Enabled;
    public int RegisteredModuleCount => 0;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Slack bot is disabled");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            throw new InvalidOperationException("Slack BotToken is not configured");
        }

        // Initialize your platform SDK here
        _isConnected = true;
        _logger.LogInformation("Slack bot started");
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        _logger.LogInformation("Slack bot stopped");
        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(
        ulong channelId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Slack bot is not connected");

        var slackChannelId = GetSlackChannelId(channelId);
        if (slackChannelId == null)
            throw new InvalidOperationException($"No Slack channel found for ID {channelId}");

        // Implement retry logic
        var maxAttempts = _options.MaxRetryAttempts;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Call your platform SDK
                // await _slackClient.ChatPostMessage(slackChannelId, message);
                _logger.LogDebug("Sent message to Slack channel {ChannelId}", channelId);
                return;
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Transient error, retrying in {Delay}s", delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public async Task SendEmbedAsync(
        ulong channelId,
        EmbedData embed,
        CancellationToken cancellationToken = default)
    {
        // Convert EmbedData to platform-specific format (Slack Blocks)
        if (embed.ColorRed != 0 || embed.ColorGreen != 0 || embed.ColorBlue != 0)
        {
            _logger.LogDebug("Slack blocks support limited color options");
        }

        // Build Slack Block Kit message
        var blocks = ConvertEmbedToBlocks(embed);

        // Send using platform SDK
        await SendMessageAsync(channelId, embed.Description ?? "", cancellationToken);
    }

    public async Task ReplyToMessageAsync(
        ulong messageId,
        ulong channelId,
        string reply,
        CancellationToken cancellationToken = default)
    {
        // Slack uses thread_ts for threaded replies
        // Implement thread reply logic
        await SendMessageAsync(channelId, reply, cancellationToken);
    }

    public BotStatus GetStatus()
    {
        return new BotStatus
        {
            IsEnabled = _options.Enabled,
            IsConnected = IsConnected,
            BotName = "Slack Bot",
            BotId = null,
            ServerCount = _channelMapping.Count
        };
    }

    // Broadcast methods (if platform supports real-time events)
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
                _logger.LogWarning(ex, "Failed to send to channel {ChannelId}", channelId);
            }
        }

        // If your platform supports event subscriptions, implement response listening
        // Otherwise, return timeout result
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
                _logger.LogWarning(ex, "Failed to send embed to channel {ChannelId}", channelId);
            }
        }
        return new FirstResponderResult { TimedOut = true, SentMessages = sentMessages };
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
                _logger.LogWarning(ex, "Failed to broadcast to channel {ChannelId}", channelId);
            }
        }
        _logger.LogWarning("Slack broadcast sent. Response handling requires event subscription.");
    }

    // Helper methods
    private string? GetSlackChannelId(ulong channelId)
    {
        return _channelMapping.TryGetValue(channelId, out var slackId) ? slackId : null;
    }

    private static bool IsTransientError(Exception ex)
    {
        // Check for rate limits, server errors, etc.
        return ex.Message.Contains("rate_limit") ||
               ex.Message.Contains("service_unavailable");
    }

    private object ConvertEmbedToBlocks(EmbedData embed)
    {
        // Convert to Slack Block Kit format
        return new { /* blocks */ };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _isConnected = false;
        _channelMapping.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // IBotCommandService - implement if platform supports slash commands
    public Task RegisterCommandsAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Slack commands are registered via the Slack App manifest");
        return Task.CompletedTask;
    }

    public Task RegisterCommandsToServerAsync(ulong serverId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RegisterCommandsGloballyAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

### 5. Create Service Collection Extensions

```csharp
// ServiceCollectionExtensions.cs
namespace Mystira.App.Infrastructure.Slack;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackBot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SlackOptions>(
            configuration.GetSection(SlackOptions.SectionName));

        services.AddSingleton<SlackBotService>();
        services.AddSingleton<IMessagingService>(sp => sp.GetRequiredService<SlackBotService>());
        services.AddSingleton<IChatBotService>(sp => sp.GetRequiredService<SlackBotService>());
        services.AddSingleton<IBotCommandService>(sp => sp.GetRequiredService<SlackBotService>());

        return services;
    }

    public static IServiceCollection AddSlackBotKeyed(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceKey = "slack")
    {
        services.Configure<SlackOptions>(
            configuration.GetSection(SlackOptions.SectionName));

        services.AddSingleton<SlackBotService>();
        services.AddKeyedSingleton<IMessagingService>(serviceKey, (sp, _) => sp.GetRequiredService<SlackBotService>());
        services.AddKeyedSingleton<IChatBotService>(serviceKey, (sp, _) => sp.GetRequiredService<SlackBotService>());
        services.AddKeyedSingleton<IBotCommandService>(serviceKey, (sp, _) => sp.GetRequiredService<SlackBotService>());

        return services;
    }
}
```

### 6. Add to Solution

```bash
dotnet sln add src/Mystira.App.Infrastructure.Slack/Mystira.App.Infrastructure.Slack.csproj
```

### 7. Create Tests

```csharp
// tests/Mystira.App.Infrastructure.Slack.Tests/SlackBotServiceTests.cs
public class SlackBotServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // ...
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotConnected_ShouldThrow()
    {
        // ...
    }
}
```

### 8. Update Configuration

```json
// appsettings.json
{
  "Slack": {
    "Enabled": false,
    "BotToken": "",
    "AppToken": "",
    "SigningSecret": "",
    "DefaultTimeoutSeconds": 30,
    "MaxRetryAttempts": 3
  }
}
```

## Platform-Specific Considerations

### Channel ID Mapping

Each platform has different ID formats:
- **Discord**: Native ulong IDs
- **Teams**: String conversation IDs
- **WhatsApp**: Phone numbers
- **Slack**: String channel IDs (C1234567)

Use `ConcurrentDictionary` to map between internal ulong IDs and platform-specific IDs.

### Retry Logic

Implement exponential backoff for transient errors:

```csharp
var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
```

### Thread Safety

Use `ConcurrentDictionary` for shared state and `Interlocked` for flags.

### Rate Limiting

Handle rate limits gracefully:
1. Check for rate limit errors
2. Parse retry-after headers if available
3. Implement backoff
4. Log warnings

## Checklist

- [ ] Create Infrastructure project
- [ ] Add to ChatPlatform enum
- [ ] Create Options class
- [ ] Implement IChatBotService
- [ ] Implement IBotCommandService
- [ ] Create ServiceCollectionExtensions
- [ ] Add to solution
- [ ] Create test project
- [ ] Write unit tests
- [ ] Add configuration section
- [ ] Create webhook controller (if needed)
- [ ] Add health check
- [ ] Update documentation

## References

- [Slack API](https://api.slack.com/)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [Microsoft Bot Framework](https://dev.botframework.com/)
