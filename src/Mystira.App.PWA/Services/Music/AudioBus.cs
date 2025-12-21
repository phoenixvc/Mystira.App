using Microsoft.JSInterop;
using Mystira.App.Domain.Models;

namespace Mystira.App.PWA.Services.Music;

public class AudioBus : IAudioBus, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IMediaApiClient _mediaApiClient;
    private readonly ISettingsService _settingsService;
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AudioBus(IJSRuntime jsRuntime, IMediaApiClient mediaApiClient, ISettingsService settingsService)
    {
        _jsRuntime = jsRuntime;
        _mediaApiClient = mediaApiClient;
        _settingsService = settingsService;
    }

    private async Task EnsureModuleLoadedAsync()
    {
        if (_module != null) return;

        await _semaphore.WaitAsync();
        try
        {
            if (_module != null) return;
            // Use a stable version string to break cache once but avoid multiple module instances
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/audioPlayer.js?v=1.0.1");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PlayMusicAsync(string trackId, MusicTransitionHint transition, float volume = 1.0f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        var trackUrl = _mediaApiClient.GetMediaResourceEndpointUrl(trackId);
        await _module!.InvokeVoidAsync("playMusic", trackUrl, transition.ToString(), volume);
    }

    public async Task StopMusicAsync(MusicTransitionHint transition)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        await _module!.InvokeVoidAsync("stopMusic", transition.ToString());
    }

    public async Task PlaySoundEffectAsync(string trackId, bool loop = false, float volume = 1.0f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        var trackUrl = _mediaApiClient.GetMediaResourceEndpointUrl(trackId);
        await _module!.InvokeVoidAsync("playSfx", trackUrl, loop, volume);
    }

    public async Task StopSoundEffectAsync(string trackId)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        var trackUrl = _mediaApiClient.GetMediaResourceEndpointUrl(trackId);
        await _module!.InvokeVoidAsync("stopSfx", trackUrl);
    }

    public async Task SetMusicVolumeAsync(float volume, float durationSeconds = 0.5f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        await _module!.InvokeVoidAsync("setMusicVolume", volume, durationSeconds);
    }

    public async Task DuckMusicAsync(bool duck, float duckVolume = 0.2f)
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        await _module!.InvokeVoidAsync("duckMusic", duck, duckVolume);
    }

    public async Task PauseAllAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        await _module!.InvokeVoidAsync("pauseAll");
    }

    public async Task ResumeAllAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        await _module!.InvokeVoidAsync("resumeAll");
    }

    public async Task PauseMusicAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        await _module!.InvokeVoidAsync("pauseMusic");
    }

    public async Task ResumeMusicAsync()
    {
        if (!await _settingsService.GetAudioEnabledAsync()) return;

        await EnsureModuleLoadedAsync();
        await _module!.InvokeVoidAsync("resumeMusic");
    }

    public async Task<bool> IsMusicPausedAsync()
    {
        await EnsureModuleLoadedAsync();
        return await _module!.InvokeAsync<bool>("isMusicPaused");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
