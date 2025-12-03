namespace Mystira.App.Application.CQRS.MediaAssets.Queries;

/// <summary>
/// Query to validate that a list of media IDs exist in the system.
/// Used when creating/updating scenarios, characters, etc. to ensure referenced media exists.
/// Returns list of missing media IDs (empty list if all valid).
/// </summary>
public record ValidateMediaReferencesQuery(List<string> MediaIds)
    : IQuery<MediaValidationResult>;

/// <summary>
/// Result of media reference validation
/// </summary>
public class MediaValidationResult
{
    /// <summary>
    /// True if all media references are valid (no missing IDs)
    /// </summary>
    public bool IsValid => MissingMediaIds.Count == 0;

    /// <summary>
    /// List of media IDs that don't exist in the system
    /// </summary>
    public List<string> MissingMediaIds { get; set; } = new();

    /// <summary>
    /// Total number of media IDs validated
    /// </summary>
    public int TotalValidated { get; set; }

    /// <summary>
    /// Number of valid media IDs found
    /// </summary>
    public int ValidCount { get; set; }
}
