namespace Mystira.App.Shared.Media;

/// <summary>
/// Centralized registry for MIME types and media file extensions.
/// Eliminates duplication across MediaApiService, UploadMediaUseCase, and other media handlers.
/// </summary>
public static class MimeTypeRegistry
{
    /// <summary>
    /// Audio file extensions and their MIME types
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AudioTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".aac", "audio/aac" },
        { ".m4a", "audio/mp4" },
        { ".waptt", "audio/ogg" }
    };

    /// <summary>
    /// Video file extensions and their MIME types
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> VideoTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".mp4", "video/mp4" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".wmv", "video/x-ms-wmv" },
        { ".mkv", "video/x-matroska" },
        { ".webm", "video/webm" }
    };

    /// <summary>
    /// Image file extensions and their MIME types
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ImageTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".webp", "image/webp" },
        { ".svg", "image/svg+xml" }
    };

    /// <summary>
    /// Combined dictionary of all supported MIME types
    /// </summary>
    private static readonly Lazy<IReadOnlyDictionary<string, string>> _allTypes = new(() =>
    {
        var combined = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in AudioTypes) combined[kvp.Key] = kvp.Value;
        foreach (var kvp in VideoTypes) combined[kvp.Key] = kvp.Value;
        foreach (var kvp in ImageTypes) combined[kvp.Key] = kvp.Value;
        return combined;
    });

    /// <summary>
    /// All supported MIME types
    /// </summary>
    public static IReadOnlyDictionary<string, string> AllTypes => _allTypes.Value;

    /// <summary>
    /// Gets the MIME type for a given file extension or filename.
    /// Returns "application/octet-stream" if the extension is not recognized.
    /// </summary>
    /// <param name="fileNameOrExtension">The filename or extension (with or without leading dot)</param>
    /// <returns>The MIME type string</returns>
    public static string GetMimeType(string fileNameOrExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrExtension))
            return "application/octet-stream";

        var extension = fileNameOrExtension.StartsWith('.')
            ? fileNameOrExtension
            : Path.GetExtension(fileNameOrExtension);

        return AllTypes.TryGetValue(extension, out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }

    /// <summary>
    /// Gets the file extension for a given MIME type.
    /// Returns null if the MIME type is not recognized.
    /// </summary>
    /// <param name="mimeType">The MIME type</param>
    /// <returns>The file extension with leading dot, or null</returns>
    public static string? GetExtension(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return null;

        return AllTypes.FirstOrDefault(kvp =>
            kvp.Value.Equals(mimeType, StringComparison.OrdinalIgnoreCase)).Key;
    }

    /// <summary>
    /// Determines the media type category (audio, video, image) for a given extension.
    /// </summary>
    /// <param name="fileNameOrExtension">The filename or extension</param>
    /// <returns>The media type: "audio", "video", "image", or null if not recognized</returns>
    public static string? GetMediaType(string fileNameOrExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrExtension))
            return null;

        var extension = fileNameOrExtension.StartsWith('.')
            ? fileNameOrExtension
            : Path.GetExtension(fileNameOrExtension);

        if (AudioTypes.ContainsKey(extension)) return "audio";
        if (VideoTypes.ContainsKey(extension)) return "video";
        if (ImageTypes.ContainsKey(extension)) return "image";

        return null;
    }

    /// <summary>
    /// Gets all allowed extensions for a specific media type.
    /// </summary>
    /// <param name="mediaType">The media type: "audio", "video", or "image"</param>
    /// <returns>Array of allowed extensions with leading dots</returns>
    public static string[] GetAllowedExtensions(string mediaType)
    {
        return mediaType?.ToLowerInvariant() switch
        {
            "audio" => AudioTypes.Keys.ToArray(),
            "video" => VideoTypes.Keys.ToArray(),
            "image" => ImageTypes.Keys.ToArray(),
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Validates if a file extension is allowed for the specified media type.
    /// </summary>
    /// <param name="fileNameOrExtension">The filename or extension</param>
    /// <param name="mediaType">The expected media type</param>
    /// <returns>True if the extension is valid for the media type</returns>
    public static bool IsValidExtension(string fileNameOrExtension, string mediaType)
    {
        var allowedExtensions = GetAllowedExtensions(mediaType);
        if (allowedExtensions.Length == 0)
            return false;

        var extension = fileNameOrExtension.StartsWith('.')
            ? fileNameOrExtension
            : Path.GetExtension(fileNameOrExtension);

        return allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the maximum file size in bytes for a given media type.
    /// </summary>
    /// <param name="mediaType">The media type</param>
    /// <returns>Maximum file size in bytes</returns>
    public static long GetMaxFileSizeBytes(string mediaType)
    {
        return mediaType?.ToLowerInvariant() switch
        {
            "audio" => 50 * 1024 * 1024,   // 50MB
            "video" => 100 * 1024 * 1024,  // 100MB
            "image" => 10 * 1024 * 1024,   // 10MB
            _ => 10 * 1024 * 1024          // Default 10MB
        };
    }
}
