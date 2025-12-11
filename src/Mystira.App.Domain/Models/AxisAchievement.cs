namespace Mystira.App.Domain.Models;

public class AxisAchievement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AgeGroupId { get; set; } = string.Empty;
    public string CompassAxisId { get; set; } = string.Empty;
    public string AxesDirection { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
