namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents a compass axis in the game system for tracking character development.
/// </summary>
public class CompassAxis
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this record is soft-deleted (for referential integrity).
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
