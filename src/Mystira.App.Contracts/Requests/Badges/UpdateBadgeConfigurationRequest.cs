namespace Mystira.App.Contracts.Requests.Badges;

public class UpdateBadgeConfigurationRequest
{
    public string? Name { get; set; }
    public string? Message { get; set; }
    public string? Axis { get; set; }
    public float? Threshold { get; set; }
    public string? ImageId { get; set; }
}

