namespace Mystira.App.Domain.Models;

public class Scenario
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DifficultyLevel Difficulty { get; set; }
    public SessionLength SessionLength { get; set; }
    public List<string> Archetypes { get; set; } = new();
    public string AgeGroup { get; set; } = string.Empty;
    public int MinimumAge { get; set; }
    public List<string> CoreAxes { get; set; } = new();
    public List<ScenarioCharacter> Characters { get; set; } = new();
    public List<Scene> Scenes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
    public List<string> Archetype { get; set; } = new();
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
    public string EchoType { get; set; } = string.Empty;
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
    public string EchoType { get; set; } = string.Empty;
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
    Narrative,
    Choice,
    Roll,
    Special
}

public static class MasterLists
{
    public static readonly List<string> Archetypes = new()
    {
        "heroic", "guardian", "trickster", "wise/mental", "inner", "moral", "elemental"
    };

    public static readonly List<string> CoreAxes = new()
    {
        "honesty", "bravery", "generosity", "loyalty", "humility", "empathy", "resilience",
        "responsibility", "justice", "trust", "kindness", "discipline", "patience", "curiosity",
        "forgiveness", "self_awareness", "integrity", "assertiveness", "fairness", "self_control",
        "cooperation", "adaptability", "courage", "compassion", "gratitude", "perseverance",
        "open_mindedness", "decisiveness", "emotional_intelligence", "altruism", "ambition",
        "creativity", "independence", "respect", "self_acceptance", "focus", "moral_consistency",
        "self_reflection", "social_bonding", "conflict_resolution", "ethical_reasoning",
        "identity_alignment", "relational_security", "growth_mindset"
    };

    public static readonly List<string> EchoTypes = new()
    {
        "honesty", "deception", "loyalty", "betrayal", "justice", "injustice", "fairness", "bias",
        "forgiveness", "revenge", "sacrifice", "selfishness", "obedience", "rebellion", "doubt",
        "confidence", "shame", "pride", "regret", "hope", "despair", "grief", "denial", "acceptance",
        "awakening", "resignation", "growth", "stagnation", "kindness", "neglect", "compassion",
        "coldness", "generosity", "envy", "gratitude", "resentment", "love", "jealousy", "trust",
        "manipulation", "support", "abandonment", "bravery", "fear", "aggression", "cowardice",
        "protection", "avoidance", "confrontation", "flight", "freeze", "rescue", "denial_of_help",
        "risk_taking", "panic", "resilience", "authenticity", "masking", "conformity", "individualism",
        "dependence", "independence", "attention_seeking", "withdrawal", "role_adoption", "role_rejection",
        "listening", "interrupting", "mockery", "encouragement", "humiliation", "respect", "disrespect",
        "sharing", "withholding", "blaming", "apologizing", "curiosity", "closed-mindedness",
        "truth_seeking", "value_conflict", "reflection", "projection", "mirroring", "internalization",
        "breakthrough", "denial_of_truth", "clarity", "pattern_repetition", "pattern_break",
        "echo_amplification", "influence_spread", "echo_collision", "legacy_creation", "reputation_change",
        "morality_shift", "alignment_pull", "world_change", "first_blood", "oath_made", "oath_broken",
        "promise", "secret_revealed", "lie_exposed", "lesson_learned", "lesson_ignored", "role_locked",
        "destiny_revealed"
    };
}
