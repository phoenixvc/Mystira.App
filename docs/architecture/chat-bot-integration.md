# Chat Bot Integration Architecture

## Overview

The Mystira.App platform provides a multi-platform chat bot integration layer following hexagonal (ports & adapters) architecture. This enables the application to communicate via Discord, Microsoft Teams, and WhatsApp through a unified interface.

## C4 Architecture Diagrams

### Level 1: System Context

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              SYSTEM CONTEXT                                   │
└──────────────────────────────────────────────────────────────────────────────┘

    ┌─────────────┐          ┌─────────────┐          ┌─────────────┐
    │   Discord   │          │   Teams     │          │  WhatsApp   │
    │    User     │          │    User     │          │    User     │
    └──────┬──────┘          └──────┬──────┘          └──────┬──────┘
           │                        │                        │
           │ Slash Commands         │ Adaptive Cards         │ Messages
           │ Messages               │ Messages               │ Templates
           │ Embeds                 │ Hero Cards             │
           ▼                        ▼                        ▼
    ┌──────────────────────────────────────────────────────────────┐
    │                                                               │
    │                      Mystira.App System                       │
    │                                                               │
    │  ┌─────────────────────────────────────────────────────────┐ │
    │  │              Chat Bot Integration Layer                  │ │
    │  │                                                          │ │
    │  │   • Send messages to users                               │ │
    │  │   • Receive commands and interactions                    │ │
    │  │   • Broadcast to multiple channels                       │ │
    │  │   • Support ticket system                                │ │
    │  └─────────────────────────────────────────────────────────┘ │
    │                                                               │
    └───────────────────────────┬───────────────────────────────────┘
                                │
                                ▼
    ┌─────────────────────────────────────────────────────────────────┐
    │                    External Services                             │
    │                                                                  │
    │   ┌─────────────┐   ┌─────────────┐   ┌──────────────────────┐  │
    │   │  Discord    │   │  Microsoft  │   │  Azure Communication │  │
    │   │  Gateway    │   │  Bot        │   │  Services            │  │
    │   │  API        │   │  Framework  │   │  (WhatsApp)          │  │
    │   └─────────────┘   └─────────────┘   └──────────────────────┘  │
    └─────────────────────────────────────────────────────────────────┘
```

### Level 2: Container Diagram

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              CONTAINER DIAGRAM                                │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                            Mystira.App System                                │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                        API Layer                                        │ │
│  │  ┌──────────────────┐    ┌──────────────────┐                          │ │
│  │  │  Mystira.App.Api │    │ Mystira.App.     │                          │ │
│  │  │                  │    │ Admin.Api        │                          │ │
│  │  └────────┬─────────┘    └────────┬─────────┘                          │ │
│  └───────────┼───────────────────────┼────────────────────────────────────┘ │
│              │                       │                                       │
│              ▼                       ▼                                       │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     Application Layer                                   │ │
│  │                                                                         │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐   │ │
│  │  │                   Ports (Interfaces)                             │   │ │
│  │  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐    │   │ │
│  │  │  │ IChatBotService │ │IBotCommandService│ │IMessagingService│    │   │ │
│  │  │  └─────────────────┘ └─────────────────┘ └─────────────────┘    │   │ │
│  │  └─────────────────────────────────────────────────────────────────┘   │ │
│  │                                                                         │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐   │ │
│  │  │                    CQRS Handlers                                 │   │ │
│  │  │  • SendDiscordMessageCommandHandler                              │   │ │
│  │  │  • SendDiscordEmbedCommandHandler                                │   │ │
│  │  │  • GetDiscordBotStatusQueryHandler                               │   │ │
│  │  └─────────────────────────────────────────────────────────────────┘   │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│              │                       │                        │              │
│              ▼                       ▼                        ▼              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Infrastructure Layer (Adapters)                      │ │
│  │                                                                         │ │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────┐   │ │
│  │  │ Infrastructure. │ │ Infrastructure. │ │ Infrastructure.         │   │ │
│  │  │ Discord         │ │ Teams           │ │ WhatsApp                │   │ │
│  │  │                 │ │                 │ │                         │   │ │
│  │  │ DiscordBot-     │ │ TeamsBotService │ │ WhatsAppBotService      │   │ │
│  │  │ Service         │ │                 │ │                         │   │ │
│  │  │                 │ │ Uses Bot        │ │ Uses Azure              │   │ │
│  │  │ Uses Discord.NET│ │ Framework       │ │ Communication Services  │   │ │
│  │  └────────┬────────┘ └────────┬────────┘ └────────────┬────────────┘   │ │
│  └───────────┼───────────────────┼───────────────────────┼────────────────┘ │
└──────────────┼───────────────────┼───────────────────────┼──────────────────┘
               │                   │                       │
               ▼                   ▼                       ▼
        ┌─────────────┐    ┌─────────────┐    ┌──────────────────────┐
        │   Discord   │    │  Microsoft  │    │  Azure Communication │
        │   Gateway   │    │  Bot        │    │  Services            │
        │   API       │    │  Connector  │    │  Messages API        │
        └─────────────┘    └─────────────┘    └──────────────────────┘
```

