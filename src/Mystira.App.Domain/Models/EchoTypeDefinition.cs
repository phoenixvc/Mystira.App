namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents an echo type definition stored in the database.
/// Echo types are used in scenarios for game events and progression tracking.
/// </summary>
public class EchoTypeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Category of the echo type (e.g., "moral", "emotional", "behavioral", "social", "cognitive", "meta")
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this record is soft-deleted (for referential integrity).
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
