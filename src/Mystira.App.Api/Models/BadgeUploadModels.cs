namespace Mystira.App.Api.Models;

/// <summary>
/// Model for badge YAML file structure
/// </summary>
public class BadgeYamlFile
{
    public List<BadgeYamlItem> Badges { get; set; } = new();
}

/// <summary>
/// Individual badge item from YAML
/// </summary>
public class BadgeYamlItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Axis { get; set; } = string.Empty;
    public float Threshold { get; set; }
    public string ImageId { get; set; } = string.Empty;
}

/// <summary>
/// Result of badge upload operation
/// </summary>
public class BadgeUploadResult
{
    public bool Success { get; set; }
    public int UploadedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> SuccessfulUploads { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }
}