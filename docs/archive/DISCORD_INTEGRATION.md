# Discord Integration Guide

## Overview

Mystira now includes native Discord bot integration, allowing you to:

- Send real-time notifications to Discord channels
- Process game commands through Discord
- Monitor game sessions and player activities
- Integrate with Discord communities for enhanced engagement

## Architecture

The Discord integration follows Mystira's hexagonal architecture pattern:

```
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                       │
│         (Use Cases, Business Logic)                      │
│                        ↓                                 │
│         IDiscordBotService (Port/Interface)              │
└─────────────────────────────────────────────────────────┘
                           ↑
                    (implements)
                           │
┌─────────────────────────────────────────────────────────┐
│            Infrastructure.Discord Layer                  │
│                                                          │
│  ┌──────────────────┐  ┌──────────────────┐            │
│  │ DiscordBotService│  │DiscordOptions    │            │
│  │  (Adapter)       │  │(Configuration)   │            │
│  └──────────────────┘  └──────────────────┘            │
│           ↓                                              │
│  ┌──────────────────────────────────────┐               │
│  │        Discord.NET Library           │               │
│  └──────────────────────────────────────┘               │
└─────────────────────────────────────────────────────────┘
```

### Key Components

1. **IDiscordBotService** (Port)
   - Defines Discord operations without implementation details
   - Located in: `Mystira.App.Infrastructure.Discord/Services/`
   - Provides methods for sending messages, embeds, and replies

2. **DiscordBotService** (Adapter)
   - Implements IDiscordBotService using Discord.NET
   - Handles connection lifecycle and error recovery
   - Manages gateway intents and permissions

3. **DiscordBotHostedService**
   - Background service for continuous bot operation
   - Suitable for Azure App Services, Container Apps, WebJobs
   - Handles graceful startup and shutdown

4. **DiscordOptions**
   - Configuration POCO for bot settings
   - Supports Azure Key Vault, User Secrets, appsettings.json
   - Configures intents, timeouts, retry policies

## Getting Started

### 1. Create a Discord Bot

