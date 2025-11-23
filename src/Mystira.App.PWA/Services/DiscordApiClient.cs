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
            await SetAuthorizationHeaderAsync();
            return await HttpClient.GetFromJsonAsync<DiscordStatusResponse>("api/discord/status", JsonOptions);
        }
        catch
        {
            // Return disabled status if API call fails
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
        catch
        {
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
        catch
        {
            return false;
        }
    }
}
