# Mystira.App.Infrastructure.Discord

Discord bot integration infrastructure for the Mystira platform.

## Overview

This library provides Discord bot functionality using Discord.NET, following the hexagonal architecture pattern. It includes:

- **Discord Bot Service**: Core Discord.NET client wrapper with message handling
- **Hosted Service**: Background service for continuous bot operation
- **Health Checks**: Monitor bot connectivity and status
- **Configuration**: Flexible configuration via appsettings.json or Azure Key Vault

## Installation

Add the package reference to your project:

```bash
dotnet add reference ../Mystira.App.Infrastructure.Discord/Mystira.App.Infrastructure.Discord.csproj
```

## Configuration

### appsettings.json

```json
{
  "Discord": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "GuildIds": "",
    "EnableMessageContentIntent": true,
    "EnableGuildMembersIntent": false,
    "DefaultTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "LogAllMessages": false,
    "CommandPrefix": "!"
  }
}
```

### Azure Key Vault (Recommended for Production)

Store the bot token securely in Azure Key Vault:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Then set the secret:
- Key: `Discord--BotToken`
- Value: Your Discord bot token

### User Secrets (Development)

```bash
dotnet user-secrets set "Discord:BotToken" "YOUR_BOT_TOKEN_HERE"
```

## Getting a Discord Bot Token

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application" and give it a name
3. Go to the "Bot" section
4. Click "Add Bot"
5. Under "Token", click "Copy" to get your bot token
6. Enable the required **Privileged Gateway Intents**:
   - **Message Content Intent** (required if `EnableMessageContentIntent` is true)
   - **Server Members Intent** (required if `EnableGuildMembersIntent` is true)
7. Save your changes

## Usage

### Basic Setup (Console App or WebJob)

```csharp
using Mystira.App.Infrastructure.Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Add Discord bot services
builder.Services.AddDiscordBot(builder.Configuration);
builder.Services.AddDiscordBotHostedService();

// Add health checks (optional)
builder.Services.AddHealthChecks()
    .AddDiscordBotHealthCheck();

var host = builder.Build();
await host.RunAsync();
```

### ASP.NET Core Integration

```csharp
using Mystira.App.Infrastructure.Discord;

var builder = WebApplication.CreateBuilder(args);

// Add Discord bot services
builder.Services.AddDiscordBot(builder.Configuration);
builder.Services.AddDiscordBotHostedService();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDiscordBotHealthCheck();

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

### Using the Discord Bot Service

```csharp
using Mystira.App.Infrastructure.Discord.Services;
using Discord;

public class MyService
{
    private readonly IDiscordBotService _discordBot;

    public MyService(IDiscordBotService discordBot)
    {
        _discordBot = discordBot;
    }

    public async Task SendNotificationAsync(ulong channelId, string message)
    {
        if (_discordBot.IsConnected)
        {
            await _discordBot.SendMessageAsync(channelId, message);
        }
    }

    public async Task SendRichNotificationAsync(ulong channelId)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Game Session Started")
            .WithDescription("A new adventure begins!")
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await _discordBot.SendEmbedAsync(channelId, embed);
    }
}
```

## Hosting Options on Azure

### 1. Azure App Service WebJobs (Recommended)

**Best for:** Always-on Discord bots that need persistent connections

**Setup:**
```bash
# Create App Service with Always On enabled
az webapp create --resource-group myRG --plan myPlan --name my-discord-bot

# Enable Always On (required for WebJobs)
az webapp config set --resource-group myRG --name my-discord-bot --always-on true

# Deploy as WebJob
az webapp deployment source config-zip \
  --resource-group myRG \
  --name my-discord-bot \
  --src discord-bot.zip
```

**Cost:** ~$55/month (B1 tier minimum for Always On)

### 2. Azure Container Apps

**Best for:** Scalable deployments with modern container orchestration

**Setup:**
```bash
# Create container app with minimum 1 replica (bot must stay online)
az containerapp create \
  --name my-discord-bot \
  --resource-group myRG \
  --environment myEnv \
  --image myregistry.azurecr.io/discord-bot:latest \
  --min-replicas 1 \
  --max-replicas 1
```

**Cost:** Pay-per-use, typically $15-30/month for 1 replica

### 3. Azure Functions

**Note:** Not ideal for Discord bots requiring persistent connections. Use for slash commands only.

## Architecture

This library follows hexagonal/clean architecture:

- **Port (Interface)**: `IDiscordBotService` - defines operations without implementation details
- **Adapter (Implementation)**: `DiscordBotService` - Discord.NET specific implementation
- **Configuration**: `DiscordOptions` - configuration POCO
- **Infrastructure**: Health checks, hosted services, DI registration

### Dependency Flow

```
Your Application Layer (Use Cases)
        ↓ (depends on)
IDiscordBotService (Port/Interface)
        ↑ (implemented by)
DiscordBotService (Adapter)
        ↓ (uses)
    Discord.NET Library
```

## Security Best Practices

1. **Never commit bot tokens** - Always use User Secrets, Azure Key Vault, or environment variables
2. **Use Managed Identity** - When possible, use Azure Managed Identity for Key Vault access
3. **Implement rate limiting** - Discord has rate limits; respect them to avoid bans
4. **Enable Application Insights** - Monitor bot health and usage
5. **Minimal intents** - Only enable required gateway intents (reduces data exposure)
6. **Validate input** - Always validate and sanitize user input from Discord messages

## Monitoring

### Health Checks

The health check endpoint provides bot status:

```json
{
  "status": "Healthy",
  "results": {
    "discord_bot": {
      "status": "Healthy",
      "description": "Discord bot is connected and operational",
      "data": {
        "IsConnected": true,
        "BotUsername": "MystiraBot",
        "BotId": 123456789012345678
      }
    }
  }
}
```

### Application Insights

Add Application Insights to track:
- Bot connection events
- Message processing metrics
- Error rates and exceptions
- Custom telemetry for game-related events

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

## Message Content Intent Warning

⚠️ **Important:** Discord's Message Content Intent is a **privileged intent** that requires verification for bots in 100+ servers.

- For development/small deployments: Enable it in the Discord Developer Portal
- For production/large deployments: You may need to request verification from Discord
- Alternative: Use slash commands (`/`) instead of message content parsing

## Troubleshooting

### Bot Not Connecting

1. Verify bot token is correct
2. Check that required intents are enabled in Discord Developer Portal
3. Ensure bot has been invited to your server
4. Check Application Insights or logs for error messages

### Health Check Failing

1. Bot may still be connecting (takes 5-10 seconds on startup)
2. Network connectivity issues
3. Invalid token or revoked token
4. Discord API may be experiencing issues

### Bot Invited But Not Responding

1. Verify `EnableMessageContentIntent` is true in config
2. Verify Message Content Intent is enabled in Discord Developer Portal
3. Check command prefix matches your configuration
4. Review logs for message processing errors

## Examples

See the `/examples` folder in the repository root for:
- Console app example
- ASP.NET Core integration example
- Advanced message handling example
- Slash commands integration

## License

This library is part of the Mystira.App project and follows the same license.
