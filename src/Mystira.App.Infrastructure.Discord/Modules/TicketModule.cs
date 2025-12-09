using System.Collections.Concurrent;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mystira.App.Infrastructure.Discord.Configuration;

namespace Mystira.App.Infrastructure.Discord.Modules;

/// <summary>
/// Slash command module for ticket management.
/// Provides /ticket and /ticket-close commands for support ticketing.
/// </summary>
public class TicketModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordOptions _options;
    private readonly ILogger<TicketModule> _logger;

    // FIX: Use per-user semaphores to prevent race condition when creating tickets
    private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> _userLocks = new();

    public TicketModule(
        IOptions<DiscordOptions> options,
        ILogger<TicketModule> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    [SlashCommand("ticket", "Create a private support ticket channel")]
    public async Task CreateTicketAsync(
        [Summary("subject", "Short summary of your issue")] string? subject = null)
    {
        var guild = Context.Guild;
        var user = (SocketGuildUser)Context.User;

        if (_options.SupportRoleId == 0)
        {
            await RespondAsync("Support role not configured.", ephemeral: true);
            return;
        }

        // FIX: Acquire per-user lock to prevent race condition
        var userLock = _userLocks.GetOrAdd(user.Id, _ => new SemaphoreSlim(1, 1));

        // Try to acquire lock with timeout
        if (!await userLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            await RespondAsync("Please wait, your previous ticket request is still being processed.", ephemeral: true);
            return;
        }

        try
        {
            // Check for existing open ticket (now protected by lock)
            var existing = guild.TextChannels
                .FirstOrDefault(c => c.Topic != null && c.Topic.Contains($"user:{user.Id}") && !c.Topic.Contains("status:closed"));

            if (existing != null)
            {
                await RespondAsync($"You already have an open ticket: {existing.Mention}", ephemeral: true);
                return;
            }

            // Channel naming: ticket-<username>-<4digits>
            // FIX: Validate channel name length (Discord limit: 100 chars)
            var suffix = Random.Shared.Next(1000, 9999);
            var safeName = MakeSafeChannelSlug(user.Username);
            var channelName = $"ticket-{safeName}-{suffix}";
            if (channelName.Length > 100)
            {
                channelName = $"ticket-{safeName[..Math.Min(safeName.Length, 85)]}-{suffix}";
            }

            // Permission overwrites
            var overwrites = new Overwrite[]
            {
                new(guild.EveryoneRole.Id, PermissionTarget.Role,
                    new OverwritePermissions(viewChannel: PermValue.Deny)),

                new(user.Id, PermissionTarget.User,
                    new OverwritePermissions(
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Allow,
                        readMessageHistory: PermValue.Allow,
                        attachFiles: PermValue.Allow,
                        embedLinks: PermValue.Allow)),

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
                props.Topic = $"ticket | user:{user.Id} | created:{DateTimeOffset.UtcNow:O}";
            });

            // Acknowledge privately
            await RespondAsync($"Your ticket is ready: {newChannel.Mention}", ephemeral: true);

            // Intro message in ticket channel
            var embed = new EmbedBuilder()
                .WithTitle("Support Ticket Opened")
                .WithDescription(
                    $"{user.Mention}, welcome!\n\n" +
                    $"**Subject:** {(string.IsNullOrWhiteSpace(subject) ? "No subject provided" : subject)}\n\n" +
                    "Please describe your issue with as much detail as you can.\n" +
                    "A support member will reply here."
                )
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await newChannel.SendMessageAsync(embed: embed);

            // Log to intake channel if configured
            if (_options.SupportIntakeChannelId != 0)
            {
                var intake = Context.Client.GetChannel(_options.SupportIntakeChannelId) as IMessageChannel;
                if (intake != null)
                {
                    await intake.SendMessageAsync($"New ticket created by {user.Mention}: {newChannel.Mention}");
                }
            }

            _logger.LogInformation("Ticket created: {ChannelName} by {User}", channelName, user.Username);
        }
        finally
        {
            userLock.Release();
        }
    }

    [SlashCommand("ticket-close", "Close this ticket (support only)")]
    public async Task CloseTicketAsync()
    {
        if (Context.Channel is not SocketTextChannel channel)
        {
            await RespondAsync("This command can only be used in a ticket channel.", ephemeral: true);
            return;
        }

        // Guard: only close channels that look like tickets
        if (channel.Topic == null || !channel.Topic.Contains("ticket | user:"))
        {
            await RespondAsync("This doesn't look like a ticket channel.", ephemeral: true);
            return;
        }

        // Archive approach if configured
        if (_options.SupportArchiveCategoryId != 0)
        {
            // Extract user ID from topic
            var marker = "user:";
            var start = channel.Topic.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            ulong userId = 0;

            if (start >= 0)
            {
                var after = channel.Topic[(start + marker.Length)..];
                var idStr = new string(after.TakeWhile(char.IsDigit).ToArray());
                ulong.TryParse(idStr, out userId);
            }

            var overwrites = channel.PermissionOverwrites.ToList();

            // Remove user access
            if (userId != 0)
            {
                overwrites.RemoveAll(o => o.TargetType == PermissionTarget.User && o.TargetId == userId);
                overwrites.Add(new Overwrite(userId, PermissionTarget.User,
                    new OverwritePermissions(viewChannel: PermValue.Deny)));
            }

            await channel.ModifyAsync(props =>
            {
                props.CategoryId = _options.SupportArchiveCategoryId;
                props.PermissionOverwrites = overwrites;
                props.Topic = channel.Topic + " | status:closed";
            });

            await RespondAsync("Ticket archived and closed.", ephemeral: true);
            _logger.LogInformation("Ticket archived: {ChannelName}", channel.Name);
            return;
        }

        // Otherwise delete
        await RespondAsync("Ticket will be closed.", ephemeral: true);
        _logger.LogInformation("Ticket deleted: {ChannelName}", channel.Name);
        await channel.DeleteAsync();
    }

    private static string MakeSafeChannelSlug(string input)
    {
        var lower = input.ToLowerInvariant();
        var cleaned = new string(lower
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        cleaned = cleaned.Trim('-');

        while (cleaned.Contains("--"))
            cleaned = cleaned.Replace("--", "-");

        return string.IsNullOrWhiteSpace(cleaned) ? "user" : cleaned;
    }
}
