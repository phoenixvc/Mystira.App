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
    public string MinimumAge { get; set; } = string.Empty; // Now uses string values from AgeGroup class
    public string Summary { get; set; } = string.Empty;
    public List<Scene> Scenes { get; set; } = new();
    public List<string> CompassAxes { get; set; } = new(); // Max 4 from master list
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Scene
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SceneType Type { get; set; }
    public string? NextSceneId { get; set; }
    public string Description { get; set; } = string.Empty;
    public MediaReferences? Media { get; set; }
    public List<Branch> Branches { get; set; } = new();
    public List<EchoRevealReference> EchoRevealReferences { get; set; } = new();
    
    public List<SessionAchievement> SessionAchievements { get; set; } = new();
    public int Difficulty { get; set; } = 0;
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
    public string EchoType { get; set; } = string.Empty; // From master echo types list
    public string Description { get; set; } = string.Empty;
    public double Strength { get; set; } // Between 0.1 and 1.0
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CompassChange
{
    public string Axis { get; set; } = string.Empty; // From master compass axes list
    public double Delta { get; set; } // Between -1.0 and 1.0
}

public class EchoRevealReference
{
    public string EchoType { get; set; } = string.Empty;
    public float MinStrength { get; set; }
    public string TriggerSceneId { get; set; } = string.Empty;
    public string RevealMechanic { get; set; } = "none"; // mirror, dream, spirit, none
    public int MaxAgeScenes { get; set; } = 10;
    public bool Required { get; set; } = false;
}

public class CompassTracking
{
    public string Axis { get; set; } = string.Empty; // From master compass axes list
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
    Short,   // ~30 minutes
    Medium,  // ~60 minutes
    Long     // ~90+ minutes
}

public enum SceneType
{
    Narrative,  // Story telling, no player choice
    Choice,     // Player decision point (only type that can have echo_log)
    Roll,       // Dice roll required
    Special     // Special mechanics or events
}

// Master lists from PRD specification
public static class MasterLists
{
    public static readonly List<string> Archetypes = new()
    {
        "heroic", "guardian", "trickster", "wise/mental", "inner", "moral", "elemental"
    };

    public static readonly List<string> CompassAxes = new()
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