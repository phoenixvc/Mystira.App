using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Infrastructure.Discord.Configuration;

namespace Mystira.App.Infrastructure.Discord.Services;

/// <summary>
/// Service that creates a sample ticket channel on startup for testing.
/// Only runs if PostSampleTicketOnStartup is enabled in configuration.
/// </summary>
public sealed class SampleTicketStartupService
{
    private readonly DiscordBotService _botService;
    private readonly DiscordOptions _options;
    private readonly ILogger<SampleTicketStartupService> _logger;
    private int _hasRun;

    public SampleTicketStartupService(
        DiscordBotService botService,
        IOptions<DiscordOptions> options,
        ILogger<SampleTicketStartupService> logger)
    {
        _botService = botService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Posts a sample ticket channel if enabled in configuration.
    /// Safe to call multiple times - will only execute once.
    /// </summary>
    public async Task PostSampleTicketIfEnabledAsync()
    {
        if (!_options.PostSampleTicketOnStartup)
        {
            _logger.LogDebug("PostSampleTicketOnStartup is false. Skipping.");
            return;
        }

        if (_options.SupportRoleId == 0)
        {
            _logger.LogWarning("SupportRoleId not set. Skipping startup sample ticket creation.");
            return;
        }

        if (!_botService.IsConnected)
        {
            _logger.LogWarning("Discord bot not connected yet. Skipping.");
            return;
        }

        // Only run once
        if (Interlocked.Exchange(ref _hasRun, 1) == 1)
            return;

        var client = _botService.Client;
        var guild = client.GetGuild(_options.GuildId);

        if (guild == null)
        {
            _logger.LogWarning("Could not resolve guild using GuildId {GuildId}.", _options.GuildId);
            return;
        }

        var suffix = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmm");
        var channelName = $"ticket-startup-{suffix}";

        var overwrites = new Overwrite[]
        {
            new(guild.EveryoneRole.Id, PermissionTarget.Role,
                new OverwritePermissions(viewChannel: PermValue.Deny)),

            new(_options.SupportRoleId, PermissionTarget.Role,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    manageChannel: PermValue.Allow,
                    manageMessages: PermValue.Allow))
        };

        var newChannel = await guild.CreateTextChannelAsync(channelName, props =>
        {
            if (_options.SupportCategoryId != 0)
                props.CategoryId = _options.SupportCategoryId;

            props.PermissionOverwrites = overwrites;
            props.Topic = $"startup sample ticket | created:{DateTimeOffset.UtcNow:O}";
        });

        var embed = new EmbedBuilder()
            .WithTitle("Sample Ticket (Startup Channel)")
            .WithDescription(
                "This channel was created on startup to test the ticket channel flow.\n\n" +
                "**User:** sample_startup\n" +
                "**Issue:** Channel creation + permissions check\n" +
                "**Priority:** Low"
            )
            .WithColor(Color.Orange)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await newChannel.SendMessageAsync(embed: embed);

        // Log to intake channel if configured
        if (_options.SupportIntakeChannelId != 0)
        {
            var intake = client.GetChannel(_options.SupportIntakeChannelId) as IMessageChannel;
            if (intake != null)
            {
                await intake.SendMessageAsync($"Startup sample ticket channel created: {newChannel.Mention}");
            }
        }

        _logger.LogInformation("Created startup sample ticket channel {ChannelName}.", channelName);
    }
}
