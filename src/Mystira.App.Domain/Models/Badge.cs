namespace Mystira.App.Domain.Models;

public class Badge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AgeGroupId { get; set; } = string.Empty;
    public string CompassAxisId { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int TierOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float RequiredScore { get; set; }
    public string ImageId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
