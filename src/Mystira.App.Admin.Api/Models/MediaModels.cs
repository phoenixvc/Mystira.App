using System.Text.Json.Serialization;

namespace Mystira.App.Admin.Api.Models;

/// <summary>
/// Represents a media asset
/// </summary>
public class MediaAsset
{
    public string Id { get; set; } = string.Empty;
    public string MediaId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty; // audio, video, image
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Hash { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? ThumbnailUrl { get; set; }
    public MediaMetadata? Metadata { get; set; }
}

/// <summary>
/// Media metadata for additional information
/// </summary>
public class MediaMetadata
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }
    public int? Bitrate { get; set; }
    public string? Format { get; set; }
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}

/// <summary>
/// Request model for querying media assets
/// </summary>
public class MediaQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? MediaType { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Response model for media queries
/// </summary>
public class MediaQueryResponse
{
    public List<MediaAsset> Media { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Request model for updating media
/// </summary>
public class MediaUpdateRequest
{
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public string? MediaType { get; set; }
}

/// <summary>
/// Result model for bulk upload operations
/// </summary>
public class BulkUploadResult
{
    public bool Success { get; set; }
    public int UploadedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> SuccessfulUploads { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// Result model for media validation
/// </summary>
public class MediaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> MissingMediaIds { get; set; } = new();
    public List<string> ValidMediaIds { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// Media usage statistics
/// </summary>
public class MediaUsageStats
{
    public int TotalMediaFiles { get; set; }
    public int AudioFiles { get; set; }
    public int VideoFiles { get; set; }
    public int ImageFiles { get; set; }
    public long TotalStorageBytes { get; set; }
    public string TotalStorageFormatted { get; set; } = string.Empty;
    public Dictionary<string, int> TagUsage { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

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
/// Single character map file containing all characters
/// </summary>
public class CharacterMapFile
{
    public string Id { get; set; } = "character-map";
    public List<Character> Characters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Individual character entry
/// </summary>
public class Character
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty; // Media ID reference
    public CharacterMetadata Metadata { get; set; } = new();
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

/// <summary>
/// Bundle upload request models
/// </summary>
public class BundleUploadRequest
{
    public bool ValidateReferences { get; set; } = true;
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// Bundle validation result
/// </summary>
public class BundleValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int ScenarioCount { get; set; }
    public int MediaCount { get; set; }
}

/// <summary>
/// Bundle upload result
/// </summary>
public class BundleUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ScenariosImported { get; set; }
    public int MediaImported { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}