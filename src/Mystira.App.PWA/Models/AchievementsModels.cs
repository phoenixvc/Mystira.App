namespace Mystira.App.PWA.Models;

public class AchievementsViewModel
{
    public string ProfileId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public string AgeGroupId { get; set; } = string.Empty;
    public List<AxisAchievementsSectionViewModel> Axes { get; set; } = new();
}

public class AxisAchievementsSectionViewModel
{
    public string AxisId { get; set; } = string.Empty;
    public string AxisName { get; set; } = string.Empty;
    public float CurrentScore { get; set; }

    public List<AxisAchievementCopy> AxisCopy { get; set; } = new();

    public List<BadgeTierViewModel> Tiers { get; set; } = new();
}

public class AxisAchievementCopy
{
    public string Direction { get; set; } = string.Empty; // "positive" or "negative"
    public string Description { get; set; } = string.Empty;
}

public class BadgeTierViewModel
{
    public string BadgeId { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int TierOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float RequiredScore { get; set; }

    public string ImageId { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public bool IsEarned { get; set; }
    public DateTime? EarnedAt { get; set; }

    public float CurrentScore { get; set; }

    public float RemainingScore => Math.Max(0, RequiredScore - CurrentScore);

    public float ProgressPercent
    {
        get
        {
            if (RequiredScore <= 0)
            {
                return 0;
            }

            return Math.Clamp((CurrentScore / RequiredScore) * 100f, 0f, 100f);
        }
    }
}
