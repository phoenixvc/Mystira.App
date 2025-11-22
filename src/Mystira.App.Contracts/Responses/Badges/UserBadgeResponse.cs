namespace Mystira.App.Contracts.Responses.Badges;

public class UserBadgeResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserProfileId { get; set; } = string.Empty;
    public string BadgeConfigurationId { get; set; } = string.Empty;
    public string BadgeName { get; set; } = string.Empty;
    public string BadgeMessage { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public float TriggerValue { get; set; }
    public float Threshold { get; set; }
    public DateTime EarnedAt { get; set; }
    public string? GameSessionId { get; set; }
    public string? ScenarioId { get; set; }
    public string ImageId { get; set; } = string.Empty;
}

