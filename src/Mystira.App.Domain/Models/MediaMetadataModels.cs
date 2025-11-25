namespace Mystira.App.Domain.Models;

/// <summary>
/// Individual media metadata entry
/// </summary>
public class MediaMetadataEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // image, audio, video
    public string Description { get; set; } = string.Empty;
    public int AgeRating { get; set; }
    public string SubjectReferenceId { get; set; } = string.Empty;
    public List<ClassificationTag> ClassificationTags { get; set; } = new();
    public List<Modifier> Modifiers { get; set; } = new();
    public bool Loopable { get; set; } = false;
}

/// <summary>
/// Represents a classifier for metadata tags within media metadata entries.
/// </summary>
public class ClassificationTag
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Represents a modifier for media item classifications
/// </summary>
public class Modifier
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Single media metadata file containing all media metadata entries
/// </summary>
public class MediaMetadataFile
{
    public string Id { get; set; } = "media-metadata";
    public List<MediaMetadataEntry> Entries { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Individual character media metadata entry
/// </summary>
public class CharacterMediaMetadataEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // image, audio, video
    public string Description { get; set; } = string.Empty;
    public string AgeRating { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool Loopable { get; set; } = false;
}

/// <summary>
/// Single character media metadata file containing all character media metadata entries
/// </summary>
public class CharacterMediaMetadataFile
{
    public string Id { get; set; } = "character-media-metadata";
    public List<CharacterMediaMetadataEntry> Entries { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Individual character entry for API responses
/// </summary>
public class Character
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty; // Media ID reference
    public CharacterMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Character entry used in CharacterMapFile
/// Note: This is different from CharacterMap - this is for file-based character storage
/// </summary>
public class CharacterMapFileCharacter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty; // Media ID reference
    public CharacterMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Single character map file containing all characters
/// </summary>
public class CharacterMapFile
{
    public string Id { get; set; } = "character-map";
    public List<CharacterMapFileCharacter> Characters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
}

