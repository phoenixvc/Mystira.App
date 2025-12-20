using Mystira.App.Domain.Models;

namespace Mystira.App.PWA.Services.Music;

public interface IAudioBus
{
    Task PlayMusicAsync(string trackId, MusicTransitionHint transition, float volume = 1.0f);
    Task StopMusicAsync(MusicTransitionHint transition);
    Task PlaySoundEffectAsync(string trackId, bool loop = false, float volume = 1.0f);
    Task StopSoundEffectAsync(string trackId);
    Task SetMusicVolumeAsync(float volume, float durationSeconds = 0.5f);
    Task DuckMusicAsync(bool duck, float duckVolume = 0.2f);
    Task PauseAllAsync();
    Task ResumeAllAsync();
}
