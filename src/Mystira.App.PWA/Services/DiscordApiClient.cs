using System.Net.Http.Json;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for Discord bot operations
/// </summary>
public class DiscordApiClient : BaseApiClient, IDiscordApiClient
{
    public DiscordApiClient(HttpClient httpClient, ILogger<DiscordApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<DiscordStatusResponse?> GetStatusAsync()
    {
        try
        {
            // Discord status is a public endpoint - no auth required
            return await HttpClient.GetFromJsonAsync<DiscordStatusResponse>("api/discord/status", JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            // Log as warning since Discord integration is optional
            Logger.LogWarning(ex, "Discord status API unavailable (this is expected if Discord integration is not configured).");
            return new DiscordStatusResponse
            {
                Enabled = false,
                Connected = false,
                Message = "Discord integration not available"
            };
        }
        catch (Exception ex)
        {
            // Log unexpected errors
            Logger.LogWarning(ex, "Failed to get Discord status from API.");
            return new DiscordStatusResponse
            {
                Enabled = false,
                Connected = false,
                Message = "Discord integration not available"
            };
        }
    }

    public async Task<bool> SendMessageAsync(ulong channelId, string message)
    {
        try
        {
            var request = new DiscordMessageRequest
            {
                ChannelId = channelId,
                Message = message
            };

            await SetAuthorizationHeaderAsync();
            var response = await HttpClient.PostAsJsonAsync("api/discord/send", request, JsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send Discord message.");
            return false;
        }
    }

    public async Task<bool> SendEmbedAsync(DiscordEmbedRequest request)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await HttpClient.PostAsJsonAsync("api/discord/send-embed", request, JsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send Discord embed.");
            return false;
        }
    }
}
