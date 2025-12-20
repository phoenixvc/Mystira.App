using Microsoft.JSInterop;
using Mystira.App.Domain.Models;

namespace Mystira.App.PWA.Services.Music;

public class AudioBus : IAudioBus
{
    private readonly IJSRuntime _jsRuntime;

    public AudioBus(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task PlayMusicAsync(string trackId, MusicTransitionHint transition, float volume = 1.0f)
    {
        await _jsRuntime.InvokeVoidAsync("AudioEngine.playMusic", trackId, transition.ToString(), volume);
    }

    public async Task StopMusicAsync(MusicTransitionHint transition)
    {
        await _jsRuntime.InvokeVoidAsync("AudioEngine.stopMusic", transition.ToString());
    }

    public async Task PlaySoundEffectAsync(string trackId, bool loop = false, float volume = 1.0f)
    {
        await _jsRuntime.InvokeVoidAsync("AudioEngine.playSfx", trackId, loop, volume);
    }

    public async Task StopSoundEffectAsync(string trackId)
    {
        await _jsRuntime.InvokeVoidAsync("AudioEngine.stopSfx", trackId);
    }

    public async Task SetMusicVolumeAsync(float volume, float durationSeconds = 0.5f)
    {
        await _jsRuntime.InvokeVoidAsync("AudioEngine.setMusicVolume", volume, durationSeconds);
    }

    public async Task PauseAllAsync()
    {
        await _jsRuntime.InvokeVoidAsync("AudioEngine.pauseAll");
    }

    public async Task ResumeAllAsync()
    {
        await _jsRuntime.InvokeVoidAsync("AudioEngine.resumeAll");
    }
}
