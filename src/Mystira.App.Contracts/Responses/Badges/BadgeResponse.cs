namespace Mystira.App.Contracts.Responses.Badges;

/// <summary>
/// Response for a badge definition (public badge configuration)
/// </summary>
public class BadgeResponse
{
    public string Id { get; set; } = string.Empty;
    public string AgeGroupId { get; set; } = string.Empty;
    public string CompassAxisId { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int TierOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float RequiredScore { get; set; }
    public string ImageId { get; set; } = string.Empty;
}
