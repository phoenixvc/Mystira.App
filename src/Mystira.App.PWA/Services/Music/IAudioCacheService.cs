using Mystira.App.PWA.Models;

namespace Mystira.App.PWA.Services.Music;

public interface IAudioCacheService
{
    /// <summary>
    /// Pre-caches all audio tracks associated with a scenario.
    /// </summary>
    Task CacheScenarioAudioAsync(Scenario scenario);

    /// <summary>
    /// Pre-caches a single audio track.
    /// </summary>
    Task CacheAudioAsync(string mediaId);
}
