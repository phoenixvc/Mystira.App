using Mystira.App.Domain.Models;
using Scene = Mystira.App.PWA.Models.Scene;
using Scenario = Mystira.App.PWA.Models.Scenario;
using SceneType = Mystira.App.PWA.Models.SceneType;

namespace Mystira.App.PWA.Services.Music;

public class MusicResolver : IMusicResolver
{
    private const double EnergyChangeThreshold = 0.35;
    private readonly Random _rng = new();

    public SceneMusicSettings GetEffectiveIntent(Scene scene)
    {
        if (scene.Music != null)
        {
            return scene.Music;
        }

        // Defaults by scene type as per requirements
        return scene.SceneType switch
        {
            SceneType.Narrative => new SceneMusicSettings
            {
                Continuity = MusicContinuity.PreferContinue,
                Ducking = MusicDucking.Narration,
                TransitionHint = MusicTransitionHint.Auto,
                Profile = MusicProfile.Neutral
            },
            SceneType.Choice => new SceneMusicSettings
            {
                Continuity = MusicContinuity.PreferContinue,
                Ducking = MusicDucking.Dialogue,
                TransitionHint = MusicTransitionHint.CrossfadeShort,
                Profile = MusicProfile.Neutral
            },
            SceneType.Roll => new SceneMusicSettings
            {
                Continuity = MusicContinuity.AllowChange,
                Energy = 0.7,
                TransitionHint = MusicTransitionHint.CrossfadeShort,
                Profile = MusicProfile.Tense
            },
            SceneType.Special => new SceneMusicSettings
            {
                Continuity = MusicContinuity.ForceChange,
                TransitionHint = MusicTransitionHint.CrossfadeLong,
                Profile = MusicProfile.Victory
            },
            _ => new SceneMusicSettings { Profile = MusicProfile.Neutral }
        };
    }

    public MusicResolutionResult ResolveMusic(Scene nextScene, Scenario scenario, MusicContext currentContext)
    {
        var intent = GetEffectiveIntent(nextScene);
        var palette = scenario.MusicPalette;

        // 1. Check for silence
        if (intent.Continuity == MusicContinuity.ForceSilence || intent.Profile == MusicProfile.None)
        {
             return new MusicResolutionResult
             {
                 IsSilence = true,
                 Transition = intent.TransitionHint,
                 Profile = MusicProfile.None
             };
        }

        // 2. Determine if we MUST or SHOULD change track
        bool shouldChange = false;

        if (intent.Continuity == MusicContinuity.ForceChange)
        {
            shouldChange = true;
        }
        else if (currentContext.CurrentTrackId == null)
        {
            // Nothing playing, so must start something
            shouldChange = true;
        }
        else if (intent.Continuity == MusicContinuity.PreferContinue)
        {
             // Keep playing if profile matches, otherwise change
             if (currentContext.CurrentProfile != intent.Profile)
             {
                 shouldChange = true;
             }
        }
        else if (intent.Continuity == MusicContinuity.AllowChange)
        {
             if (currentContext.CurrentProfile != intent.Profile)
             {
                 shouldChange = true;
             }
             // Change if energy delta is big enough
             else if (intent.Energy.HasValue && Math.Abs(intent.Energy.Value - currentContext.CurrentEnergy) > EnergyChangeThreshold)
             {
                 shouldChange = true;
             }
        }

        // 3. Resolution
        var transition = intent.TransitionHint;
        if (transition == MusicTransitionHint.Auto)
        {
            // Simple rule: if we are starting music from silence, HardCut or Short Crossfade.
            // If changing track, Normal Crossfade.
            transition = currentContext.CurrentTrackId == null
                ? MusicTransitionHint.CrossfadeShort
                : MusicTransitionHint.CrossfadeNormal;
        }

        if (!shouldChange)
        {
            return new MusicResolutionResult
            {
                TrackId = currentContext.CurrentTrackId,
                Profile = currentContext.CurrentProfile,
                Transition = MusicTransitionHint.Keep,
                IsSilence = false
            };
        }

        // We need to pick a new track
        // If profile is None but we are here, it likely means we fell through defaults.
        // But logic above handles None -> IsSilence = true.
        // So here intent.Profile is a real profile (e.g. Cozy, Neutral).

        var candidates = GetTracksForProfile(palette, intent.Profile);

        // If no tracks for requested profile, try default profile
        if (candidates.Count == 0 && palette != null && intent.Profile != palette.DefaultProfile)
        {
            candidates = GetTracksForProfile(palette, palette.DefaultProfile);
        }

        if (candidates.Count == 0)
        {
             // No tracks found at all -> Silence
             return new MusicResolutionResult { IsSilence = true, Profile = MusicProfile.None, Transition = transition };
        }

        var selectedTrack = PickTrack(candidates, currentContext.RecentTrackIds);

        return new MusicResolutionResult
        {
            TrackId = selectedTrack,
            Profile = intent.Profile,
            Transition = transition,
            IsSilence = false
        };
    }

    private List<string> GetTracksForProfile(MusicPalette? palette, MusicProfile profile)
    {
        if (palette == null) return new List<string>();

        if (palette.TracksByProfile.TryGetValue(profile.ToString(), out var tracks))
        {
            return tracks;
        }

        return new List<string>();
    }

    private string PickTrack(List<string> candidates, List<string> recentTracks)
    {
        if (candidates.Count == 1) return candidates[0];

        // Filter out recent if possible
        var available = candidates.Except(recentTracks).ToList();

        // If all candidates were recently played, just pick any candidate
        if (!available.Any())
        {
            available = candidates;
        }

        return available[_rng.Next(available.Count)];
    }
}
