namespace Mystira.App.Contracts.Responses.Media;

/// <summary>
/// Response from the client status API endpoint containing version information and content updates
/// </summary>
public class ClientStatusResponse
{
    /// <summary>
    /// Whether the client should force a content refresh regardless of other conditions
    /// </summary>
    public bool ForceRefresh { get; set; }

    /// <summary>
    /// The minimum supported client version
    /// </summary>
    public string MinSupportedVersion { get; set; } = string.Empty;

    /// <summary>
    /// The latest available client version
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// A user-friendly message about the status
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The content manifest containing changes to scenarios and media
    /// </summary>
    public ContentManifest ContentManifest { get; set; } = new();

    /// <summary>
    /// The current content bundle version
    /// </summary>
    public string BundleVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether an update is required (client version below minimum supported)
    /// </summary>
    public bool UpdateRequired { get; set; }
}

/// <summary>
/// Content manifest containing changes to scenarios and media
/// </summary>
public class ContentManifest
{
    /// <summary>
    /// Changes to scenarios (added, updated, removed)
    /// </summary>
    public ScenarioChanges Scenarios { get; set; } = new();

    /// <summary>
    /// Changes to media files (added, updated, removed)
    /// </summary>
    public MediaChanges Media { get; set; } = new();

    /// <summary>
    /// The current content bundle version
    /// </summary>
    public string BundleVersion { get; set; } = string.Empty;
}

/// <summary>
/// Changes to scenarios (added, updated, removed)
/// </summary>
public class ScenarioChanges
{
    /// <summary>
    /// List of scenario IDs that have been added
    /// </summary>
    public List<string> Added { get; set; } = new();

    /// <summary>
    /// List of scenario IDs that have been updated
    /// </summary>
    public List<string> Updated { get; set; } = new();

    /// <summary>
    /// List of scenario IDs that have been removed
    /// </summary>
    public List<string> Removed { get; set; } = new();
}

/// <summary>
/// Changes to media files (added, updated, removed)
/// </summary>
public class MediaChanges
{
    /// <summary>
    /// List of media items that have been added
    /// </summary>
    public List<MediaItem> Added { get; set; } = new();

    /// <summary>
    /// List of media items that have been updated
    /// </summary>
    public List<MediaItem> Updated { get; set; } = new();

    /// <summary>
    /// List of media IDs that have been removed
    /// </summary>
    public List<string> Removed { get; set; } = new();
}

/// <summary>
/// Information about a media file
/// </summary>
public class MediaItem
{
    /// <summary>
    /// The unique identifier for the media
    /// </summary>
    public string MediaId { get; set; } = string.Empty;

    /// <summary>
    /// The file path for the media
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The version of the media (typically a timestamp)
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// A hash of the media content for integrity verification
    /// </summary>
    public string Hash { get; set; } = string.Empty;
}

