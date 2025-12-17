namespace Mystira.App.PWA.Services;

/// <summary>
/// No-op Discord client used when Discord integration is disabled.
/// </summary>
public class NullDiscordApiClient : IDiscordApiClient
{
    public Task<DiscordStatusResponse?> GetStatusAsync()
    {
        return Task.FromResult<DiscordStatusResponse?>(new DiscordStatusResponse
        {
            Enabled = false,
            Connected = false,
            BotUsername = null,
            BotId = null,
            Message = "Discord integration is disabled"
        });
    }

    public Task<bool> SendMessageAsync(ulong channelId, string message) => Task.FromResult(false);

    public Task<bool> SendEmbedAsync(DiscordEmbedRequest request) => Task.FromResult(false);
}
