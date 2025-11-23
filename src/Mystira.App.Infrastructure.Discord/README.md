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

## 🔍 Architectural Analysis

### Current State Assessment

**File Count**: 6 C# files (very small, focused)
**Project References**: 0 (none!)
- Domain ✅ (not needed for Discord bot)
- Application ❌ (missing - should reference for port interfaces)

**Dependencies**:
- Discord.Net ✅ (Discord SDK)
- Microsoft.Extensions.* ✅ (DI, Health Checks, Hosting, Logging)

**Folders**:
- Services/ ✅ (Discord bot service and hosted service)
- HealthChecks/ ✅ (Discord health monitoring)
- Configuration/ ✅ (Discord options)
- ServiceCollectionExtensions ✅ (DI registration)

### ⚠️ Architectural Issues Found

#### 1. **Port Interface in Infrastructure Layer** (MEDIUM)
**Location**: `Services/IDiscordBotService.cs`

**Issue**: Port interface (abstraction) is defined in Infrastructure project:
```csharp
// Currently in Infrastructure.Discord/Services/
public interface IDiscordBotService  // This is a PORT!
{
    Task SendMessageAsync(ulong channelId, string message);
    Task SendEmbedAsync(ulong channelId, Embed embed);
    bool IsConnected { get; }
    // ...
}
```

**Impact**:
- ⚠️ Violates Dependency Inversion Principle
- ⚠️ Application layer would need to reference Infrastructure to use the interface
- ⚠️ Port (abstraction) and Adapter (implementation) in same project
- ⚠️ Can't easily swap implementations (e.g., Slack adapter for different messaging)

**Recommendation**:
- **MOVE** `IDiscordBotService` → `Application/Ports/Messaging/IMessagingService.cs`
- **KEEP** implementation (`DiscordBotService`) in Infrastructure.Discord
- **ADD** Application project reference to Infrastructure.Discord
- Infrastructure implements ports defined in Application
- Rename interface to be platform-agnostic (e.g., `IMessagingService` instead of `IDiscordBotService`)

**Correct Structure**:
```
Application/Ports/Messaging/
├── IMessagingService.cs               # Port interface (platform-agnostic)

Infrastructure.Discord/Services/
├── DiscordBotService.cs               # Adapter (implements IMessagingService)
```

#### 2. **Missing Application Reference** (MEDIUM)
**Location**: `Mystira.App.Infrastructure.Discord.csproj`

**Issue**: Infrastructure.Discord has NO project references:
```xml
<ItemGroup>
  <!-- No project references at all -->
</ItemGroup>
```

**Impact**:
- ⚠️ Can't implement ports defined in Application
- ⚠️ Forces port interfaces to live in Infrastructure (wrong layer)
- ⚠️ Breaks hexagonal architecture dependency flow

**Recommendation**:
- **ADD** reference to Application project
- After moving interface to Application/Ports
- Correct dependency flow: Infrastructure → Application → Domain
- Discord bot doesn't need Domain reference (no domain entities needed)

**Example**:
```diff
+ <ItemGroup>
+   <ProjectReference Include="..\Mystira.App.Application\Mystira.App.Application.csproj" />
+ </ItemGroup>
```

#### 3. **Platform-Specific Interface Name** (LOW)
**Location**: `Services/IDiscordBotService.cs`

**Issue**: Interface name ties abstraction to Discord implementation

**Impact**:
- ⚠️ Makes it harder to swap messaging platforms
- ⚠️ Interface name should be implementation-agnostic

**Recommendation**:
- Rename `IDiscordBotService` → `IMessagingService` or `INotificationService`
- Keep implementation name as `DiscordBotService` (specific)
- Allows for future `SlackBotService`, `TeamsB otService`, etc.

### ✅ What's Working Well

1. **Zero Project Dependencies** - Very clean, no coupling to other projects
2. **Small and Focused** - Only 6 files, does one thing well
3. **Proper Abstractions** - Has interface and implementation separated
4. **Health Checks** - Monitoring infrastructure
5. **Hosted Service** - Background service for continuous operation
6. **Configuration** - Flexible config via appsettings/Key Vault
7. **Security Best Practices** - Token management, minimal intents
8. **Excellent Documentation** - Deployment guides, troubleshooting, examples

