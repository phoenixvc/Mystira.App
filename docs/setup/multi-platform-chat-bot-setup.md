# Multi-Platform Chat Bot Setup Guide

## Overview

Mystira supports multiple chat platforms (Discord, Teams, WhatsApp) simultaneously. This guide covers how to configure your application to use one or more platforms.

## Architecture

All chat platforms implement the same Application port interfaces:

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────┐ │
│  │ IMessagingService│  │ IChatBotService  │  │IBotCommand │ │
│  │    (Port)        │  │    (Port)        │  │  Service   │ │
│  └────────┬─────────┘  └────────┬─────────┘  └─────┬──────┘ │
└───────────┼─────────────────────┼──────────────────┼────────┘
            │                     │                  │
  ┌─────────▼─────────────────────▼──────────────────▼─────────┐
  │                  Infrastructure Layer                       │
  │                                                              │
  │  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐       │
  │  │Discord Bot  │   │ Teams Bot   │   │WhatsApp Bot │       │
  │  │  Service    │   │  Service    │   │  Service    │       │
  │  └─────────────┘   └─────────────┘   └─────────────┘       │
  │                                                              │
  └──────────────────────────────────────────────────────────────┘
```

## Configuration Options

### Option 1: Single Platform (Default)

Use when you only need one chat platform:

```csharp
// Program.cs - Discord only
builder.Services.AddDiscordBot(builder.Configuration);

// Program.cs - Teams only
builder.Services.AddTeamsBotAsDefault(builder.Configuration);

// Program.cs - WhatsApp only
builder.Services.AddWhatsAppBotAsDefault(builder.Configuration);
```

**Inject normally:**
```csharp
public class MyService(IChatBotService chatBot)
{
    public async Task NotifyAsync(ulong channelId, string message)
    {
        await chatBot.SendMessageAsync(channelId, message);
    }
}
```

### Option 2: Multiple Platforms (Keyed Services)

Use when you need multiple platforms simultaneously:

```csharp
// Program.cs
builder.Services.AddDiscordBotKeyed(builder.Configuration, "discord");
builder.Services.AddTeamsBotKeyed(builder.Configuration, "teams");
builder.Services.AddWhatsAppBotKeyed(builder.Configuration, "whatsapp");
```

**Inject with keys:**
```csharp
public class NotificationService
{
    private readonly IChatBotService _discordBot;
    private readonly IChatBotService _teamsBot;
    private readonly IChatBotService _whatsappBot;

    public NotificationService(
        [FromKeyedServices("discord")] IChatBotService discordBot,
        [FromKeyedServices("teams")] IChatBotService teamsBot,
        [FromKeyedServices("whatsapp")] IChatBotService whatsappBot)
    {
        _discordBot = discordBot;
        _teamsBot = teamsBot;
        _whatsappBot = whatsappBot;
    }

    public async Task BroadcastToAllAsync(string message)
    {
        // Send to all platforms
        var tasks = new List<Task>();

        if (_discordBot.IsConnected)
            tasks.Add(_discordBot.SendMessageAsync(discordChannelId, message));

        if (_teamsBot.IsConnected)
            tasks.Add(_teamsBot.SendMessageAsync(teamsChannelId, message));

        if (_whatsappBot.IsConnected)
            tasks.Add(_whatsappBot.SendMessageAsync(whatsappChannelId, message));

        await Task.WhenAll(tasks);
    }
}
```

### Option 3: Factory Pattern

For dynamic platform selection at runtime:

```csharp
public interface IChatBotFactory
{
    IChatBotService GetBot(ChatPlatform platform);
}

public class ChatBotFactory : IChatBotFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ChatBotFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IChatBotService GetBot(ChatPlatform platform)
    {
        return platform switch
        {
            ChatPlatform.Discord => _serviceProvider.GetRequiredKeyedService<IChatBotService>("discord"),
            ChatPlatform.Teams => _serviceProvider.GetRequiredKeyedService<IChatBotService>("teams"),
            ChatPlatform.WhatsApp => _serviceProvider.GetRequiredKeyedService<IChatBotService>("whatsapp"),
            _ => throw new ArgumentException($"Unknown platform: {platform}")
        };
    }
}

// Register factory
builder.Services.AddSingleton<IChatBotFactory, ChatBotFactory>();
```

## Configuration Files

### appsettings.json

```json
{
  "Discord": {
    "BotToken": "",
    "EnableSlashCommands": true,
    "EnableMessageContentIntent": true,
    "CommandPrefix": "!",
    "MaxRetryAttempts": 3,
    "DefaultTimeoutSeconds": 30
  },
  "Teams": {
    "Enabled": true,
    "MicrosoftAppId": "",
    "MicrosoftAppPassword": "",
    "DefaultTimeoutSeconds": 30
  },
  "WhatsApp": {
    "Enabled": true,
    "ConnectionString": "",
    "ChannelRegistrationId": "",
    "MaxRetryAttempts": 3,
    "DefaultTimeoutSeconds": 30
  }
}
```

### Environment Variables (Production)

```bash
# Discord
Discord__BotToken=your-discord-token

# Teams (Azure Bot)
Teams__MicrosoftAppId=your-app-id
Teams__MicrosoftAppPassword=your-app-password

# WhatsApp (Azure Communication Services)
WhatsApp__ConnectionString=endpoint=https://xxx.communication.azure.com/;accesskey=xxx
WhatsApp__ChannelRegistrationId=your-channel-id
```

## Platform-Specific Notes

### Discord

- Full support for slash commands, embeds, and reactions
- Real-time message monitoring available
- Requires Gateway Intents for message content

### Teams

- Uses Bot Framework proactive messaging
- Requires conversation reference from prior interaction
- Embeds converted to Hero Cards

### WhatsApp

- 24-hour messaging window limitation
- Template messages required outside window
- No concept of "channels" - messages are to phone numbers

## Health Checks

Each platform has its own health check:

```csharp
// Add health checks
builder.Services.AddHealthChecks()
    .AddDiscordBotHealthCheck()
    .AddCheck<TeamsBotHealthCheck>("teams-bot")
    .AddCheck<WhatsAppBotHealthCheck>("whatsapp-bot");
```

## Troubleshooting

| Issue | Platform | Solution |
|-------|----------|----------|
| "Bot is not connected" | All | Ensure `StartAsync()` was called or use hosted service |
| "No conversation reference" | Teams/WhatsApp | Requires prior user interaction |
| Rate limited | Discord | Built-in retry with exponential backoff |
| Template required | WhatsApp | Use `SendTemplateMessageAsync()` after 24h |

## See Also

- [CHAT_BOT_INFRASTRUCTURE.md](../ops/CHAT_BOT_INFRASTRUCTURE.md) - Azure deployment
- [BOT_MONITORING.md](../ops/BOT_MONITORING.md) - Monitoring setup
- [DISCORD_INTEGRATION.md](../DISCORD_INTEGRATION.md) - Discord-specific details