### Level 3: Component Diagram (Infrastructure.Discord)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    COMPONENT DIAGRAM: Infrastructure.Discord                  │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                        Infrastructure.Discord                                │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                          Services                                    │    │
│  │                                                                      │    │
│  │  ┌────────────────────────────────────────────────────────────┐     │    │
│  │  │                    DiscordBotService                        │     │    │
│  │  │                                                             │     │    │
│  │  │  Implements:                                                │     │    │
│  │  │   • IChatBotService                                         │     │    │
│  │  │   • IBotCommandService                                      │     │    │
│  │  │   • IMessagingService                                       │     │    │
│  │  │   • IDisposable                                             │     │    │
│  │  │                                                             │     │    │
│  │  │  Contains:                                                  │     │    │
│  │  │   • DiscordSocketClient (_client)                           │     │    │
│  │  │   • InteractionService (_interactions)                      │     │    │
│  │  │                                                             │     │    │
│  │  │  Methods:                                                   │     │    │
│  │  │   • StartAsync() / StopAsync()                              │     │    │
│  │  │   • SendMessageAsync() / SendEmbedAsync()                   │     │    │
│  │  │   • RegisterCommandsAsync()                                 │     │    │
│  │  │   • SendAndAwaitFirstResponseAsync()  [Broadcast]           │     │    │
│  │  │   • BroadcastWithResponseHandlerAsync()                     │     │    │
│  │  └────────────────────────────────────────────────────────────┘     │    │
│  │                                                                      │    │
│  │  ┌──────────────────────────┐   ┌──────────────────────────────┐   │    │
│  │  │DiscordBotHostedService   │   │ SampleTicketStartupService   │   │    │
│  │  │                          │   │                              │   │    │
│  │  │ BackgroundService that   │   │ Posts sample tickets on      │   │    │
│  │  │ manages bot lifecycle    │   │ startup if configured        │   │    │
│  │  └──────────────────────────┘   └──────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                          Modules                                     │    │
│  │  ┌──────────────────────────────────────────────────────────┐       │    │
│  │  │                     TicketModule                          │       │    │
│  │  │                                                           │       │    │
│  │  │  Slash Commands:                                          │       │    │
│  │  │   • /ticket - Create support ticket                       │       │    │
│  │  │   • /ticket-close - Close and archive ticket              │       │    │
│  │  └──────────────────────────────────────────────────────────┘       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                       Configuration                                  │    │
│  │  ┌──────────────────────────────────────────────────────────┐       │    │
│  │  │                    DiscordOptions                         │       │    │
│  │  │   • BotToken            • GuildId                         │       │    │
│  │  │   • EnableSlashCommands • RegisterCommandsGlobally        │       │    │
│  │  │   • SupportRoleId       • SupportCategoryId               │       │    │
│  │  └──────────────────────────────────────────────────────────┘       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                       Health Checks                                  │    │
│  │  ┌──────────────────────────────────────────────────────────┐       │    │
│  │  │                 DiscordBotHealthCheck                     │       │    │
│  │  │   Reports: Connected, BotUsername, ServerCount            │       │    │
│  │  └──────────────────────────────────────────────────────────┘       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Sequence Diagrams

### 1. Sending a Message via IChatBotService

```
┌────────────────────────────────────────────────────────────────────────────────┐
│              SEQUENCE: Send Message Through Platform-Agnostic Interface         │
└────────────────────────────────────────────────────────────────────────────────┘

┌─────────┐     ┌───────────────┐     ┌───────────────┐     ┌─────────────┐
│ Client  │     │ CQRS Handler  │     │IChatBotService│     │  Platform   │
│ (API)   │     │               │     │ (Discord/     │     │  Gateway    │
│         │     │               │     │  Teams/WA)    │     │             │
└────┬────┘     └───────┬───────┘     └───────┬───────┘     └──────┬──────┘
     │                  │                     │                    │
     │ SendMessageCmd   │                     │                    │
     │─────────────────>│                     │                    │
     │                  │                     │                    │
     │                  │ Check IsConnected   │                    │
     │                  │────────────────────>│                    │
     │                  │                     │                    │
     │                  │      true           │                    │
     │                  │<────────────────────│                    │
     │                  │                     │                    │
     │                  │ SendMessageAsync()  │                    │
     │                  │────────────────────>│                    │
     │                  │                     │                    │
     │                  │                     │ POST /messages     │
     │                  │                     │───────────────────>│
     │                  │                     │                    │
     │                  │                     │       200 OK       │
     │                  │                     │<───────────────────│
     │                  │                     │                    │
     │                  │    Task completed   │                    │
     │                  │<────────────────────│                    │
     │                  │                     │                    │
     │ (Success, msg)   │                     │                    │
     │<─────────────────│                     │                    │
     │                  │                     │                    │
```

