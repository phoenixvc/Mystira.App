namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents a badge earned by a user profile
/// </summary>
public class UserBadge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The user profile that earned this badge
    /// </summary>
    public string UserProfileId { get; set; } = string.Empty;

    /// <summary>
    /// The badge configuration ID
    /// </summary>
    public string BadgeConfigurationId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the badge at the time it was earned
    /// </summary>
    public string BadgeName { get; set; } = string.Empty;

    /// <summary>
    /// The message of the badge at the time it was earned
    /// </summary>
    public string BadgeMessage { get; set; } = string.Empty;

    /// <summary>
    /// The axis this badge was earned for
    /// </summary>
    public string Axis { get; set; } = string.Empty;

    /// <summary>
    /// The value that triggered this badge
    /// </summary>
    public float TriggerValue { get; set; }

    /// <summary>
    /// The threshold that was met to earn this badge
    /// </summary>
    public float Threshold { get; set; }

    /// <summary>
    /// When this badge was earned
    /// </summary>
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The game session ID where this badge was earned (optional)
    /// </summary>
    public string? GameSessionId { get; set; }

    /// <summary>
    /// The scenario ID where this badge was earned (optional)
    /// </summary>
    public string? ScenarioId { get; set; }

    /// <summary>
    /// Image path for the badge
    /// </summary>
    public string ImageId { get; set; } = string.Empty;
}
