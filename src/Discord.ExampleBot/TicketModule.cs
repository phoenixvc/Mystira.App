using Discord;
using Discord.ExampleBot;
using Discord.Interactions;
using Discord.WebSocket;

public class TicketModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSettings _settings;

    public TicketModule(DiscordSettings settings)
    {
        _settings = settings;
    }

    [SlashCommand("ticket", "Create a private support ticket channel")]
    public async Task CreateTicketAsync(
        [Summary("subject", "Short summary of your issue")] string? subject = null)
    {
        var guild = Context.Guild;
        var user = (SocketGuildUser)Context.User;

        if (_settings.SupportRoleId == 0)
        {
            await RespondAsync("Support role not configured.", ephemeral: true);
            return;
        }

        // Optional: allow only one open ticket per user by name pattern
        // (You can remove this block if you want multiple tickets per user)
        var existing = guild.TextChannels
            .FirstOrDefault(c => c.Topic != null && c.Topic.Contains($"user:{user.Id}"));

        if (existing != null)
        {
            await RespondAsync($"You already have an open ticket: {existing.Mention}", ephemeral: true);
            return;
        }

        // Channel naming: ticket-<username>-<4digits>
        var suffix = Random.Shared.Next(1000, 9999);
        var safeName = MakeSafeChannelSlug(user.Username);
        var channelName = $"ticket-{safeName}-{suffix}";

        // Permission overwrites:
        var overwrites = new Overwrite[]
        {
            new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role,
                new OverwritePermissions(viewChannel: PermValue.Deny)),

            new Overwrite(user.Id, PermissionTarget.User,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    attachFiles: PermValue.Allow,
                    embedLinks: PermValue.Allow)),

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

            // Use Topic to tag ownership for lookup
            props.Topic = $"ticket | user:{user.Id} | created:{DateTimeOffset.UtcNow:O}";
        });

        // Acknowledge privately to the user
        await RespondAsync($"✅ Your ticket is ready: {newChannel.Mention}", ephemeral: true);

        // Intro message in the ticket channel
        var embed = new EmbedBuilder()
            .WithTitle("🎫 Support Ticket Opened")
            .WithDescription(
                $"{user.Mention}, welcome!\n\n" +
                $"**Subject:** {(string.IsNullOrWhiteSpace(subject) ? "No subject provided" : subject)}\n\n" +
                "Please describe your issue with as much detail as you can.\n" +
                "A support member will reply here."
            )
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await newChannel.SendMessageAsync(embed: embed);

        // Optional: log to intake channel if configured
        if (_settings.SupportIntakeChannelId != 0)
        {
            var intake = Context.Client.GetChannel(_settings.SupportIntakeChannelId) as IMessageChannel;
            if (intake != null)
            {
                await intake.SendMessageAsync(
                    $"New ticket created by {user.Mention}: {newChannel.Mention}");
            }
        }
    }

    private static string MakeSafeChannelSlug(string input)
    {
        // Very small sanitiser for Discord channel naming
        var lower = input.ToLowerInvariant();
        var cleaned = new string(lower
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        cleaned = cleaned.Trim('-');

        // Collapse doubles
        while (cleaned.Contains("--"))
            cleaned = cleaned.Replace("--", "-");

        return string.IsNullOrWhiteSpace(cleaned) ? "user" : cleaned;
    }

    [SlashCommand("ticket-close", "Close this ticket (support only)")]
    public async Task CloseTicketAsync()
    {
        if (Context.Channel is not SocketTextChannel channel)
        {
            await RespondAsync("This command can only be used in a ticket channel.", ephemeral: true);
            return;
        }

        // Simple guard: only close channels that look like tickets
        if (channel.Topic == null || !channel.Topic.Contains("ticket | user:"))
        {
            await RespondAsync("This doesn't look like a ticket channel.", ephemeral: true);
            return;
        }

        // Optional archive approach:
        // Move to an archive category and lock the user out
        if (_settings.SupportArchiveCategoryId != 0)
        {

            // Extract user id from topic
            var marker = "user:";
            var start = channel.Topic.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            ulong userId = 0;

            if (start >= 0)
            {
                var after = channel.Topic.Substring(start + marker.Length);
                var idStr = new string(after.TakeWhile(char.IsDigit).ToArray());
                ulong.TryParse(idStr, out userId);
            }

            var overwrites = channel.PermissionOverwrites.ToList();

            // Remove user access if we found them
            if (userId != 0)
            {
                overwrites.RemoveAll(o => o.TargetType == PermissionTarget.User && o.TargetId == userId);
                overwrites.Add(new Overwrite(userId, PermissionTarget.User,
                    new OverwritePermissions(viewChannel: PermValue.Deny)));
            }

            await channel.ModifyAsync(props =>
            {
                props.CategoryId = _settings.SupportArchiveCategoryId;
                props.PermissionOverwrites = overwrites;
                props.Topic = channel.Topic + " | status:closed";
            });

            await RespondAsync("✅ Ticket archived and closed.", ephemeral: true);
            return;
        }

        // Or simplest: delete
        await RespondAsync("✅ Ticket will be closed.", ephemeral: true);
        await channel.DeleteAsync();
    }

}
