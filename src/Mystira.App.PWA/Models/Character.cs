using System.Text.Json.Serialization;

namespace Mystira.App.PWA.Models;

/// <summary>
/// Character model for PWA
/// </summary>
public class Character
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string? Audio { get; set; }
    public CharacterMetadata Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Convenience properties
    public string? Role => Metadata.Roles?.FirstOrDefault();
    public string? Archetype => Metadata.Archetypes?.FirstOrDefault();
    public string DisplayInfo
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Role)) parts.Add(Role);
            if (!string.IsNullOrEmpty(Archetype)) parts.Add(Archetype);
            if (!string.IsNullOrEmpty(Metadata.Species)) parts.Add(Metadata.Species);
            return parts.Count > 0 ? string.Join(" • ", parts) : "Character";
        }
    }
}

/// <summary>
/// Character metadata
/// </summary>
public class CharacterMetadata
{
    public List<string> Roles { get; set; } = new();
    public List<string> Archetypes { get; set; } = new();
    public string Species { get; set; } = string.Empty;
    public int Age { get; set; } = 0;
    public List<string> Traits { get; set; } = new();
    public string Backstory { get; set; } = string.Empty;
}
