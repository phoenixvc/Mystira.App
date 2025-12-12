namespace Mystira.App.Domain.Models;

public class GameSession
{
    public const double CompassMinValue = -2.0;
    public const double CompassMaxValue = 2.0;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ScenarioId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public List<string> PlayerNames { get; set; } = new(); // Names only, no accounts for children
    public SessionStatus Status { get; set; } = SessionStatus.NotStarted;
    public string CurrentSceneId { get; set; } = string.Empty;
    public List<SessionChoice> ChoiceHistory { get; set; } = new();
    public List<EchoLog> EchoHistory { get; set; } = new();
    public Dictionary<string, CompassTracking> CompassValues { get; set; } = new();
    public List<PlayerCompassProgress> PlayerCompassProgressTotals { get; set; } = new();
    public List<SessionAchievement> Achievements { get; set; } = new();

    // Character assignments for this session (who plays which story character)
    public List<SessionCharacterAssignment> CharacterAssignments { get; set; } = new();

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public bool IsPaused { get; set; }
    public DateTime? PausedAt { get; set; }
    public int SceneCount { get; set; }

    // Store as string for database compatibility, but provide AgeGroup access
    private string _targetAgeGroup = "school";

    public string TargetAgeGroupName
    {
        get => _targetAgeGroup;
        set => _targetAgeGroup = value;
    }

    // Convenience property to get AgeGroup object
    public AgeGroup TargetAgeGroup
    {
        get => AgeGroup.Parse(_targetAgeGroup) ?? new AgeGroup(6, 9);
        set => _targetAgeGroup = value?.Value ?? "6-9";
    }

    public string? SelectedCharacterId { get; set; } // Character selected from character map

    public TimeSpan GetTotalElapsedTime()
    {
        var totalTime = ElapsedTime;
        if (IsPaused && PausedAt.HasValue)
        {
            totalTime += DateTime.UtcNow - PausedAt.Value;
        }
        else if (Status == SessionStatus.InProgress)
        {
            totalTime += DateTime.UtcNow - StartTime;
        }
        return totalTime;
    }

    public void RecalculateCompassProgressFromHistory()
    {
        var totalsByPlayerAndAxis = new Dictionary<(string PlayerId, string Axis), double>();
        var totalsByAxis = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var historyByAxis = new Dictionary<string, List<CompassChange>>(StringComparer.OrdinalIgnoreCase);
        var lastUpdatedByAxis = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        foreach (var choice in ChoiceHistory)
        {
            if (!TryGetCompassDelta(choice, out var axis, out var delta))
            {
                continue;
            }

            var playerId = string.IsNullOrWhiteSpace(choice.PlayerId) ? ProfileId : choice.PlayerId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                continue;
            }

            delta = Clamp(delta, CompassMinValue, CompassMaxValue);

            var key = (PlayerId: playerId, Axis: axis);
            totalsByPlayerAndAxis.TryGetValue(key, out var playerTotal);
            totalsByPlayerAndAxis[key] = Clamp(playerTotal + delta, CompassMinValue, CompassMaxValue);

            totalsByAxis.TryGetValue(axis, out var axisTotal);
            totalsByAxis[axis] = Clamp(axisTotal + delta, CompassMinValue, CompassMaxValue);

            if (!historyByAxis.TryGetValue(axis, out var history))
            {
                history = new List<CompassChange>();
                historyByAxis[axis] = history;
            }

            history.Add(new CompassChange
            {
                Axis = axis,
                Delta = delta
            });

            lastUpdatedByAxis[axis] = choice.ChosenAt;
        }

        PlayerCompassProgressTotals = totalsByPlayerAndAxis
            .Select(kvp => new PlayerCompassProgress
            {
                PlayerId = kvp.Key.PlayerId,
                Axis = kvp.Key.Axis,
                Total = kvp.Value
            })
            .OrderBy(x => x.PlayerId)
            .ThenBy(x => x.Axis)
            .ToList();

        CompassValues ??= new Dictionary<string, CompassTracking>();
        foreach (var (axis, total) in totalsByAxis)
        {
            if (!CompassValues.TryGetValue(axis, out var tracking))
            {
                tracking = new CompassTracking
                {
                    Axis = axis,
                    CurrentValue = 0.0,
                    StartingValue = 0.0,
                    History = new List<CompassChange>(),
                    LastUpdated = DateTime.UtcNow
                };
                CompassValues[axis] = tracking;
            }

            tracking.Axis = axis;
            tracking.CurrentValue = total;
            tracking.History = historyByAxis.TryGetValue(axis, out var history) ? history : new List<CompassChange>();
            tracking.LastUpdated = lastUpdatedByAxis.TryGetValue(axis, out var last) ? last : DateTime.UtcNow;
        }
    }

    private static bool TryGetCompassDelta(SessionChoice choice, out string axis, out double delta)
    {
        axis = string.Empty;
        delta = 0.0;

        if (!string.IsNullOrWhiteSpace(choice.CompassAxis) && choice.CompassDelta.HasValue)
        {
            axis = choice.CompassAxis;
            delta = ApplyDirection(choice.CompassDelta.Value, choice.CompassDirection);
            return true;
        }

        if (choice.CompassChange != null && !string.IsNullOrWhiteSpace(choice.CompassChange.Axis))
        {
            axis = choice.CompassChange.Axis;
            delta = choice.CompassChange.Delta;
            return true;
        }

        return false;
    }

    private static double ApplyDirection(double delta, string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            return delta;
        }

        var normalized = direction.Trim().ToLowerInvariant();
        if (normalized is "negative" or "neg" or "-" or "down")
        {
            return -Math.Abs(delta);
        }

        if (normalized is "positive" or "pos" or "+" or "up")
        {
            return Math.Abs(delta);
        }

        return delta;
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }
}

public class SessionChoice
{
    public string SceneId { get; set; } = string.Empty;
    public string SceneTitle { get; set; } = string.Empty;
    public string ChoiceText { get; set; } = string.Empty;
    public string NextScene { get; set; } = string.Empty;

    public string PlayerId { get; set; } = string.Empty;

    public string? CompassAxis { get; set; }
    public string? CompassDirection { get; set; }
    public double? CompassDelta { get; set; }

    public DateTime ChosenAt { get; set; } = DateTime.UtcNow;
    public EchoLog? EchoGenerated { get; set; }

    // Backward compatibility; prefer CompassAxis/CompassDelta/CompassDirection for new writes.
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

/// <summary>
/// Represents an assignment of a player (profile or guest) to a story character within a game session
/// </summary>
public class SessionCharacterAssignment
{
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Audio { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public SessionPlayerAssignment? PlayerAssignment { get; set; }
    public bool IsUnused { get; set; }
}

/// <summary>
/// Player metadata for a character assignment in the session
/// </summary>
public class SessionPlayerAssignment
{
    // "Player" (profile) or "Guest"
    public string Type { get; set; } = string.Empty;
    public string? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfileImage { get; set; }
    public string? SelectedAvatarMediaId { get; set; }

    // Guest fields
    public string? GuestName { get; set; }
    public string? GuestAgeRange { get; set; }
    public string? GuestAvatar { get; set; }
    public bool SaveAsProfile { get; set; }
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
