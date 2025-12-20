using Microsoft.JSInterop;
using Mystira.App.Domain.Models;

namespace Mystira.App.PWA.Services.Music;

public class AudioBus : IAudioBus
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IMediaApiClient _mediaApiClient;
    private readonly ISettingsService _settingsService;

    public AudioBus(IJSRuntime jsRuntime, IMediaApiClient mediaApiClient, ISettingsService settingsService)
    {
        _jsRuntime = jsRuntime;
        _mediaApiClient = mediaApiClient;
        _settingsService = settingsService;
    }

    public async Task PlayMusicAsync(string trackId, MusicTransitionHint transition, float volume = 1.0f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        var trackUrl = _mediaApiClient.GetMediaResourceEndpointUrl(trackId);
        await _jsRuntime.InvokeVoidAsync("AudioEngine.playMusic", trackUrl, transition.ToString(), volume);
    }

    public async Task StopMusicAsync(MusicTransitionHint transition)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await _jsRuntime.InvokeVoidAsync("AudioEngine.stopMusic", transition.ToString());
    }

    public async Task PlaySoundEffectAsync(string trackId, bool loop = false, float volume = 1.0f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        var trackUrl = _mediaApiClient.GetMediaResourceEndpointUrl(trackId);
        await _jsRuntime.InvokeVoidAsync("AudioEngine.playSfx", trackUrl, loop, volume);
    }

    public async Task StopSoundEffectAsync(string trackId)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        var trackUrl = _mediaApiClient.GetMediaResourceEndpointUrl(trackId);
        await _jsRuntime.InvokeVoidAsync("AudioEngine.stopSfx", trackUrl);
    }

    public async Task SetMusicVolumeAsync(float volume, float durationSeconds = 0.5f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await _jsRuntime.InvokeVoidAsync("AudioEngine.setMusicVolume", volume, durationSeconds);
    }

    public async Task DuckMusicAsync(bool duck, float duckVolume = 0.2f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await _jsRuntime.InvokeVoidAsync("AudioEngine.duckMusic", duck, duckVolume);
    }

    public async Task PauseAllAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await _jsRuntime.InvokeVoidAsync("AudioEngine.pauseAll");
    }

    public async Task ResumeAllAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await _jsRuntime.InvokeVoidAsync("AudioEngine.resumeAll");
    }

    public async Task PauseMusicAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await _jsRuntime.InvokeVoidAsync("AudioEngine.pauseMusic");
    }

    public async Task ResumeMusicAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await _jsRuntime.InvokeVoidAsync("AudioEngine.resumeMusic");
    }

    public async Task<bool> IsMusicPausedAsync()
    {
        return await _jsRuntime.InvokeAsync<bool>("AudioEngine.isMusicPaused");
    }
}
