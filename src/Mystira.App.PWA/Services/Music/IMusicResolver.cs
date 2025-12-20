using Mystira.App.Domain.Models;

namespace Mystira.App.PWA.Services.Music;

public interface IMusicResolver
{
    MusicResolutionResult ResolveMusic(Scene nextScene, Scenario scenario, MusicContext currentContext);
    SceneMusicSettings GetEffectiveIntent(Scene scene);
}

public class MusicContext
{
    public string? CurrentTrackId { get; set; }
    public MusicProfile CurrentProfile { get; set; }
    public double CurrentEnergy { get; set; }
    public List<string> RecentTrackIds { get; set; } = new();
}

public class MusicResolutionResult
{
    public string? TrackId { get; set; }
    public MusicProfile Profile { get; set; }
    public MusicTransitionHint Transition { get; set; }
    public bool IsSilence { get; set; }
}