1. Visit [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **"New Application"** and name it (e.g., "Mystira Bot")
3. Navigate to the **"Bot"** section
4. Click **"Add Bot"** to create a bot user
5. Copy your **Bot Token** (keep this secure!)
6. Under **"Privileged Gateway Intents"**, enable:
   - ✅ **Message Content Intent** (required for reading messages)
   - ✅ **Server Members Intent** (optional, for member info)
7. Save your changes

### 2. Invite Bot to Your Server

1. In Developer Portal, go to **OAuth2 → URL Generator**
2. Select scopes:
   - ✅ `bot`
   - ✅ `applications.commands` (for slash commands)
3. Select bot permissions:
   - ✅ Send Messages
   - ✅ Send Messages in Threads
   - ✅ Embed Links
   - ✅ Read Message History
   - ✅ Use Slash Commands
4. Copy the generated URL
5. Open URL in browser and authorize bot to your server

### 3. Configure Your Application

#### Using Azure Key Vault (Production)

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{vaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Set these secrets in Key Vault:
- `Discord--BotToken`: Your Discord bot token
- `Discord--GuildIds`: Comma-separated server IDs (optional)

#### Using User Secrets (Development)

```bash
dotnet user-secrets set "Discord:BotToken" "YOUR_BOT_TOKEN_HERE"
```

#### Using appsettings.json (Not Recommended)

```json
{
  "Discord": {
    "BotToken": "YOUR_BOT_TOKEN",
    "EnableMessageContentIntent": true,
    "EnableGuildMembersIntent": false,
    "LogAllMessages": false,
    "CommandPrefix": "!"
  }
}
```

### 4. Register Discord Services

In your `Program.cs` or `Startup.cs`:

```csharp
using Mystira.App.Infrastructure.Discord;

// Add Discord bot services
builder.Services.AddDiscordBot(builder.Configuration);

// Add as background service (for continuous operation)
builder.Services.AddDiscordBotHostedService();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDiscordBotHealthCheck();
```

## Usage Examples

### Sending Messages from Use Cases

```csharp
using Mystira.App.Infrastructure.Discord.Services;

public class NotifyDiscordUseCase
{
    private readonly IDiscordBotService _discordBot;

    public NotifyDiscordUseCase(IDiscordBotService discordBot)
    {
        _discordBot = discordBot;
    }

    public async Task NotifyGameSessionStarted(ulong channelId, string sessionName)
    {
        if (!_discordBot.IsConnected)
        {
            // Bot not ready yet, log or queue
            return;
        }

        await _discordBot.SendMessageAsync(
            channelId, 
            $"🎲 New game session started: **{sessionName}**");
    }
}
```

### Sending Rich Embeds

```csharp
using Discord;

public async Task SendGameStatusEmbed(ulong channelId, GameSession session)
{
    var embed = new EmbedBuilder()
        .WithTitle("🎮 Game Session Update")
        .WithDescription($"Session: {session.Name}")
        .AddField("Status", session.Status, inline: true)
        .AddField("Players", session.PlayerCount, inline: true)
        .AddField("Started", session.StartTime.ToString("g"), inline: true)
        .WithColor(Color.Green)
        .WithTimestamp(DateTimeOffset.Now)
        .WithFooter("Mystira Adventure Platform")
        .Build();

    await _discordBot.SendEmbedAsync(channelId, embed);
}
```

### Replying to Messages

```csharp
public async Task ReplyToUser(ulong messageId, ulong channelId, string response)
{
    await _discordBot.ReplyToMessageAsync(
        messageId, 
        channelId, 
        $"✅ {response}");
}
```

## Hosting Options

### Azure App Service with WebJobs (Recommended)

**Best for:** Always-on bots requiring persistent connections

**Setup:**
```bash
# Create App Service (B1 tier minimum for Always On)
az webapp create \
  --resource-group mystira-rg \
  --plan mystira-plan \
  --name mystira-discord-bot \
  --runtime "DOTNET|9.0"

# Enable Always On (required for WebJobs)
az webapp config set \
  --resource-group mystira-rg \
  --name mystira-discord-bot \
  --always-on true

# Configure bot token
az webapp config appsettings set \
  --resource-group mystira-rg \
  --name mystira-discord-bot \
  --settings Discord__BotToken="YOUR_TOKEN"
```

**Cost:** ~$55/month (B1 tier)

### Azure Container Apps

**Best for:** Scalable containerized deployments

**Setup:**
```bash
# Create container app (minimum 1 replica for persistent bot)
az containerapp create \
  --name mystira-discord-bot \
  --resource-group mystira-rg \
  --environment mystira-env \
  --image mystiraacr.azurecr.io/discord-bot:latest \
  --min-replicas 1 \
  --max-replicas 1 \
  --secrets discord-token="YOUR_TOKEN" \
  --env-vars "Discord__BotToken=secretref:discord-token"
```

**Cost:** Pay-per-use, typically $15-30/month

### Azure Container Instances

**Best for:** Simple single-instance deployments

```bash
az container create \
  --resource-group mystira-rg \
  --name mystira-discord-bot \
  --image mystiraacr.azurecr.io/discord-bot:latest \
  --restart-policy Always \
  --environment-variables Discord__BotToken="YOUR_TOKEN"
```

**Cost:** ~$10-20/month

## Advanced Features

### Custom Message Handling

Extend `DiscordBotService` to add custom message processing:

```csharp
public class CustomDiscordBotService : DiscordBotService
{
    private readonly IGameSessionService _gameService;

    public CustomDiscordBotService(
        IOptions<DiscordOptions> options,
        ILogger<CustomDiscordBotService> logger,
        IGameSessionService gameService)
        : base(options, logger)
    {
        _gameService = gameService;
    }

    protected override async Task MessageReceivedAsync(SocketMessage message)
    {
        // Call base implementation first
        await base.MessageReceivedAsync(message);

        // Custom handling
        if (message.Content.StartsWith("!roll"))
        {
            var result = RollDice();
            await message.Channel.SendMessageAsync($"🎲 You rolled: {result}");
        }
    }
}
```

### Slash Commands Integration

```csharp
using Discord.Interactions;

public class GameCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("start-adventure", "Start a new adventure")]
    public async Task StartAdventure(
        [Summary("scenario", "The scenario to play")] string scenarioName)
    {
        await RespondAsync($"Starting adventure: {scenarioName}...");
        // Create game session logic here
    }

    [SlashCommand("roll", "Roll dice")]
    public async Task Roll(
        [Summary("dice", "Dice notation (e.g., 2d6)")] string dice)
    {
        var result = DiceRoller.Roll(dice);
        await RespondAsync($"🎲 Result: {result}");
    }
}
```

### Health Monitoring

Access health check endpoint to monitor bot status:

```bash
curl https://your-app.azurewebsites.net/health
```

Response:
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
        "BotId": "123456789012345678"
      }
    }
  }
}
```

## Security Best Practices

### 1. Never Hardcode Bot Tokens
❌ **Don't do this:**
```csharp
var token = "MTA1ODM5NzY0ODY1NTI3NDU2Nw.GxYz..."; // NEVER!
```

✅ **Do this:**
```csharp
var token = configuration["Discord:BotToken"];
```

### 2. Use Azure Key Vault for Production
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 3. Enable Managed Identity
```bash
az webapp identity assign \
  --resource-group mystira-rg \
  --name mystira-discord-bot

