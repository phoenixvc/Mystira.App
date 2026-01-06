using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MusicProfile
{
    None,
    Neutral,
    Cozy,
    Playful,
    Wonder,
    Mystery,
    Tense,
    Action,
    Sad,
    Victory
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MusicContinuity
{
    PreferContinue,
    AllowChange,
    ForceChange,
    ForceSilence
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MusicTransitionHint
{
    Auto,
    Keep,
    CrossfadeShort,
    CrossfadeNormal,
    CrossfadeLong,
    HardCut
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MusicPriority
{
    Background,
    Important
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MusicDucking
{
    None,
    Narration,
    Dialogue
}

public class SceneSoundEffect
{
    public string Track { get; set; } = string.Empty;
    public bool Loopable { get; set; }
    public double Energy { get; set; }
}

public class MusicPalette
{
    public MusicProfile DefaultProfile { get; set; } = MusicProfile.Neutral;
    
    // Using string keys to map nicely to JSON properties like "neutral", "cozy"
    // The keys should match the MusicProfile enum names (case-insensitive)
    public Dictionary<string, List<string>> TracksByProfile { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class SceneMusicSettings
{
    public MusicProfile Profile { get; set; }
    public double? Energy { get; set; }
    public MusicContinuity Continuity { get; set; } = MusicContinuity.PreferContinue;
    public MusicTransitionHint TransitionHint { get; set; } = MusicTransitionHint.Auto;
    public MusicPriority Priority { get; set; } = MusicPriority.Background;
    public MusicDucking Ducking { get; set; } = MusicDucking.None;
}