### 2. Broadcast First Responder Pattern (Discord)

```
┌────────────────────────────────────────────────────────────────────────────────┐
│          SEQUENCE: Broadcast and Await First Response (Discord)                 │
└────────────────────────────────────────────────────────────────────────────────┘

┌─────────┐  ┌───────────────┐  ┌──────────────┐  ┌─────────────┐  ┌─────────┐
│ Caller  │  │DiscordBot-    │  │ Discord      │  │ Channel A   │  │Channel B│
│         │  │ Service       │  │ Gateway      │  │ (User)      │  │ (User)  │
└────┬────┘  └───────┬───────┘  └──────┬───────┘  └──────┬──────┘  └────┬────┘
     │               │                 │                 │              │
     │ SendAndAwait  │                 │                 │              │
     │ FirstResponse │                 │                 │              │
     │ ([A,B], msg,  │                 │                 │              │
     │  30s)         │                 │                 │              │
     │──────────────>│                 │                 │              │
     │               │                 │                 │              │
     │               │ POST message    │                 │              │
     │               │ to Channel A    │                 │              │
     │               │────────────────>│                 │              │
     │               │                 │   Message       │              │
     │               │                 │────────────────>│              │
     │               │                 │                 │              │
     │               │ POST message    │                 │              │
     │               │ to Channel B    │                 │              │
     │               │────────────────>│                 │              │
     │               │                 │     Message     │              │
     │               │                 │────────────────────────────────>│
     │               │                 │                 │              │
     │               │ Subscribe to    │                 │              │
     │               │ MessageReceived │                 │              │
     │               │ event           │                 │              │
     │               │                 │                 │              │
     │               │     [waiting for response...]     │              │
     │               │                 │                 │              │
     │               │                 │     Reply!      │              │
     │               │                 │<────────────────│              │
     │               │                 │                 │              │
     │               │ MessageReceived │                 │              │
     │               │ (from A)        │                 │              │
     │               │<────────────────│                 │              │
     │               │                 │                 │              │
     │               │ Unsubscribe     │                 │              │
     │               │ event handler   │                 │              │
     │               │                 │                 │              │
     │ FirstResponder│                 │                 │              │
     │ Result        │                 │                 │              │
     │ {Channel A,   │                 │                 │              │
     │  Response...} │                 │                 │              │
     │<──────────────│                 │                 │              │
     │               │                 │                 │              │
```

### 3. Slash Command Registration and Execution

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                SEQUENCE: Slash Command Registration & Execution                 │
└────────────────────────────────────────────────────────────────────────────────┘

┌─────────┐  ┌───────────────┐  ┌──────────────┐  ┌─────────────┐  ┌─────────┐
│Startup  │  │DiscordBot-    │  │ Interaction- │  │ Discord     │  │  User   │
│Service  │  │ Service       │  │ Service      │  │ Gateway     │  │         │
└────┬────┘  └───────┬───────┘  └──────┬───────┘  └──────┬──────┘  └────┬────┘
     │               │                 │                 │              │
     │ RegisterCommands               │                 │              │
     │ Async(assembly)                │                 │              │
     │──────────────>│                 │                 │              │
     │               │                 │                 │              │
     │               │ AddModulesAsync │                 │              │
     │               │ (Ticket Module) │                 │              │
     │               │────────────────>│                 │              │
     │               │                 │                 │              │
     │               │ RegisterCommands│                 │              │
     │               │ ToGuildAsync()  │                 │              │
     │               │────────────────>│                 │              │
     │               │                 │                 │              │
     │               │                 │ PUT /commands   │              │
     │               │                 │────────────────>│              │
     │               │                 │                 │              │
     │               │                 │     200 OK      │              │
     │               │                 │<────────────────│              │
     │               │                 │                 │              │
     │               │                 │                 │              │
     │               │       ... Later ...               │              │
     │               │                 │                 │              │
     │               │                 │                 │   /ticket    │
     │               │                 │                 │<─────────────│
     │               │                 │                 │              │
     │               │ InteractionCreated                │              │
     │               │<────────────────────────────────────              │
     │               │                 │                 │              │
     │               │ ExecuteCommand- │                 │              │
     │               │ Async(context)  │                 │              │
     │               │────────────────>│                 │              │
     │               │                 │                 │              │
     │               │                 │ TicketModule.   │              │
     │               │                 │ CreateTicket()  │              │
     │               │                 │                 │              │
     │               │                 │ RespondAsync    │              │
     │               │                 │────────────────>│              │
     │               │                 │                 │   Response   │
     │               │                 │                 │─────────────>│
