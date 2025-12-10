# Discord Integration - API Configuration

## Overview

The Discord bot has been integrated into the Mystira.App.Api with a locating control that allows you to enable/disable the feature via configuration.

## Configuration

### appsettings.json

Add the following section to your `appsettings.json`:

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

### Enabling Discord Integration

1. **Set `Enabled` to `true`**:
   ```json
   "Discord": {
     "Enabled": true,
     // ... other settings
   }
   ```

2. **Configure Bot Token** (via Azure Key Vault or User Secrets):
   ```bash
   # Development (User Secrets)
   dotnet user-secrets set "Discord:BotToken" "YOUR_BOT_TOKEN_HERE"
   
   # Production (Azure Key Vault)
   az keyvault secret set \
     --vault-name mystira-keyvault \
     --name Discord--BotToken \
     --value "YOUR_BOT_TOKEN_HERE"
   ```

3. **Restart the API**

### Disabling Discord Integration

Simply set `Enabled` to `false` in configuration:

```json
"Discord": {
  "Enabled": false
}
```

No other changes needed. The API will start without Discord services.

## API Endpoints

When Discord is enabled, the following endpoints become available:

### Check Discord Status
```http
GET /api/discord/status
Authorization: Bearer {admin-token}
```

**Response:**
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
    {
      "name": "Session ID",
      "value": "session-123",
      "inline": true
    },
    {
      "name": "Player",
      "value": "Alice",
      "inline": true
    }
  ]
}
```

## Health Checks

When Discord is enabled, a health check is added:

```http
GET /health
```

**Response includes Discord status:**
```json
{
  "status": "Healthy",
  "results": {
    "blob_storage": {
      "status": "Healthy"
    },
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

## Usage Example

### From Application Code

Inject `IDiscordBotService` (if enabled):

```csharp
public class GameSessionService
{
    private readonly IDiscordBotService? _discordBot;
    
    public GameSessionService(IDiscordBotService? discordBot = null)
    {
        _discordBot = discordBot;
    }
    
    public async Task NotifySessionStart(string sessionName, ulong channelId)
    {
        // Only send if Discord is enabled and connected
        if (_discordBot?.IsConnected == true)
        {
            await _discordBot.SendMessageAsync(
                channelId, 
                $"🎲 New game session started: **{sessionName}**");
        }
    }
}
```

### From External Systems

Use the REST API:

```bash
# Get status
curl -X GET https://mystira-api.azurewebsites.net/api/discord/status \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"

# Send notification
curl -X POST https://mystira-api.azurewebsites.net/api/discord/send \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": 1234567890123456789,
    "message": "Test message from API"
  }'
```

## Security

- ⚠️ **Admin Only**: All Discord endpoints require `Admin` role
- 🔒 **Bot Token**: Store securely in Azure Key Vault (never in code)
- ✅ **Optional**: Discord can be disabled without affecting other features
- 🔐 **Health Check**: Publicly accessible at `/health` (consider restrictions)

## Deployment

### Azure App Service

1. **Enable Discord** via App Settings:
   ```bash
   az webapp config appsettings set \
     --resource-group mystira-rg \
     --name mystira-api \
     --settings Discord__Enabled=true
   ```

2. **Set Bot Token** via Key Vault reference:
   ```bash
   az webapp config appsettings set \
     --resource-group mystira-rg \
     --name mystira-api \
     --settings Discord__BotToken="@Microsoft.KeyVault(SecretUri=https://mystira-kv.vault.azure.net/secrets/DiscordBotToken/)"
   ```

3. **Restart App Service**

### Docker/Container

Set environment variables:

```dockerfile
ENV Discord__Enabled=true
ENV Discord__BotToken=${DISCORD_BOT_TOKEN}
```

Or pass at runtime:

```bash
docker run -e Discord__Enabled=true \
           -e Discord__BotToken="YOUR_TOKEN" \
           mystira-api:latest
```

## Monitoring

Check logs for Discord status:

```
info: Microsoft.Hosting.Lifetime[0]
      Using Azure Cosmos DB (Cloud Database)
info: Microsoft.Hosting.Lifetime[0]
      Discord bot integration: ENABLED
info: Discord.Net[0]
      Discord bot is ready! Logged in as MystiraBot
```

If disabled:

```
info: Microsoft.Hosting.Lifetime[0]
      Discord bot integration: DISABLED
```

## Troubleshooting

### Discord endpoints return 404

- Discord integration is disabled
- Check `Discord:Enabled` in configuration

### Discord endpoints return 503 Service Unavailable

- Bot is enabled but not connected
- Check bot token is valid
- Verify intents are enabled in Discord Developer Portal
- Check logs for connection errors

### Endpoints return 401 Unauthorized

- Missing or invalid admin token
- Discord endpoints require `Admin` role

## Future Enhancements

- Slash commands support
- Message reactions
- Thread management
- Voice channel notifications
- Role-based access to specific Discord features
