# Discord Ticket Bot (C#) — Setup & Instructions

This README consolidates the instructions we covered in this chat for building a Discord bot that:
- Posts general chat messages.
- Creates **dedicated private ticket channels** where support staff can reply to users.
- Posts a **sample ticket message on startup** (configurable).
- Uses `appsettings.json` + `appsettings.Development.json` (with secrets in Development).

---

## What a webhook can and can’t do

A Discord webhook can **only post messages** to a specific channel.
It **cannot** create channels/threads or manage permissions.

To create real tickets inside Discord, you need a **bot token**.

---

## Create a bot in the Developer Portal

1. Open the Discord Developer Portal.
2. Click **New Application**.
3. Name it and create it.
4. Go to **Bot** in the sidebar.
5. Click **Add Bot**.
6. Copy/reset your **bot token**.

**Security**
- Never commit the token to source control.
- Store it in `appsettings.Development.json` (per your chosen approach).

---

## Private app + authorization link error

If you see:

> “Private application cannot have a default authorization link”

Fix it by:
1. **Installation → Install Link → None**
2. Then **Bot → Public Bot → OFF**

This removes the default install link so the app can be private.

---

## One-time invite link (manual)

Even with a private app, you can generate a manual install link when needed:

1. Go to **OAuth2 → URL Generator**
2. Scopes:
    - `bot`
    - `applications.commands` (if you want slash commands)
3. Choose required permissions (see below).
4. Copy the generated URL and open it in a browser.
5. You’ll see:
    - **Pick your server**
    - **Authorize**
    - Confirm permissions

If you don’t see “Pick your server”:
- Ensure you’re logged in to Discord.
- You must have **Manage Server** on the target server.
- Use a full browser (not an in-app browser).

---

## Permissions to grant the bot

### For posting general chats
Minimum:
- View Channels
- Send Messages
- Read Message History

Recommended:
- Embed Links
- Attach Files (if needed)
- Add Reactions (optional)

### For creating **channel-based tickets**
Add:
- **Manage Channels**

Nice-to-have:
- Manage Messages

Avoid Administrator unless you’re testing.

---

## What the IDs mean

You will configure these IDs:

- **SupportRoleId**  
  The role that can see/respond to tickets (e.g., Support, Mods, Helpdesk).

- **SupportCategoryId**  
  The category where ticket channels are created (e.g., “Support Tickets”).
  Set to `0` if you don’t want a category.

- **SupportIntakeChannelId**  
  A normal text channel used for:
    - Startup sample ticket post
    - Optional logging/announcements  
      Example: `#support-intake` or `#bot-testing`.

---

## How to copy IDs

1. Enable **Developer Mode**:
    - Discord **User Settings → Advanced → Developer Mode**
2. Copy:
    - **Role ID**: Server Settings → Roles → right-click role → **Copy ID**
    - **Category ID**: right-click category name → **Copy ID**
    - **Channel ID**: right-click channel → **Copy ID**
    - **Guild ID**: right-click server icon → **Copy ID**

---

## Configuration files

### `appsettings.json` (safe defaults)

