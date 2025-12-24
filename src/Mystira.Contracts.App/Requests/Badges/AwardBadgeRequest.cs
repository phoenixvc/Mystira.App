using System.ComponentModel.DataAnnotations;

namespace Mystira.Contracts.App.Requests.Badges;

public class AwardBadgeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string UserProfileId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string BadgeConfigurationId { get; set; } = string.Empty;

    // Axis of the badge being awarded (e.g., Courage, Compassion)
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Axis { get; set; } = string.Empty;

    [Required]
    [Range(0.1, 100.0)]
    public float TriggerValue { get; set; }

    public string? GameSessionId { get; set; }
    public string? ScenarioId { get; set; }
}

