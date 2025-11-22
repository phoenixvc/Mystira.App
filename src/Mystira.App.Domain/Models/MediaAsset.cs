namespace Mystira.App.Domain.Models;

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

