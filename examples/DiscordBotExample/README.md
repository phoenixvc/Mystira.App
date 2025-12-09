# Discord Bot Example

This is a simple console application demonstrating how to use the Discord bot infrastructure.

## Setup

1. **Create a Discord Bot:**
   - Go to [Discord Developer Portal](https://discord.com/developers/applications)
   - Click "New Application" and name it
   - Go to the "Bot" section and click "Add Bot"
   - Copy your bot token
   - Enable **Message Content Intent** under "Privileged Gateway Intents"

2. **Configure the Bot Token:**
   
   Using User Secrets (recommended for development):
   ```bash
   cd examples/DiscordBotExample
   dotnet user-secrets init
   dotnet user-secrets set "Discord:BotToken" "YOUR_BOT_TOKEN_HERE"
   ```

   Or edit `appsettings.json` (not recommended - don't commit tokens!):
   ```json
   {
     "Discord": {
       "BotToken": "YOUR_BOT_TOKEN_HERE"
     }
   }
   ```

3. **Invite the Bot to Your Server:**
   - In the Discord Developer Portal, go to OAuth2 → URL Generator
   - Select scopes: `bot`
   - Select permissions: `Send Messages`, `Read Message History`
   - Copy the generated URL and open it in a browser
   - Select your server and authorize

4. **Run the Example:**
   ```bash
   dotnet run
   ```

## What This Example Does

- Starts a Discord bot using the hosted service pattern
- Connects to Discord and stays online
- Logs all Discord events and messages (when `LogAllMessages` is true)
- Demonstrates health checks for monitoring bot status

## Extending the Example

To handle messages or commands, you can:

1. **Create a custom message handler:**
   ```csharp
   public class CustomMessageHandler : BackgroundService
   {
       private readonly IDiscordBotService _bot;
       
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           // Custom message handling logic
       }
   }
   ```

2. **Use Discord.NET's command framework:**
   - See Discord.NET documentation for slash commands
   - Implement `InteractionService` for modern Discord interactions

3. **Integrate with application services:**
   - Send game notifications
   - Process adventure commands
   - Sync user data between Discord and Mystira

## Troubleshooting

### "Discord bot token is not configured"
- Make sure you've set the bot token using user secrets or appsettings.json

### "Unauthorized"
- Your bot token may be invalid or expired
- Regenerate the token in Discord Developer Portal

### Bot connects but doesn't see messages
- Enable **Message Content Intent** in Discord Developer Portal
- Set `EnableMessageContentIntent: true` in configuration

### Bot disconnects frequently
- Check your internet connection
- Verify the bot token hasn't been regenerated
- Review logs for error messages