```json
{
  "Discord": {
    "Token": null,
    "GuildId": 0,
    "SupportRoleId": 0,
    "SupportCategoryId": 0,
    "SupportIntakeChannelId": 0,
    "PostSampleTicketOnStartup": true,
    "RegisterGloballyIfNoGuildId": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Discord": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

### `appsettings.Development.json` (secrets live here)

```json
{
  "Discord": {
    "Token": "YOUR_DEV_BOT_TOKEN",
    "GuildId": 123456789012345678,
    "SupportRoleId": 234567890123456789,
    "SupportCategoryId": 345678901234567890,
    "SupportIntakeChannelId": 456789012345678901,
    "PostSampleTicketOnStartup": true,
    "RegisterGloballyIfNoGuildId": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Discord": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

### `.gitignore`

```gitignore
appsettings.Development.json
```

---

## Settings class

```csharp
public sealed class DiscordSettings
{
    public string? Token { get; set; }

    public ulong GuildId { get; set; }
    public ulong SupportRoleId { get; set; }
    public ulong SupportCategoryId { get; set; }
    public ulong SupportIntakeChannelId { get; set; }

    public bool PostSampleTicketOnStartup { get; set; } = true;
    public bool RegisterGloballyIfNoGuildId { get; set; } = false;
}
```

---

## Program.cs (your current style) — with startup sample poster

This keeps your non-Generic Host setup and triggers the sample ticket post in `OnReadyAsync`.

```csharp
using System.Reflection;
using Discord;
using Discord.ExampleBot;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Program
{
    private DiscordSocketClient _client = null!;
    private InteractionService _interactions = null!;
    private IServiceProvider _services = null!;

    public static Task Main(string[] args) => new Program().RunAsync();

    public async Task RunAsync()
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var discordSettings = configuration.GetSection("Discord").Get<DiscordSettings>()
                              ?? throw new InvalidOperationException("Discord settings missing.");

        if (string.IsNullOrWhiteSpace(discordSettings.Token))
            throw new InvalidOperationException("Discord:Token not set.");

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        };

        _client = new DiscordSocketClient(config);
        _interactions = new InteractionService(_client.Rest);

        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(discordSettings)
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .AddSingleton<SampleTicketStartupService>()
            .BuildServiceProvider();

        _client.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };
        _interactions.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };

        _client.Ready += () => OnReadyAsync(discordSettings);
        _client.InteractionCreated += async interaction =>
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        };

        await _client.LoginAsync(TokenType.Bot, discordSettings.Token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task OnReadyAsync(DiscordSettings settings)
    {
        await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        if (settings.GuildId != 0)
        {
            await _interactions.RegisterCommandsToGuildAsync(settings.GuildId);
            Console.WriteLine($"Slash commands registered to guild {settings.GuildId}");
        }
        else if (settings.RegisterGloballyIfNoGuildId)
        {
            await _interactions.RegisterCommandsGloballyAsync();
            Console.WriteLine("Slash commands registered globally");
        }
        else
        {
            Console.WriteLine("No GuildId provided. Commands not registered.");
        }

        var startup = _services.GetRequiredService<SampleTicketStartupService>();
        await startup.PostSampleTicketIfEnabledAsync();
    }
}
```

---

## Sample ticket startup poster service

Posts a single sample message to your intake channel on startup when enabled.

```csharp
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Threading;

public sealed class SampleTicketStartupService
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordSettings _settings;
    private readonly ILogger<SampleTicketStartupService> _logger;

    private int _hasRun = 0;

    public SampleTicketStartupService(
        DiscordSocketClient client,
        DiscordSettings settings,
        ILogger<SampleTicketStartupService> logger)
    {
        _client = client;
        _settings = settings;
        _logger = logger;
    }

    public async Task PostSampleTicketIfEnabledAsync()
    {
        if (!_settings.PostSampleTicketOnStartup)
        {
            _logger.LogInformation("PostSampleTicketOnStartup is false. Skipping.");
            return;
        }

        if (_settings.SupportIntakeChannelId == 0)
        {
            _logger.LogWarning("SupportIntakeChannelId not set. Skipping startup sample ticket post.");
            return;
        }

        if (Interlocked.Exchange(ref _hasRun, 1) == 1)
            return;

        var channel = _client.GetChannel(_settings.SupportIntakeChannelId) as IMessageChannel;
        if (channel == null)
        {
            _logger.LogWarning("Could not resolve intake channel ID {ChannelId}", _settings.SupportIntakeChannelId);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("🧪 Sample Ticket (Startup)")
            .WithDescription(
                "Startup smoke test message.\n\n" +
                "**User:** test_user\n" +
                "**Issue:** Bot startup + permissions check\n" +
                "**Priority:** Low"
            )
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await channel.SendMessageAsync(embed: embed);

        _logger.LogInformation("Posted startup sample ticket to intake channel.");
    }
}
```

---

## Ticket creation (dedicated private channel)

The goal: create a **private channel** where support can reply directly to the user.

High-level flow:
1. User runs `/ticket`.
2. Bot creates a channel under `SupportCategoryId`.
3. Bot sets permission overwrites:
    - Deny everyone.
    - Allow the requesting user.
    - Allow the support role.
4. Bot posts an intro message.

**Essential overwrites:**
- `@everyone`: deny `ViewChannel`
- user: allow `ViewChannel`, `SendMessages`, `ReadMessageHistory`
- support role: allow same + optionally `ManageChannel`

---

## GuildId guidance

Set **GuildId** to your **test server ID** for development so slash commands register quickly.

- Right-click your server icon → **Copy ID**
- Put it in `appsettings.Development.json`.

If you want to avoid guild-specific registration:
- Set `GuildId` to `0` and use `RegisterGloballyIfNoGuildId`.

---

## Recommended server setup

1. Create a role: **Support**
2. Create a category: **Support Tickets**
3. Create a channel: **#support-intake**
4. Invite your bot with:
    - View Channels
    - Send Messages
    - Read Message History
    - Manage Channels
    - (Optional) Manage Messages
    - Embed Links

---

## Summary

You now have:
- A private Discord app configuration approach.
- A manual OAuth invite flow for your private app.
- Least-privilege permissions.
- Config-driven IDs and secrets.
- A C# bot that:
    - registers slash commands
    - can create private ticket channels
    - posts a sample intake message on startup

---

## Next easy upgrades (optional)

- Add a `/ticket-close` command to archive/delete tickets.
- Add a button-based “Open Ticket” panel in `#support-intake`.
- Add logging of ticket creation events to the intake channel.


---

## Giving the ticket creator access to the dedicated channel

To ensure the **user who created the ticket** can see and respond in their ticket channel, add a **user-specific permission overwrite** when you create the channel.

Use this overwrite set (channel-based tickets):

```csharp
var overwrites = new Overwrite[]
{
    // Hide from everyone
    new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role,
        new OverwritePermissions(viewChannel: PermValue.Deny)),

    // Allow the ticket creator
    new Overwrite(user.Id, PermissionTarget.User,
        new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow,
            readMessageHistory: PermValue.Allow,
            attachFiles: PermValue.Allow,
            embedLinks: PermValue.Allow)),

    // Allow support staff
    new Overwrite(_settings.SupportRoleId, PermissionTarget.Role,
        new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow,
            readMessageHistory: PermValue.Allow,
            manageChannel: PermValue.Allow,
            manageMessages: PermValue.Allow))
};
```

Then apply it during channel creation:

```csharp
var newChannel = await guild.CreateTextChannelAsync(channelName, props =>
{
    if (_settings.SupportCategoryId != 0)
        props.CategoryId = _settings.SupportCategoryId;

    props.PermissionOverwrites = overwrites;

    // Handy for later lookups
    props.Topic = $"ticket | user:{user.Id} | created:{DateTimeOffset.UtcNow:O}";
});
```

### Notes

- Your bot must have **Manage Channels** permission.
- If you use a category, make sure its permission overwrites do not conflict with these channel-level overwrites.
- This pattern is ideal for the “dedicated private channel per ticket” model.
