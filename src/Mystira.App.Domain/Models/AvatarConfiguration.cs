namespace Mystira.App.Domain.Models;

/// <summary>
/// Represents avatar configuration for an age group
/// </summary>
public class AvatarConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AgeGroup { get; set; } = string.Empty;
    public List<string> AvatarMediaIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the avatar mapping file stored in storage
/// </summary>
public class AvatarConfigurationFile
{
    public string Id { get; set; } = "avatar-configuration";
    public Dictionary<string, List<string>> AgeGroupAvatars { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
}
