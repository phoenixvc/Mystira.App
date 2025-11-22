using System.Text.Json.Serialization;

namespace Mystira.App.Contracts.Models;

/// <summary>
/// Individual media metadata entry
/// </summary>
public class MediaMetadataEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // image, audio, video

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("age_rating")]
    public int AgeRating { get; set; }

    [JsonPropertyName("subjectReferenceId")]
    public string SubjectReferenceId { get; set; } = string.Empty;

    [JsonPropertyName("classificationTags")]
    public List<ClassificationTag> ClassificationTags { get; set; } = new();

    [JsonPropertyName("modifiers")]
    public List<Modifier> Modifiers { get; set; } = new();

    [JsonPropertyName("loopable")]
    public bool Loopable { get; set; } = false;
}

/// <summary>
/// Represents a classifier for metadata tags within media metadata entries.
/// </summary>
public class ClassificationTag
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Represents a modifier for media item classifications
/// </summary>
public class Modifier
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