# Grant Key Vault access
az keyvault set-policy \
  --name mystira-keyvault \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Implement Rate Limiting
Discord has strict rate limits. The library handles most cases, but be mindful:
- Global rate limit: 50 requests per second
- Per-channel: Varies by action
- DM rate limits: More restrictive

### 5. Validate Input
Always sanitize and validate Discord user input before processing:
```csharp
public async Task ProcessCommand(string userInput)
{
    // Sanitize input
    var sanitized = Regex.Replace(userInput, @"[^\w\s-]", "");
    
    // Validate length
    if (sanitized.Length > 100)
    {
        await RespondAsync("Input too long!");
        return;
    }
    
    // Process safely
    await HandleCommand(sanitized);
}
```

## Monitoring and Observability

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Log custom metrics
telemetryClient.TrackEvent("DiscordMessageProcessed", new Dictionary<string, string>
{
    ["GuildId"] = guildId.ToString(),
    ["ChannelId"] = channelId.ToString()
});
```

### Logging Best Practices

Configure structured logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Discord": "Information",
      "Mystira.App.Infrastructure.Discord": "Debug"
    }
  }
}
```

## Troubleshooting

### Bot Not Connecting

**Symptoms:** Health check shows "Unhealthy", bot offline in Discord

**Solutions:**
1. Verify bot token is correct
2. Check intents are enabled in Discord Developer Portal
3. Ensure network connectivity to Discord API
4. Review logs for authentication errors

### Message Content Not Available

**Symptoms:** `message.Content` is empty

**Solutions:**
1. Enable **Message Content Intent** in Discord Developer Portal
2. Set `EnableMessageContentIntent: true` in configuration
3. Note: Bots in 100+ servers need verification from Discord

### Rate Limiting Issues

**Symptoms:** 429 errors in logs

**Solutions:**
1. Reduce message frequency
2. Implement message queuing
3. Use bulk operations where possible
4. Cache frequently accessed data

### Bot Crashes on Startup

**Symptoms:** Bot starts then immediately stops

**Solutions:**
1. Check token is configured correctly
2. Verify all required intents are enabled
3. Review startup logs for exceptions
4. Ensure .NET 9 runtime is available

## Cost Optimization

### Development
- Use free tier locally with `dotnet run`
- No hosting costs during development

### Production
| Option | Monthly Cost | Best For |
|--------|-------------|----------|
| App Service B1 | ~$55 | Always-on, reliable |
| Container Apps | ~$15-30 | Scalable, modern |
| Container Instances | ~$10-20 | Simple, single-instance |

### Cost-Saving Tips
1. Use shared App Service plan if hosting multiple services
2. Enable auto-scaling on Container Apps during off-peak hours
3. Use consumption-based pricing for non-critical bots
4. Monitor and optimize message frequency

## Examples

See the `/examples/DiscordBotExample` folder for:
- Basic console application setup
- Message handling examples
- Health check integration
- Configuration best practices

## Further Reading

- [Discord.NET Documentation](https://docs.discordnet.dev/)
- [Discord Developer Portal](https://discord.com/developers/docs)
- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Mystira Architecture Guidelines](./architecture/ARCHITECTURAL_RULES.md)

## Support

For issues or questions:
1. Check the [troubleshooting section](#troubleshooting)
2. Review [Discord.NET docs](https://docs.discordnet.dev/)
3. Open an issue in the repository
4. Contact the development team
