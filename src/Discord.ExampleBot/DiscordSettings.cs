namespace Discord.ExampleBot;

public sealed class DiscordSettings
{
    public string? Token { get; set; }

    public ulong GuildId { get; set; }
    public ulong SupportRoleId { get; set; }
    public ulong SupportCategoryId { get; set; }

    // Where the startup sample message is posted:
    public ulong SupportIntakeChannelId { get; set; }

    // Toggle:
    public bool PostSampleTicketOnStartup { get; set; } = true;

    public bool RegisterGloballyIfNoGuildId { get; set; } = false;
    public ulong SupportArchiveCategoryId { get; set; }
}
