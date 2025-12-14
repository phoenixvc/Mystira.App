using YamlDotNet.Serialization;

namespace Mystira.App.Domain.Models;

// YAML-specific models for scenario loading
public class YamlScenario
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = new();

    [YamlMember(Alias = "difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [YamlMember(Alias = "session_length")]
    public string SessionLength { get; set; } = string.Empty;

    [YamlMember(Alias = "archetypes")]
    public List<string> Archetypes { get; set; } = new();

    [YamlMember(Alias = "age_group")]
    public string AgeGroup { get; set; } = string.Empty;

    [YamlMember(Alias = "minimum_age")]
    public int MinimumAge { get; set; } = 1;

    [YamlMember(Alias = "core_axes")]
    public List<string> CoreAxes { get; set; } = new();

    [YamlMember(Alias = "compass_axes")]
    public List<string> LegacyCompassAxes { get; set; } = new();

    [YamlMember(Alias = "summary")]
    public string Summary { get; set; } = string.Empty;

    [YamlMember(Alias = "created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = string.Empty;

    [YamlMember(Alias = "scenes")]
    public List<YamlScene> Scenes { get; set; } = new();

    // Convert to domain model
    public Scenario ToDomainModel()
    {
        var axes = CoreAxes?.Any() == true ? CoreAxes : LegacyCompassAxes ?? new List<string>();

        return new Scenario
        {
            Id = Id,
            Title = Title,
            Description = Description,
            Tags = Tags,
            Difficulty = Enum.Parse<DifficultyLevel>(Difficulty),
            SessionLength = Enum.Parse<SessionLength>(SessionLength),
            Archetypes = Archetypes.Where(a => Archetype.Parse(a) != null).Select(Archetype.Parse).ToList()!,
            AgeGroup = AgeGroup,
            MinimumAge = MinimumAge,
            CoreAxes = axes.Where(a => CoreAxis.Parse(a) != null).Select(CoreAxis.Parse).ToList()!,
            CreatedAt = DateTime.TryParse(CreatedAt, out var createdAt) ? createdAt : DateTime.UtcNow,
            Scenes = Scenes.Select(s => s.ToDomainModel()).ToList()
        };
    }
}

public class YamlScene
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    [YamlMember(Alias = "active_character")]
    public string? ActiveCharacter { get; set; }

    [YamlMember(Alias = "next_scene")]
    public string? NextScene { get; set; }

    [YamlMember(Alias = "next_scene_id")]
    public string? LegacyNextSceneId { get; set; }

    [YamlMember(Alias = "difficulty")]
    public int? Difficulty { get; set; }

    [YamlMember(Alias = "media")]
    public YamlMediaReferences? Media { get; set; }

    [YamlMember(Alias = "branches")]
    public List<YamlBranch> Branches { get; set; } = new();

    [YamlMember(Alias = "echo_reveals")]
    public List<YamlEchoRevealReference> EchoReveals { get; set; } = new();

    [YamlMember(Alias = "echo_reveal_references")]
    public List<YamlEchoRevealReference> LegacyEchoRevealReferences { get; set; } = new();

    public Scene ToDomainModel()
    {
        var nextSceneId = !string.IsNullOrWhiteSpace(NextScene) ? NextScene : LegacyNextSceneId;
        var echoReveals = (EchoReveals?.Any() == true ? EchoReveals : LegacyEchoRevealReferences) ?? new List<YamlEchoRevealReference>();

        return new Scene
        {
            Id = Id,
            Title = Title,
            Type = Enum.Parse<SceneType>(Type, true),
            Description = Description,
            ActiveCharacter = ActiveCharacter,
            NextSceneId = string.IsNullOrWhiteSpace(nextSceneId) ? null : nextSceneId,
            Difficulty = Difficulty,
            Media = Media?.ToDomainModel(),
            Branches = Branches.Select(b => b.ToDomainModel()).ToList(),
            EchoReveals = echoReveals.Select<YamlEchoRevealReference, EchoReveal>(e => e.ToDomainModel()).ToList()
        };
    }
}

public class YamlMediaReferences
{
    [YamlMember(Alias = "image")]
    public string? Image { get; set; }

    [YamlMember(Alias = "audio")]
    public string? Audio { get; set; }

    [YamlMember(Alias = "video")]
    public string? Video { get; set; }

    public MediaReferences ToDomainModel()
    {
        return new MediaReferences
        {
            Image = Image,
            Audio = Audio,
            Video = Video
        };
    }
}

public class YamlBranch
{
    [YamlMember(Alias = "choice")]
    public string Choice { get; set; } = string.Empty;

    [YamlMember(Alias = "next_scene")]
    public string? NextScene { get; set; }

    [YamlMember(Alias = "next_scene_id")]
    public string? LegacyNextSceneId { get; set; }

    [YamlMember(Alias = "echo_log")]
    public YamlEchoLog? EchoLog { get; set; }

    [YamlMember(Alias = "compass_change")]
    public YamlCompassChange? CompassChange { get; set; }

    public Branch ToDomainModel()
    {
        var nextSceneId = !string.IsNullOrWhiteSpace(NextScene) ? NextScene : LegacyNextSceneId;

        return new Branch
        {
            Choice = Choice,
            NextSceneId = string.IsNullOrWhiteSpace(nextSceneId) ? string.Empty : nextSceneId,
            EchoLog = EchoLog?.ToDomainModel(),
            CompassChange = CompassChange?.ToDomainModel()
        };
    }
}

public class YamlEchoLog
{
    [YamlMember(Alias = "echo_type")]
    public string EchoType { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    [YamlMember(Alias = "strength")]
    public float Strength { get; set; }

    public EchoLog ToDomainModel()
    {
        return new EchoLog
        {
            EchoType = Mystira.App.Domain.Models.EchoType.Parse(EchoType)!,
            Description = Description,
            Strength = Strength,
            Timestamp = DateTime.UtcNow
        };
    }
}

public class YamlCompassChange
{
    [YamlMember(Alias = "axis")]
    public string Axis { get; set; } = string.Empty;

    [YamlMember(Alias = "delta")]
    public float Delta { get; set; }

    [YamlMember(Alias = "developmental_link")]
    public string? DevelopmentalLink { get; set; }

    public CompassChange ToDomainModel()
    {
        return new CompassChange
        {
            Axis = Axis,
            Delta = Delta,
            DevelopmentalLink = DevelopmentalLink
        };
    }
}

public class YamlEchoRevealReference
{
    [YamlMember(Alias = "echo_type")]
    public string EchoType { get; set; } = string.Empty;

    [YamlMember(Alias = "min_strength")]
    public float MinStrength { get; set; }

    [YamlMember(Alias = "trigger_scene_id")]
    public string TriggerSceneId { get; set; } = string.Empty;

    [YamlMember(Alias = "reveal_mechanic")]
    public string RevealMechanic { get; set; } = "none";

    [YamlMember(Alias = "max_age_scenes")]
    public int MaxAgeScenes { get; set; } = 10;

    [YamlMember(Alias = "required")]
    public bool Required { get; set; } = false;

    public EchoReveal ToDomainModel()
    {
        return new EchoReveal
        {
            EchoType = Mystira.App.Domain.Models.EchoType.Parse(EchoType)!,
            MinStrength = MinStrength,
            TriggerSceneId = TriggerSceneId,
            RevealMechanic = RevealMechanic,
            MaxAgeScenes = MaxAgeScenes,
            Required = Required
        };
    }
}
