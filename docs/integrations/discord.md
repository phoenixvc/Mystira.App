# Discord Integration

**Last Updated**: 2025-12-10
**Status**: Production Ready

This document covers the complete Discord integration for the Mystira platform, including infrastructure, API, and frontend components.

---

## Quick Start

### 1. Enable Discord in API

```bash
# Development (User Secrets)
dotnet user-secrets set "Discord:Enabled" "true"
dotnet user-secrets set "Discord:BotToken" "YOUR_BOT_TOKEN_HERE"

# Production (Azure)
az webapp config appsettings set \
  --resource-group mystira-rg \
  --name mystira-api \
  --settings Discord__Enabled=true
```

### 2. Create Discord Bot

1. Visit [Discord Developer Portal](https://discord.com/developers/applications)
2. Create new application and add bot
3. Enable **Message Content Intent** under Privileged Gateway Intents
4. Copy bot token (store securely!)
5. Invite bot to your server with Send Messages + Embed Links permissions

---

## Architecture Overview

```
+------------------------------------------------------------------+
|  FRONTEND (Blazor PWA)                                            |
|  +--------------------+                                           |
|  | Floating Widget    |  <- Always visible (bottom-right)        |
|  | - Status display   |                                           |
|  | - Send messages    |                                           |
|  +--------------------+                                           |
|  Components: DiscordWidget.razor, IDiscordApiClient               |
+----------------------+--------------------------------------------+
                       | HTTPS (Bearer Token)
                       v
+------------------------------------------------------------------+
|  API LAYER (ASP.NET Core)                                         |
|  Locating Control: "Discord:Enabled" = true/false                |
|                                                                   |
|  Endpoints:                                                       |
|  - GET  /api/discord/status     - Bot status                     |
|  - POST /api/discord/send       - Send message                   |
|  - POST /api/discord/send-embed - Send rich embed                |
|  - GET  /health                 - Health check (includes Discord)|
+----------------------+--------------------------------------------+
                       | (if Enabled=true)
                       v
+------------------------------------------------------------------+
|  INFRASTRUCTURE LAYER (Mystira.App.Infrastructure.Discord)        |
|                                                                   |
|  Port/Adapter Pattern:                                            |
|  - IDiscordBotService (interface/port)                           |
|  - DiscordBotService (Discord.NET adapter)                       |
|  - DiscordBotHostedService (background service)                  |
|  - DiscordOptions (configuration)                                |
|  - DiscordBotHealthCheck (health monitoring)                     |
+----------------------+--------------------------------------------+
                       | Discord API
                       v
                  [Discord Platform]
```

---

## Configuration

### Full Configuration Options

```json
{
  "Discord": {
    "Enabled": false,
    "BotToken": "",
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

### Configuration Sources (Priority Order)

| Source | Use Case | Example |
|--------|----------|---------|
| Azure Key Vault | Production secrets | `Discord--BotToken` |
| App Settings | Azure deployment | `Discord__Enabled=true` |
| User Secrets | Development | `dotnet user-secrets set` |
| appsettings.json | Defaults only | Never store tokens here |

---

## API Endpoints

### Check Discord Status
```http
GET /api/discord/status
Authorization: Bearer {admin-token}
```

Response:
```json
{
  "enabled": true,
  "connected": true,
  "botUsername": "MystiraBot",
  "botId": "123456789012345678"
}
```

### Send Message
```http
POST /api/discord/send
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "channelId": 1234567890123456789,
  "message": "Hello from Mystira!"
}
```

### Send Rich Embed
```http
POST /api/discord/send-embed
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "channelId": 1234567890123456789,
  "title": "Game Session Started",
  "description": "A new adventure begins!",
  "colorRed": 52,
  "colorGreen": 152,
  "colorBlue": 219,
  "footer": "Mystira Adventure Platform",
  "fields": [
    { "name": "Session ID", "value": "session-123", "inline": true },
    { "name": "Player", "value": "Alice", "inline": true }
  ]
}
```

---

## Frontend Widget

### Features
- Floating display (bottom-right corner)
- Collapsible with status indicator
- Real-time status updates (auto-refresh every 30s)
- Send notification form
- Responsive design (mobile/desktop)

### Widget States

**Collapsed (Default):**
```
    +-----+
    |     |  <- Discord icon
    |  *  |  <- Status dot (green/red/gray)
    +-----+
