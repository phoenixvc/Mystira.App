# Mystira.App.Infrastructure.Discord

Discord messaging adapter implementing the messaging port defined by the Application layer. This project serves as a **secondary adapter** in the hexagonal architecture.

## ✅ Hexagonal Architecture - FULLY COMPLIANT

**Layer**: **Infrastructure - Discord Adapter (Secondary/Driven)**

The Infrastructure.Discord layer is a **secondary adapter** (driven adapter) that:
- **Implements** messaging port interface defined in `Application.Ports.Messaging`
- **Provides** Discord bot functionality using Discord.NET
- **Manages** Discord-specific health checks and configuration
- **Abstracts** Discord.NET SDK details from the Application layer
- **ZERO reverse dependencies** - Application never references Infrastructure

**Dependency Flow** (Correct ✅):
```
Domain Layer (Core)
    ↓ references
Application Layer
    ↓ defines
Application.Ports.Messaging (IMessagingService)
    ↑ implemented by
Infrastructure.Discord (THIS - Implementation)
    ↓ uses
Discord.NET SDK
```

**Key Principles**:
- ✅ **Port Implementation** - Implements `IMessagingService` from Application
- ✅ **Technology Adapter** - Adapts Discord.NET to Application needs
- ✅ **Dependency Inversion** - Application defines ports, Infrastructure implements them
- ✅ **Clean Architecture** - No circular dependencies, proper layering
- ✅ **Swappable** - Can be replaced with other messaging platforms (Slack, Teams, etc.)

## Project Structure

