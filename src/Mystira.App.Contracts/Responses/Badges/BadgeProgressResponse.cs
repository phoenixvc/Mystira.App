namespace Mystira.App.Contracts.Responses.Badges;

/// <summary>
/// Response for badge progress information for a profile
/// </summary>
public class BadgeProgressResponse
{
    public string AgeGroupId { get; set; } = string.Empty;
    public List<AxisProgressResponse> AxisProgresses { get; set; } = new();
}

/// <summary>
/// Progress for a specific compass axis
/// </summary>
public class AxisProgressResponse
{
    public string AxisId { get; set; } = string.Empty;
    public string AxisName { get; set; } = string.Empty;
    public float CurrentScore { get; set; }
    public List<BadgeTierProgressResponse> Tiers { get; set; } = new();
}

/// <summary>
/// Progress for a specific badge tier
/// </summary>
public class BadgeTierProgressResponse
{
    public string BadgeId { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int TierOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float RequiredScore { get; set; }
    public string ImageId { get; set; } = string.Empty;
    public bool IsEarned { get; set; }
    public DateTime? EarnedAt { get; set; }
    public float ProgressToThreshold { get; set; } // Current score towards this tier
    public float RemainingScore { get; set; } // Score needed to reach this tier
}
