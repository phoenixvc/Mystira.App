namespace Mystira.App.Domain.Models;

public class GameSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ScenarioId { get; set; } = string.Empty;
    public string DmName { get; set; } = string.Empty;
    public List<string> PlayerNames { get; set; } = new(); // Names only, no accounts for children
    public SessionStatus Status { get; set; } = SessionStatus.NotStarted;
    public string CurrentSceneId { get; set; } = string.Empty;
    public List<SessionChoice> ChoiceHistory { get; set; } = new();
    public List<EchoLog> EchoHistory { get; set; } = new();
    public Dictionary<string, CompassTracking> CompassValues { get; set; } = new();
    public List<SessionAchievement> Achievements { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public bool IsPaused { get; set; }
    public DateTime? PausedAt { get; set; }
    public int SceneCount { get; set; }
    // Store as string for database compatibility, but provide AgeGroup access
    private string _targetAgeGroup = AgeGroup.School.Name;
    public string TargetAgeGroupName 
    { 
        get => _targetAgeGroup; 
        set => _targetAgeGroup = value; 
    }
    
    // Convenience property to get AgeGroup object
    public AgeGroup TargetAgeGroup 
    { 
        get => AgeGroup.GetByName(_targetAgeGroup) ?? AgeGroup.School;
        set => _targetAgeGroup = value?.Name ?? AgeGroup.School.Name;
    }
    public string? SelectedCharacterId { get; set; } // Character selected from character map
}

public class SessionChoice
{
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string ChoiceText { get; set; } = string.Empty;
    public string NextScene { get; set; } = string.Empty;
    public DateTime ChosenAt { get; set; } = DateTime.UtcNow;
    public EchoLog? EchoGenerated { get; set; }
    public CompassChange? CompassChange { get; set; }
}

public class SessionAchievement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public AchievementType Type { get; set; }
    public string CompassAxis { get; set; } = string.Empty; // For compass-based achievements
    public float ThresholdValue { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    public bool IsVisible { get; set; } = true;
}

public enum SessionStatus
{
    NotStarted,
    InProgress,
    Paused,
    Completed,
    Abandoned
}

public enum AchievementType
{
    CompassThreshold,   // When compass axis reaches threshold
    FirstChoice,        // First choice made in session
    SessionComplete,    // Completed a full scenario
    EchoRevealed,      // When an echo is revealed
    ConsistentChoice,  // Multiple choices in same direction
    MoralGrowth        // Positive compass movement
}

// Badge configuration is now handled by the BadgeConfiguration model and API