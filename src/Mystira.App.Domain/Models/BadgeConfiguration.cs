namespace Mystira.App.Domain.Models;

public class BadgeConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty; // Compass axis from MasterLists.CompassAxes
    public float Threshold { get; set; } = 0.0f;
    public string ImageId { get; set; } = string.Empty; // Media path like "media/images/badge_honesty_1.jpg"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// YAML structure for badge configuration export/import
/// </summary>
public class BadgeConfigurationYaml
{
    public List<BadgeConfigurationYamlEntry> Badges { get; set; } = new();
}

public class BadgeConfigurationYamlEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public float Threshold { get; set; } = 0.0f;
    public string ImageId { get; set; } = string.Empty; // Using underscore for YAML compatibility
}
