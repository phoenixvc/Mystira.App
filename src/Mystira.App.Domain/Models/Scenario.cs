namespace Mystira.App.Domain.Models;

public class Scenario
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DifficultyLevel Difficulty { get; set; }
    public SessionLength SessionLength { get; set; }
    public List<Archetype> Archetypes { get; set; } = new();
    public string AgeGroup { get; set; } = string.Empty;
    public int MinimumAge { get; set; }
    public List<CoreAxis> CoreAxes { get; set; } = new();
    public List<ScenarioCharacter> Characters { get; set; } = new();
    public List<Scene> Scenes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
        {
            errors.Add("Scenario title cannot be empty.");
        }

        if (Scenes == null || !Scenes.Any())
        {
            errors.Add("Scenario must have at least one scene.");
        }
        else
        {
            var sceneIds = new HashSet<string>(Scenes.Select(s => s.Id));
            foreach (var scene in Scenes)
            {
                if (!string.IsNullOrEmpty(scene.NextSceneId) && !sceneIds.Contains(scene.NextSceneId))
                {
                    errors.Add($"Scene '{scene.Title}' has an invalid NextSceneId: {scene.NextSceneId}");
                }

                foreach (var branch in scene.Branches)
                {
                    if (!sceneIds.Contains(branch.NextSceneId))
                    {
                        errors.Add($"Scene '{scene.Title}' has a branch with an invalid NextSceneId: {branch.NextSceneId}");
                    }
                }
            }
        }

        return !errors.Any();
    }
}

public class ScenarioCharacter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public ScenarioCharacterMetadata Metadata { get; set; } = new();
}

public class ScenarioCharacterMetadata
{
    public List<string> Role { get; set; } = new();
    public List<Archetype> Archetype { get; set; } = new();
    public string Species { get; set; } = string.Empty;
    public int Age { get; set; }
    public List<string> Traits { get; set; } = new();
    public string Backstory { get; set; } = string.Empty;
}

public class Scene
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SceneType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? NextSceneId { get; set; }
    public MediaReferences? Media { get; set; }
    public List<Branch> Branches { get; set; } = new();
    public List<EchoReveal> EchoReveals { get; set; } = new();
    public int? Difficulty { get; set; }
}

public class Branch
{
    public string Choice { get; set; } = string.Empty;
    public string NextSceneId { get; set; } = string.Empty;
    public EchoLog? EchoLog { get; set; }
    public CompassChange? CompassChange { get; set; }
}

public class MediaReferences
{
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public string? Video { get; set; }
}

public class EchoLog
{
    public EchoType EchoType { get; set; } = EchoType.Parse("honesty")!;
    public string Description { get; set; } = string.Empty;
    public double Strength { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CompassChange
{
    public string Axis { get; set; } = string.Empty;
    public double Delta { get; set; }
    public string? DevelopmentalLink { get; set; }
}

public class EchoReveal
{
    public EchoType EchoType { get; set; } = EchoType.Parse("honesty")!;
    public double MinStrength { get; set; }
    public string TriggerSceneId { get; set; } = string.Empty;
    public int? MaxAgeScenes { get; set; }
    public string? RevealMechanic { get; set; }
    public bool? Required { get; set; }
}

public class CompassTracking
{
    public string Axis { get; set; } = string.Empty;
    public double CurrentValue { get; set; } = 0.0;
    public double StartingValue { get; set; } = 0.0;
    public List<CompassChange> History { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}

public enum SessionLength
{
    Short,
    Medium,
    Long
}

public enum SceneType
{
    Narrative = 0,
    Choice = 1,
    Roll = 2,
    Special = 3
}