```

**Expanded - Online:**
```
+-------------------------------+
| Discord                     x |
+-------------------------------+
| * Connected as MystiraBot     |
|                               |
| [Send Notification]           |
|                               |
| Last checked: 14:23:45        |
+-------------------------------+
```

### Integration

The widget is included in `MainLayout.razor`:
```razor
<DiscordWidget />
```

---

## Usage in Application Code

### Inject and Use Discord Service

```csharp
public class GameSessionService
{
    private readonly IDiscordBotService? _discord;

    public GameSessionService(IDiscordBotService? discord = null)
    {
        _discord = discord;
    }

    public async Task NotifySessionStart(string sessionName, ulong channelId)
    {
        // Only send if Discord is enabled and connected
        if (_discord?.IsConnected == true)
        {
            await _discord.SendMessageAsync(
                channelId,
                $"New game session started: **{sessionName}**");
        }
    }
}
```

### Send Rich Embeds

```csharp
using Discord;

public async Task SendGameStatusEmbed(ulong channelId, GameSession session)
{
    var embed = new EmbedBuilder()
        .WithTitle("Game Session Update")
        .WithDescription($"Session: {session.Name}")
        .AddField("Status", session.Status, inline: true)
        .AddField("Players", session.PlayerCount, inline: true)
        .WithColor(Color.Green)
        .WithTimestamp(DateTimeOffset.Now)
        .Build();

    await _discord.SendEmbedAsync(channelId, embed);
}
```

---

## Deployment Options

| Option | Cost | Best For |
|--------|------|----------|
| Azure App Service (B1) | ~$55/mo | Always-on, reliable |
| Azure Container Apps | ~$15-30/mo | Scalable, modern |
| Azure Container Instances | ~$10-20/mo | Simple, budget |

### Azure App Service Setup

```bash
# Create App Service
az webapp create \
  --resource-group mystira-rg \
  --plan mystira-plan \
  --name mystira-discord-bot \
  --runtime "DOTNET|9.0"

# Enable Always On (required)
az webapp config set \
  --resource-group mystira-rg \
  --name mystira-discord-bot \
  --always-on true

# Configure via Key Vault reference
az webapp config appsettings set \
  --resource-group mystira-rg \
  --name mystira-api \
  --settings Discord__BotToken="@Microsoft.KeyVault(SecretUri=https://mystira-kv.vault.azure.net/secrets/DiscordBotToken/)"
```

---

## Security Best Practices

1. **Never hardcode bot tokens** - Use Key Vault or User Secrets
2. **Admin-only endpoints** - All Discord API endpoints require Admin role
3. **Managed Identity** - Use Azure Managed Identity for Key Vault access
4. **Rate limiting** - Discord has strict limits; the library handles most cases
5. **Input validation** - Always sanitize user input before sending to Discord

---

## Health Monitoring

### Health Check Endpoint

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

### Startup Logs

When enabled:
```
info: Discord bot integration: ENABLED
info: Discord bot is ready! Logged in as MystiraBot
```

When disabled:
```
info: Discord bot integration: DISABLED
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Bot not connecting | Verify token, check intents in Developer Portal |
| `message.Content` empty | Enable Message Content Intent in Developer Portal |
| 429 rate limit errors | Reduce message frequency, use bulk operations |
| 404 on Discord endpoints | Discord integration is disabled - check `Discord:Enabled` |
| 503 Service Unavailable | Bot enabled but not connected - check token/intents |
| 401 Unauthorized | Missing/invalid admin token |

---

## Future Enhancements

- Slash commands support
- Message reactions
- Thread management
- Voice channel notifications
- Role-based access control
- Webhook integration (alternative to bot)

---

## References

- [Discord.NET Documentation](https://docs.discordnet.dev/)
- [Discord Developer Portal](https://discord.com/developers/docs)
- [Mystira Architecture Guidelines](../architecture/ARCHITECTURAL_RULES.md)