```

### 4. Multi-Platform Service Registration

```
┌────────────────────────────────────────────────────────────────────────────────┐
│              SEQUENCE: Multi-Platform Bot Registration (DI)                     │
└────────────────────────────────────────────────────────────────────────────────┘

┌─────────┐  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐
│Startup  │  │ServiceCollection│ │ Discord Ext  │  │  Teams Ext   │
│         │  │               │  │               │  │              │
└────┬────┘  └───────┬───────┘  └───────┬───────┘  └──────┬───────┘
     │               │                  │                 │
     │ AddDiscordBot │                  │                 │
     │ (config)      │                  │                 │
     │──────────────>│                  │                 │
     │               │                  │                 │
     │               │ Configure<>      │                 │
     │               │────────────────>│                 │
     │               │                  │                 │
     │               │ Singleton:       │                 │
     │               │ DiscordBotService│                 │
     │               │ as IChatBot,     │                 │
     │               │    IBotCommand   │                 │
     │               │────────────────>│                 │
     │               │                  │                 │
     │               │                  │                 │
     │ AddTeamsBotKeyed                 │                 │
     │ (config, "teams")                │                 │
     │──────────────>│                  │                 │
     │               │                  │                 │
     │               │ Configure<>      │                 │
     │               │─────────────────────────────────>│
     │               │                  │                 │
     │               │ KeyedSingleton:  │                 │
     │               │ TeamsBotService  │                 │
     │               │ key="teams"      │                 │
     │               │─────────────────────────────────>│
     │               │                  │                 │
     │               │                  │                 │
     │ // Later: Resolve keyed service │                 │
     │               │                  │                 │
     │ GetKeyed<IChatBotService>       │                 │
     │ ("teams")     │                  │                 │
     │──────────────>│                  │                 │
     │               │                  │                 │
     │ TeamsBotService                  │                 │
     │<──────────────│                  │                 │
```

## Interface Contracts

### IChatBotService

| Method | Description | Discord | Teams | WhatsApp |
|--------|-------------|---------|-------|----------|
| `StartAsync()` | Initialize connection | WebSocket connect | Credential init | Client init |
| `StopAsync()` | Graceful shutdown | Logout + disconnect | Clear state | Clear client |
| `SendMessageAsync()` | Send text message | Channel.SendMessage | ConnectorClient.Send | TextNotificationContent |
| `SendEmbedAsync()` | Send rich message | EmbedBuilder | HeroCard | Formatted text |
| `ReplyToMessageAsync()` | Reply to specific msg | MessageReference | Same conversation | Same number |
| `IsConnected` | Connection status | ConnectionState | _isConnected flag | _client != null |
| `GetStatus()` | Bot status info | Full support | Partial | Partial |
| `SendAndAwaitFirstResponseAsync()` | Broadcast pattern | Full support | Send only | Send only |

### IBotCommandService

| Method | Description | Discord | Teams | WhatsApp |
|--------|-------------|---------|-------|----------|
| `RegisterCommandsAsync()` | Load command modules | InteractionService.AddModules | N/A (portal) | N/A |
| `RegisterCommandsToServerAsync()` | Server-specific | RegisterToGuild | N/A | N/A |
| `RegisterCommandsGloballyAsync()` | Global commands | RegisterGlobally | N/A | N/A |
| `IsEnabled` | Commands enabled | Config flag | Always false | Always false |
| `RegisteredModuleCount` | Module count | _interactions.Modules.Count | 0 | 0 |

## Configuration

### appsettings.json Structure

```json
{
  "Discord": {
    "Enabled": true,
    "BotToken": "",
    "GuildId": 0,
    "EnableSlashCommands": true,
    "RegisterCommandsGlobally": false,
    "SupportRoleId": 0,
    "SupportCategoryId": 0
  },
  "Teams": {
    "Enabled": false,
    "MicrosoftAppId": "",
    "MicrosoftAppPassword": "",
    "TenantId": "",
    "EnableAdaptiveCards": true
  },
  "WhatsApp": {
    "Enabled": false,
    "ConnectionString": "",
    "ChannelRegistrationId": "",
    "PhoneNumberId": ""
  }
}
```

## Service Registration Patterns

### Single Platform (Default)
```csharp
services.AddDiscordBot(configuration);  // Registers as IChatBotService
```

### Multiple Platforms (Keyed Services)
```csharp
services.AddDiscordBot(configuration);              // Default
services.AddTeamsBotKeyed(configuration, "teams");  // Keyed
services.AddWhatsAppBotKeyed(configuration, "whatsapp");

// Resolve specific platform:
var teamsBot = serviceProvider.GetKeyedService<IChatBotService>("teams");
```

### Platform Factory Pattern
```csharp
public interface IChatBotFactory
{
    IChatBotService GetBot(string platform);
}
```