- [Mystira Application Suite](#mystira-application-suite)
  - [Deployments](#deployments)
  - [Repository Overview](#repository-overview)
  - [Design System & Visual Identity](#design-system--visual-identity)
  - [Technology Stack](#technology-stack)
  - [🏗️ Architecture & Design Patterns](#️-architecture--design-patterns)
  - [Audit Findings & Governance](#audit-findings--governance)
  - [Getting Started](#getting-started)
  - [Testing & Quality Gates](#testing--quality-gates)
  - [Technical Debt & Future Work](#technical-debt--future-work)
  - [Contributing / PR Checklist](#contributing--pr-checklist)
  - [AI Assistant Integration](#ai-assistant-integration)

**Port Interface** (defined in Application layer):
- `IMessagingService` lives in `Application/Ports/Messaging/`
- Infrastructure.Discord references Application to implement this port

| Label         | Area                                   | Description                                                                             |
| ------------- | -------------------------------------- | --------------------------------------------------------------------------------------- |
| 🧠 Domain      | `src/Mystira.App.Domain`               | Core domain models, enumerations, and shared business logic.                           |
| ☁️ Azure Infra | `src/Mystira.App.Infrastructure.Azure` | Azure configuration, Cosmos DB & Blob Storage adapters.                                 |
| 🌐 Public API  | `src/Mystira.App.Api`                  | ASP.NET Core API serving PWA and mobile clients.                                        |
| 🛡️ Admin API   | `src/Mystira.App.Admin.Api`            | Internal-facing API for content management and moderation.                              |
| 📱 PWA         | `src/Mystira.App.PWA`                  | Blazor WebAssembly PWA with offline assets and haptics.                                 |
| 📊 Ops Console | `tools/Mystira.App.CosmosConsole`    | CLI for Cosmos DB exports and analytics.                                                |

## Design System & Visual Identity

The Mystira brand is built for children (5-12) and parents, balancing playfulness with safety.

- **Primary Color:** `#7c3aed` (Violet) - Used for primary actions and achievements.
- **Neutral Color:** `#1F2937` (Dark Gray) - Foundation for layouts and text.
- **Success Color:** `#10B981` (Green) - Used for badge notifications.
- **UX Principles:** Card-based layouts, high contrast, mobile-first responsiveness, and COPPA-first data handling.

Application defines the platform-agnostic port interface:

- **Frameworks:** .NET 9 (Unified across all projects).
- **Frontend:** Blazor WebAssembly PWA with Service Workers and custom Image Cache.
- **Backend:** ASP.NET Core with Wolverine (CQRS/Messaging) and FluentValidation.
- **Persistence:** Azure Cosmos DB (EF Core 9 provider) and Azure Blob Storage.
- **Resilience:** Polly policies (Retry, Circuit Breaker) applied to all inter-service communication.
- **Storage:** `InMemoryStoreService` for session-state (transient) and JS IndexedDB for binary asset caching.

## 🏗️ Architecture & Design Patterns

Mystira uses **Hexagonal Architecture (Ports & Adapters)** to decouple business logic from infrastructure.

### Hexagonal Model
```
┌────────────────────────────────┐
│  Infrastructure (Adapters)     │
│  • CosmosDB, Blob, WhatsApp    │
└──────────────┬─────────────────┘
               │ implements
               ↓
┌────────────────────────────────┐
│  Application (Ports)           │
│  • Handlers, Interfaces, Specs │
└──────────────┬─────────────────┘
               │ depends on
               ↓
┌────────────────────────────────┐
│  Domain (Core)                 │
│  • Models, Value Objects       │
└────────────────────────────────┘
```

## Audit Findings & Governance

A governed audit (2025-12) identified and resolved the following:

- **BUG-01:** Fixed DFS cycle logic in `CalculateBadgeScoresQueryHandler` to prevent score corruption in circular stories.
- **REF-01:** Standardized storage naming by renaming `IndexedDbService` to `InMemoryStoreService`.
- **PERF-01:** Implemented LRU eviction and 7-day TTL in `imageCacheManager.js` to prevent unbounded storage growth.
- **Governance:** Established strict COPPA compliance guidelines for data handling.

public interface IMessagingService
{
    Task SendMessageAsync(ulong channelId, string message);
    Task SendRichMessageAsync(ulong channelId, RichMessage richMessage);
    Task<bool> IsConnectedAsync();
}
```

Infrastructure.Discord provides the Discord-specific implementation:

```csharp
// Location: Infrastructure.Discord/Services/DiscordBotService.cs
using Mystira.App.Application.Ports.Messaging;  // Port interface ✅
using Discord;
using Discord.WebSocket;

namespace Mystira.App.Infrastructure.Discord.Services;

public class DiscordBotService : IMessagingService  // Implements port ✅
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(
        DiscordSocketClient client,
        ILogger<DiscordBotService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendMessageAsync(ulong channelId, string message)
    {
        var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync(message);
            _logger.LogInformation("Sent message to Discord channel {ChannelId}", channelId);
        }
    }

    public async Task SendRichMessageAsync(ulong channelId, RichMessage richMessage)
    {
        var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
        if (channel != null)
        {
            var embed = new EmbedBuilder()
                .WithTitle(richMessage.Title)
                .WithDescription(richMessage.Description)
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.Now)
                .Build();

            await channel.SendMessageAsync(embed: embed);
            _logger.LogInformation("Sent rich message to Discord channel {ChannelId}", channelId);
        }
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(_client.ConnectionState == ConnectionState.Connected);
    }
}
```

## Usage in Application Layer

Application use cases depend on the port interface, not Discord implementation:

```csharp
// Location: Application/UseCases/Notifications/SendGameNotificationUseCase.cs
using Mystira.App.Application.Ports.Messaging;  // Port ✅

namespace Mystira.App.Application.UseCases.Notifications;

public class SendGameNotificationUseCase
{
    private readonly IMessagingService _messagingService;  // Port interface ✅
    private readonly ILogger<SendGameNotificationUseCase> _logger;

    public SendGameNotificationUseCase(
        IMessagingService messagingService,  // Port ✅
        ILogger<SendGameNotificationUseCase> logger)
    {
        _messagingService = messagingService;
        _logger = logger;
    }

    public async Task ExecuteAsync(ulong channelId, string gameEvent)
    {
        if (!await _messagingService.IsConnectedAsync())
        {
            _logger.LogWarning("Messaging service not connected");
            return;
        }

        var richMessage = new RichMessage
        {
            Title = "Game Event",
            Description = gameEvent
        };

        await _messagingService.SendRichMessageAsync(channelId, richMessage);
        _logger.LogInformation("Game notification sent: {Event}", gameEvent);
    }
}
```

**Benefits**:
- ✅ Application never references Infrastructure.Discord
- ✅ Can swap Discord for Slack/Teams without changing Application
- ✅ Easy to mock for testing
- ✅ Clear separation of concerns

## Dependency Injection

Register Discord implementation in API layer `Program.cs`:

```csharp
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Discord.Services;
using Discord.WebSocket;

// Register Discord client
builder.Services.AddSingleton(sp =>
{
    var config = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
    };
    return new DiscordSocketClient(config);
});

// Register port implementation
builder.Services.AddScoped<IMessagingService, DiscordBotService>();  // Discord adapter ✅

// Add hosted service for bot
builder.Services.AddDiscordBotHostedService();

// Or use extension method
builder.Services.AddDiscordBot(builder.Configuration);
```

For testing, swap with mock implementation:

```csharp
#if DEBUG
// Use console logging for development
builder.Services.AddScoped<IMessagingService, ConsoleMessagingService>();
#else
// Use Discord for production
builder.Services.AddScoped<IMessagingService, DiscordBotService>();
#endif
```

## Configuration

### appsettings.json

```json
{
  "Discord": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "EnableMessageContentIntent": true,
    "EnableGuildMembersIntent": false,
    "CommandPrefix": "!"
  }
}
```

### Azure Key Vault (Recommended for Production)

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Secret: `Discord--BotToken`

### User Secrets (Development)

```bash
dotnet user-secrets set "Discord:BotToken" "YOUR_BOT_TOKEN_HERE"
```

## Getting a Discord Bot Token

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application" and give it a name
3. Go to the "Bot" section
4. Click "Add Bot"
5. Under "Token", click "Copy"
6. Enable required **Privileged Gateway Intents**:
   - **Message Content Intent** (if reading messages)
   - **Server Members Intent** (if tracking members)

## Health Checks

Discord-specific health checks:

```csharp
public class DiscordBotHealthCheck : IHealthCheck
{
    private readonly IMessagingService _messagingService;  // Port ✅

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isConnected = await _messagingService.IsConnectedAsync();

        if (isConnected)
        {
            return HealthCheckResult.Healthy("Discord bot is connected");
        }

        return HealthCheckResult.Unhealthy("Discord bot is not connected");
    }
}
```

Register health checks:

```csharp
builder.Services.AddHealthChecks()
    .AddDiscordBotHealthCheck();
```

Access at:
- `/health` - Comprehensive health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

## Testing

### Unit Testing with Mocked Port

Application use cases can be tested without Discord:

```csharp
[Fact]
public async Task SendGameNotification_WithConnectedService_SendsMessage()
{
    // Arrange
    var mockMessaging = new Mock<IMessagingService>();  // Mock port ✅
    mockMessaging
        .Setup(m => m.IsConnectedAsync())
        .ReturnsAsync(true);
    mockMessaging
        .Setup(m => m.SendRichMessageAsync(
            123456789,
            It.IsAny<RichMessage>()))
        .Returns(Task.CompletedTask);

    var useCase = new SendGameNotificationUseCase(
        mockMessaging.Object,
        mockLogger.Object);

    // Act
    await useCase.ExecuteAsync(123456789, "Player joined the game");

    // Assert
    mockMessaging.Verify(m => m.SendRichMessageAsync(
        123456789,
        It.IsAny<RichMessage>()), Times.Once);
}
```

### Integration Testing with Mock Discord Client

```csharp
[Fact]
public async Task DiscordBotService_SendMessage_Success()
{
    // Arrange
    var mockClient = new Mock<DiscordSocketClient>();
    var mockChannel = new Mock<IMessageChannel>();

    mockClient
        .Setup(c => c.GetChannelAsync(It.IsAny<ulong>(), CacheMode.AllowDownload, null))
        .ReturnsAsync(mockChannel.Object);

    var service = new DiscordBotService(mockClient.Object, mockLogger.Object);

    // Act
    await service.SendMessageAsync(123456789, "Test message");

    // Assert
    mockChannel.Verify(c => c.SendMessageAsync(
        "Test message",
        false,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        MessageFlags.None), Times.Once);
}
```

## Hosting on Azure

### Azure App Service WebJobs (Recommended)

Best for always-on Discord bots with persistent connections.

```bash
# Create App Service
az webapp create --resource-group myRG --plan myPlan --name my-discord-bot

# Enable Always On
az webapp config set --resource-group myRG --name my-discord-bot --always-on true
```

**Cost**: ~$55/month (B1 tier minimum)

### Azure Container Apps

Best for scalable deployments with container orchestration.

```bash
az containerapp create \
  --name my-discord-bot \
  --resource-group myRG \
  --environment myEnv \
  --image myregistry.azurecr.io/discord-bot:latest \
  --min-replicas 1 \
  --max-replicas 1
```

**Cost**: $15-30/month for 1 replica

## Architectural Compliance Verification

Verify that Infrastructure.Discord correctly implements Application port:

```bash
# Check that Infrastructure.Discord references Application
grep "Mystira.App.Application" Mystira.App.Infrastructure.Discord.csproj
# Expected: <ProjectReference Include="..\Mystira.App.Application\...">

# Check that services use Application.Ports namespace
grep -r "using Mystira.App.Application.Ports" Services/
# Expected: All service files import from Application.Ports

# Check NO Infrastructure references in Application
cd ../Mystira.App.Application
grep -r "using Mystira.App.Infrastructure" .
# Expected: (no output - Application never references Infrastructure)
```

**Results**:
- ✅ Infrastructure.Discord references Application (correct direction)
- ✅ Services implement Application.Ports.Messaging interface
- ✅ Application has ZERO Infrastructure references
- ✅ Full dependency inversion achieved

## Alternative Implementations

The port-based architecture allows easy swapping of messaging platforms:

### Console Messaging (Development)
```csharp
public class ConsoleMessagingService : IMessagingService
{
    public Task SendMessageAsync(ulong channelId, string message)
    {
        Console.WriteLine($"[Channel {channelId}] {message}");
        return Task.CompletedTask;
    }

    public Task SendRichMessageAsync(ulong channelId, RichMessage richMessage)
    {
        Console.WriteLine($"[Channel {channelId}] {richMessage.Title}: {richMessage.Description}");
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync() => Task.FromResult(true);
}
```

### Slack Messaging (Alternative Platform)
```csharp
public class SlackMessagingService : IMessagingService
{
    private readonly SlackClient _slackClient;

    public async Task SendMessageAsync(ulong channelId, string message)
    {
        await _slackClient.PostMessageAsync(
            channelId.ToString(),
            message);
    }

    public async Task SendRichMessageAsync(ulong channelId, RichMessage richMessage)
    {
        var blocks = new[]
        {
            new { type = "header", text = richMessage.Title },
            new { type = "section", text = richMessage.Description }
        };

        await _slackClient.PostMessageAsync(
            channelId.ToString(),
            blocks: blocks);
    }
    // ... other methods
}
```

## Security Best Practices

1. **Never commit bot tokens** - Use User Secrets, Key Vault, or environment variables
2. **Use Managed Identity** - Azure Managed Identity for Key Vault access
3. **Implement rate limiting** - Respect Discord rate limits
4. **Enable Application Insights** - Monitor bot health
5. **Minimal intents** - Only enable required gateway intents
6. **Validate input** - Sanitize Discord message input

## Troubleshooting

### Bot Not Connecting
1. Verify bot token is correct
2. Check required intents enabled in Discord Developer Portal
3. Ensure bot is invited to server
4. Check logs for error messages

### Health Check Failing
1. Bot may still be connecting (5-10 seconds)
2. Network connectivity issues
3. Invalid or revoked token
4. Discord API issues

## Related Documentation

- **[Application](../Mystira.App.Application/README.md)** - Defines port interface this layer implements
- **[Infrastructure.Azure](../Mystira.App.Infrastructure.Azure/README.md)** - Similar port adapter pattern
- **[API](../Mystira.App.Api/README.md)** - Registers Discord implementation via DI

## Summary

**What This Layer Does**:
- ✅ Implements messaging port interface from Application.Ports.Messaging
- ✅ Provides Discord bot functionality using Discord.NET
- ✅ Manages Discord-specific health checks and configuration
- ✅ Maintains clean hexagonal architecture

## Technical Debt & Future Work

Implementation progress and outstanding technical debt are tracked in the [Technical Debt Registry](docs/technical-debt-registry.md).

### Planned Future Work
- **Story Protocol Hardening:** Complete actual contract ABI integration for real-time IP attribution.
- **Offline Persistence:** Transition `InMemoryStoreService` to a real IndexedDB-backed persistence layer for the PWA.
- **Adaptive Music:** Enhance the audio engine with engagement-based cross-fading.

## Contributing / PR Checklist
**Key Success Metrics**:
- ✅ **Zero reverse dependencies** - Application never references Infrastructure.Discord
- ✅ **Clean interfaces** - All ports defined in Application layer
- ✅ **Testability** - Use cases can mock messaging service
- ✅ **Swappability** - Can replace Discord with Slack, Teams, or console output

## License

Copyright (c) 2025 Mystira. All rights reserved.
