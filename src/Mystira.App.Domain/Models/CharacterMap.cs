namespace Mystira.App.Domain.Models;

public class CharacterMap
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty; // Media path like "media/images/elarion.jpg"
    public string? Audio { get; set; } // Optional audio file like "media/audio/elarion_voice.mp3"
    public CharacterMetadata Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CharacterMetadata
{
    public List<string> Roles { get; set; } = new(); // e.g., ["mentor", "trickster"]
    public List<string> Archetypes { get; set; } = new(); // e.g., ["guardian", "the listener"]
    public string Species { get; set; } = string.Empty; // e.g., "elf", "goblin"
    public int Age { get; set; } = 0;
    public List<string> Traits { get; set; } = new(); // e.g., ["wise", "calm", "mysterious"]
    public string Backstory { get; set; } = string.Empty;
}

/// <summary>
/// YAML structure for character map export/import
/// </summary>
public class CharacterMapYaml
{
    public List<CharacterMapYamlEntry> Characters { get; set; } = new();
}

public class CharacterMapYamlEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string? Audio { get; set; }
    public CharacterMetadata Metadata { get; set; } = new();
}