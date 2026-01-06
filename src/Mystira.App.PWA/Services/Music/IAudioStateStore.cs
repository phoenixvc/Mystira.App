using Mystira.App.Domain.Models;

namespace Mystira.App.PWA.Services.Music;

public interface IAudioStateStore
{
    MusicContext Context { get; }
    HashSet<string> ActiveLoopingSfx { get; }
    void Reset();
}

public class AudioStateStore : IAudioStateStore
{
    public MusicContext Context { get; } = new();
    public HashSet<string> ActiveLoopingSfx { get; } = new();

    public void Reset()
    {
        Context.CurrentTrackId = null;
        Context.CurrentProfile = MusicProfile.None;
        Context.CurrentEnergy = 0;
        Context.RecentTrackIds.Clear();
        ActiveLoopingSfx.Clear();
    }
}
