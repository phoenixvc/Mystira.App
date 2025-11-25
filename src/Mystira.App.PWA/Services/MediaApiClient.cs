using System.Text.Json;

namespace Mystira.App.PWA.Services;

/// <summary>
/// API client for media-related operations
/// </summary>
public class MediaApiClient : BaseApiClient, IMediaApiClient
{
    public MediaApiClient(HttpClient httpClient, ILogger<MediaApiClient> logger, ITokenProvider tokenProvider)
        : base(httpClient, logger, tokenProvider)
    {
    }

    public async Task<string?> GetMediaUrlFromId(string mediaId)
    {
        try
        {
            Logger.LogInformation("Fetching media id '{MediaId}' from API...", mediaId);

            var encodedMediaId = Uri.EscapeDataString(mediaId);
            var response = await HttpClient.GetAsync($"api/Media/{encodedMediaId}/info");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonString);

                if (document.RootElement.TryGetProperty("url", out var urlElement) &&
                    urlElement.ValueKind == JsonValueKind.String)
                {
                    var url = urlElement.GetString()!;
                    Logger.LogInformation("Successfully fetched media URL for '{MediaId}'", encodedMediaId);
                    return url;
                }

                Logger.LogWarning("URL property not found in response for '{MediaId}'", encodedMediaId);
                return null;
            }

            Logger.LogWarning("API request failed with status: {StatusCode}. Media '{MediaId}' not available.",
                response.StatusCode, encodedMediaId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching url for media id {MediaId} from API.", mediaId);
            return null;
        }
    }

    public string GetMediaResourceEndpointUrl(string mediaId)
    {
        return $"{GetApiBaseAddressPublic()}api/media/{mediaId}";
    }
}