## 📋 Refactoring TODO

### 🟡 High Priority

- [ ] **Move port interface to Application layer**
  - Move `Services/IDiscordBotService.cs` → `Application/Ports/Messaging/IMessagingService.cs`
  - Rename to platform-agnostic name (`IMessagingService`)
  - Location: `Infrastructure.Discord/Services/IDiscordBotService.cs`

- [ ] **Add Application project reference**
  - Add `<ProjectReference Include="..\Mystira.App.Application\..." />`
  - Update implementation to reference Application port
  - Location: `Mystira.App.Infrastructure.Discord.csproj`

- [ ] **Update implementation to use Application port**
  - `DiscordBotService : IMessagingService` (from Application)
  - Remove local interface definition
  - Keep implementation details Discord-specific

### 🟢 Medium Priority

- [ ] **Consider platform-agnostic port design**
  - Design `IMessagingService` to support multiple platforms
  - Could support Discord, Slack, Teams, etc.
  - Common operations: SendMessage, SendRichMessage, etc.

### 🔵 Low Priority

- [ ] **Add alternative messaging adapters**
  - Create `Infrastructure.Slack` for Slack integration
  - Create `Infrastructure.Teams` for Microsoft Teams
  - All implement same `IMessagingService` port

- [ ] **Add integration tests**
  - Test Discord bot service with mock Discord client
  - Test health checks
  - Verify hosted service lifecycle

## 💡 Recommendations

### Immediate Actions
1. **Move port interface to Application/Ports** - Correct dependency inversion
2. **Add Application reference** - Enable proper layering
3. **Rename to platform-agnostic name** - Support future messaging platforms

### Short-term
1. **Design generic messaging port** - Support multiple platforms
2. **Add port documentation** - Explain messaging abstraction
3. **Integration tests** - Mock Discord client for testing

### Long-term
1. **Multi-platform support** - Add Slack, Teams adapters
2. **Slash commands** - Modern Discord interaction model
3. **Event-driven architecture** - Publish domain events to Discord

## 📊 SWOT Analysis

### Strengths 💪
- ✅ **Zero Project Dependencies** - Completely decoupled
- ✅ **Small and Focused** - Only 6 files, single responsibility
- ✅ **Clean Separation** - Interface and implementation separated
- ✅ **Health Checks** - Proper monitoring
- ✅ **Hosted Service** - Background operation support
- ✅ **Security Best Practices** - Token management, minimal intents
- ✅ **Excellent Documentation** - Deployment, troubleshooting, examples
- ✅ **Modern Discord.NET** - Latest SDK version

### Weaknesses ⚠️
- ⚠️ **Port Interface Misplaced** - Should be in Application layer
- ⚠️ **Missing Application Reference** - Can't implement Application ports
- ⚠️ **Platform-Specific Interface** - Name tied to Discord
- ⚠️ **No Tests** - Missing integration tests

### Opportunities 🚀
- 📈 **Multi-Platform Messaging** - Support Slack, Teams, etc.
- 📈 **Slash Commands** - Modern Discord interactions
- 📈 **Event-Driven** - Domain events → Discord notifications
- 📈 **Rich Embeds** - Enhanced notification formatting
- 📈 **Bot Commands** - Interactive game commands
- 📈 **Thread Support** - Organized game session discussions

### Threats 🔒
- ⚡ **Discord API Changes** - Discord.NET SDK updates required
- ⚡ **Rate Limiting** - Discord rate limits can impact functionality
- ⚡ **Intent Verification** - Message Content Intent requires verification for 100+ servers
- ⚡ **Hosting Costs** - Always-on hosting required for bots

### Risk Mitigation
1. **Abstract with ports** - Make messaging platform swappable
2. **Implement rate limiting** - Respect Discord API limits
3. **Monitor usage** - Track message counts and intent usage
4. **Pin SDK versions** - Control Discord.NET updates

## Related Documentation

- **[Application Layer](../Mystira.App.Application/README.md)** - Where port interfaces belong
- **[Infrastructure.Azure](../Mystira.App.Infrastructure.Azure/README.md)** - Similar port interface pattern
- **[Main README](../../README.md)** - Project overview

## License

This library is part of the Mystira.App project and follows the same license.
