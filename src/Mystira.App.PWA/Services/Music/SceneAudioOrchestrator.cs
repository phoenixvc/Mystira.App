using Mystira.App.Domain.Models;
using Mystira.App.PWA.Models;
using Scene = Mystira.App.PWA.Models.Scene;
using Scenario = Mystira.App.PWA.Models.Scenario;

namespace Mystira.App.PWA.Services.Music;

public class SceneAudioOrchestrator
{
    private readonly IMusicResolver _resolver;
    private readonly IAudioBus _audioBus;
    private readonly MusicContext _context = new();
    private readonly HashSet<string> _activeLoopingSfx = new();

    public SceneAudioOrchestrator(IMusicResolver resolver, IAudioBus audioBus)
    {
        _resolver = resolver;
        _audioBus = audioBus;
    }

    public async Task EnterSceneAsync(Scene scene, Scenario scenario)
    {
        // 1. Resolve Music
        var result = _resolver.ResolveMusic(scene, scenario, _context);

        if (result.IsSilence)
        {
            if (_context.CurrentTrackId != null)
            {
                await _audioBus.StopMusicAsync(result.Transition);
                _context.CurrentTrackId = null;
                _context.CurrentProfile = MusicProfile.None;
                _context.CurrentEnergy = 0;
            }
        }
        else if (result.TrackId != null)
        {
            // If it's a new track OR we are currently in silence (CurrentTrackId is null)
            // but the resolver found a track, we MUST play it.
            if (result.TrackId != _context.CurrentTrackId || _context.CurrentTrackId == null)
            {
                // Update energy from intent
                var intent = _resolver.GetEffectiveIntent(scene);
                var energy = intent.Energy ?? 0.4; // Default middle energy if not specified
                _context.CurrentEnergy = energy;

                await _audioBus.PlayMusicAsync(result.TrackId, result.Transition, (float)energy);
                _context.CurrentTrackId = result.TrackId;
                _context.CurrentProfile = result.Profile;

                // Maintain history
                _context.RecentTrackIds.Add(result.TrackId);
                if (_context.RecentTrackIds.Count > 5)
                    _context.RecentTrackIds.RemoveAt(0);
            }
            else
            {
                // Same track, update energy if needed
                var intent = _resolver.GetEffectiveIntent(scene);
                var energy = intent.Energy ?? _context.CurrentEnergy;

                if (Math.Abs(energy - _context.CurrentEnergy) > 0.05)
                {
                    await _audioBus.SetMusicVolumeAsync((float)energy);
                }

                _context.CurrentEnergy = energy;
            }
        }

        // 2. Sound Effects
        // Stop previous looping SFX that are not in the new scene
        // For simplicity, we assume all previous loops should stop unless explicitly sustained (not in schema yet).
        // A smarter engine would diff the lists.
        // Let's implement diffing.

        var newSfx = scene.SoundEffects ?? new List<SceneSoundEffect>();
        var newLoopingTracks = newSfx.Where(s => s.Loopable).Select(s => s.Track).ToHashSet();

        // Stop loops that are no longer present
        var toStop = _activeLoopingSfx.Where(t => !newLoopingTracks.Contains(t)).ToList();
        foreach (var track in toStop)
        {
            await _audioBus.StopSoundEffectAsync(track);
            _activeLoopingSfx.Remove(track);
        }

        // Play new loops / one-shots
        foreach (var sfx in newSfx)
        {
            if (sfx.Loopable)
            {
                // Only start if not already playing
                if (!_activeLoopingSfx.Contains(sfx.Track))
                {
                    await _audioBus.PlaySoundEffectAsync(sfx.Track, true, (float)sfx.Energy);
                    _activeLoopingSfx.Add(sfx.Track);
                }
            }
            else
            {
                // Always play one-shots on entry
                await _audioBus.PlaySoundEffectAsync(sfx.Track, false, (float)sfx.Energy);
            }
        }
    }

    /// <summary>
    /// Pauses or resumes audio when a blocking action (like video) starts or stops.
    /// </summary>
    public async Task OnSceneActionAsync(bool isActionActive)
    {
        if (isActionActive)
        {
            await _audioBus.PauseAllAsync();
        }
        else
        {
            await _audioBus.ResumeAllAsync();
        }
    }

    public async Task StopAllAsync()
    {
        await _audioBus.StopMusicAsync(MusicTransitionHint.CrossfadeNormal);
        _context.CurrentTrackId = null;
        _context.CurrentProfile = MusicProfile.None;
        _context.CurrentEnergy = 0;

        foreach (var track in _activeLoopingSfx.ToList())
        {
            await _audioBus.StopSoundEffectAsync(track);
            _activeLoopingSfx.Remove(track);
        }
    }
}
