using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// DEPRECATED: This service violates hexagonal architecture.
/// Controllers should use IMediator (CQRS pattern) instead.
/// </summary>
/// <remarks>
/// Migration guide:
/// - GetMediaMetadataFileAsync → GetMediaMetadataFileQuery
/// - Other methods → Create corresponding CQRS commands/queries as needed
/// See ARCHITECTURAL_REFACTORING_PLAN.md for details.
/// </remarks>
[Obsolete("Use IMediator with CQRS queries/commands instead. See ARCHITECTURAL_REFACTORING_PLAN.md")]
public interface IMediaMetadataService
{
    /// <summary>
    /// Gets the media metadata file
    /// </summary>
    /// <returns>The media metadata file</returns>
    Task<MediaMetadataFile?> GetMediaMetadataFileAsync();

    /// <summary>
    /// Updates the media metadata file
    /// </summary>
    /// <param name="metadataFile">The updated media metadata file</param>
    /// <returns>The updated media metadata file</returns>
    Task<MediaMetadataFile> UpdateMediaMetadataFileAsync(MediaMetadataFile metadataFile);

    /// <summary>
    /// Adds a new media metadata entry
    /// </summary>
    /// <param name="entry">The media metadata entry to add</param>
    /// <returns>The updated media metadata file</returns>
    Task<MediaMetadataFile> AddMediaMetadataEntryAsync(MediaMetadataEntry entry);

    /// <summary>
    /// Updates an existing media metadata entry
    /// </summary>
    /// <param name="entryId">The ID of the entry to update</param>
    /// <param name="entry">The updated media metadata entry</param>
    /// <returns>The updated media metadata file</returns>
    Task<MediaMetadataFile> UpdateMediaMetadataEntryAsync(string entryId, MediaMetadataEntry entry);

    /// <summary>
    /// Removes a media metadata entry
    /// </summary>
    /// <param name="entryId">The ID of the entry to remove</param>
    /// <returns>The updated media metadata file</returns>
    Task<MediaMetadataFile> RemoveMediaMetadataEntryAsync(string entryId);

    /// <summary>
    /// Gets a specific media metadata entry by ID
    /// </summary>
    /// <param name="entryId">The ID of the entry</param>
    /// <returns>The media metadata entry or null if not found</returns>
    Task<MediaMetadataEntry?> GetMediaMetadataEntryAsync(string entryId);

    /// <summary>
    /// Imports media metadata entries from a JSON file
    /// </summary>
    /// <param name="jsonData">The JSON data containing media metadata entries</param>
    /// <param name="overwriteExisting">Whether to overwrite existing entries</param>
    /// <returns>The updated media metadata file</returns>
    Task<MediaMetadataFile> ImportMediaMetadataEntriesAsync(string jsonData, bool overwriteExisting = false);
}
