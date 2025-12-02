namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents a fantasy theme definition stored in the database.
/// Fantasy themes are used to categorize scenarios and user preferences.
/// </summary>
public class FantasyThemeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
