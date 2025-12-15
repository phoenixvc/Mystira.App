namespace Mystira.App.PWA.Models;

public class FinalizeSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public List<ProfileBadgeAwardsResponse> Awards { get; set; } = new();
}

public class ProfileBadgeAwardsResponse
{
    public string ProfileId { get; set; } = string.Empty;
    public List<UserBadgeResponse> NewBadges { get; set; } = new();
}

public class UserBadgeResponse
{
    public string UserProfileId { get; set; } = string.Empty;
    public string BadgeId { get; set; } = string.Empty;
    public string BadgeName { get; set; } = string.Empty;
    public string BadgeMessage { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public float TriggerValue { get; set; }
    public float Threshold { get; set; }
    public DateTime EarnedAt { get; set; }
    public string? ImageId { get; set; }
}
