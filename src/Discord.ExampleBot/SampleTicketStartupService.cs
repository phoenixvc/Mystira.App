using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Discord.ExampleBot;

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

        if (_settings.SupportRoleId == 0)
        {
            _logger.LogWarning("SupportRoleId not set. Skipping startup sample ticket creation.");
            return;
        }

        if (_client.ConnectionState != ConnectionState.Connected)
        {
            _logger.LogWarning("Discord client not connected yet. Skipping.");
            return;
        }

        if (Interlocked.Exchange(ref _hasRun, 1) == 1)
            return;

        var guild = _client.GetGuild(_settings.GuildId);
        if (guild == null)
        {
            _logger.LogWarning("Could not resolve guild using GuildId {GuildId}.", _settings.GuildId);
            return;
        }

        var suffix = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmm");
        var channelName = $"ticket-startup-{suffix}";

        var overwrites = new Overwrite[]
        {
            new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role,
                new OverwritePermissions(viewChannel: PermValue.Deny)),

            new Overwrite(_settings.SupportRoleId, PermissionTarget.Role,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    manageChannel: PermValue.Allow,
                    manageMessages: PermValue.Allow))
        };

        var newChannel = await guild.CreateTextChannelAsync(channelName, props =>
        {
            if (_settings.SupportCategoryId != 0)
                props.CategoryId = _settings.SupportCategoryId;

            props.PermissionOverwrites = overwrites;
            props.Topic = $"startup sample ticket | created:{DateTimeOffset.UtcNow:O}";
        });

        var embed = new EmbedBuilder()
            .WithTitle("🧪 Sample Ticket (Startup Channel)")
            .WithDescription(
                "This channel was created on startup to test the ticket channel flow.\n\n" +
                "**User:** sample_startup\n" +
                "**Issue:** Channel creation + permissions check\n" +
                "**Priority:** Low"
            )
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await newChannel.SendMessageAsync(embed: embed);

        // Optional log to intake if you still want it
        if (_settings.SupportIntakeChannelId != 0)
        {
            var intake = _client.GetChannel(_settings.SupportIntakeChannelId) as IMessageChannel;
            if (intake != null)
            {
                await intake.SendMessageAsync($"Startup sample ticket channel created: {newChannel.Mention}");
            }
        }

        _logger.LogInformation("Created startup sample ticket channel {ChannelName}.", channelName);
    }
}
