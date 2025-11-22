namespace Mystira.App.Contracts.Models;

/// <summary>
/// Single media metadata file containing all media metadata entries
/// </summary>
public class MediaMetadataFile
{
    public string Id { get; set; } = "media-metadata";
    public List<MediaMetadataEntry> Entries { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

