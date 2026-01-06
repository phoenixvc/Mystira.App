using Microsoft.Extensions.Logging;
using Mystira.App.PWA.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Mystira.App.PWA.Services.Music;

public class AudioCacheService : IAudioCacheService
{
    private readonly ILogger<AudioCacheService> _logger;
    private readonly IMediaApiClient _mediaApiClient;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _concurrencySemaphore = new(4, 4);

    public AudioCacheService(ILogger<AudioCacheService> logger, IMediaApiClient mediaApiClient, HttpClient httpClient)
    {
        _logger = logger;
        _mediaApiClient = mediaApiClient;
        _httpClient = httpClient;
    }

    public async Task CacheScenarioAudioAsync(Scenario scenario)
    {
        if (scenario?.Scenes == null || !scenario.Scenes.Any())
        {
            return;
        }

        _logger.LogInformation("Starting audio pre-cache for scenario: {ScenarioTitle}", scenario.Title);

        var audioIds = new HashSet<string>();

        // 1. Collect from scenes
        foreach (var scene in scenario.Scenes)
        {
            if (!string.IsNullOrEmpty(scene.Media?.Audio))
            {
                audioIds.Add(scene.Media.Audio);
            }

            if (scene.SoundEffects != null)
            {
                foreach (var sfx in scene.SoundEffects)
                {
                    if (!string.IsNullOrEmpty(sfx.Track))
                    {
                        audioIds.Add(sfx.Track);
                    }
                }
            }
        }

        // 2. Collect from music palette
        if (scenario.MusicPalette?.TracksByProfile != null)
        {
            foreach (var trackList in scenario.MusicPalette.TracksByProfile.Values)
            {
                if (trackList != null)
                {
                    foreach (var track in trackList)
                    {
                        if (!string.IsNullOrEmpty(track))
                        {
                            audioIds.Add(track);
                        }
                    }
                }
            }
        }

        _logger.LogInformation("Found {Count} unique audio tracks to cache", audioIds.Count);

        var tasks = audioIds.Select(async id =>
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                await CacheAudioAsync(id);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);

        _logger.LogInformation("Finished audio pre-cache for scenario: {ScenarioTitle}", scenario.Title);
    }

    public async Task CacheAudioAsync(string mediaId)
    {
        if (string.IsNullOrEmpty(mediaId)) return;

        try
        {
            var url = _mediaApiClient.GetMediaResourceEndpointUrl(mediaId);
            _logger.LogDebug("Caching audio: {MediaId} from {Url}", mediaId, url);

            // Use 'no-cors' mode for audio pre-fetching.
            // This is safer for binary assets as it avoids CORS preflight and works even if the server
            // doesn't have open CORS headers for fetch (matching how <audio> elements work).
            // NOTE: In 'no-cors' mode, the response is 'opaque', meaning we can't read status or body,
            // but the browser still populates its cache.
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.SetBrowserRequestMode(BrowserRequestMode.NoCors);
            request.SetBrowserRequestCache(BrowserRequestCache.Default);

            // Use the default HttpCompletionOption.ResponseContentRead (by omitting the parameter)
            // to ensure the HttpClient consumes the full stream, which triggers the browser
            // to download the entire file into its cache.
            using var response = await _httpClient.SendAsync(request);

            _logger.LogDebug("Triggered cache for: {MediaId} (NoCors mode)", mediaId);
        }
        catch (Exception ex)
        {
            // We log this as information/debug rather than warning if it's a fetch error in pre-caching,
            // as it shouldn't block the game, but here it failed to even start.
            _logger.LogDebug(ex, "Note: Pre-cache request for {MediaId} had an issue. This is usually non-fatal.", mediaId);
        }
    }
}
