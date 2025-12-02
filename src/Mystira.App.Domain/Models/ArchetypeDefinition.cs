namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents an archetype definition that can be assigned to characters in scenarios.
/// </summary>
public class ArchetypeDefinition
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
