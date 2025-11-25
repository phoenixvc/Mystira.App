using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// DEPRECATED: This service violates hexagonal architecture.
/// Controllers should use IMediator (CQRS pattern) instead.
/// </summary>
/// <remarks>
/// Migration guide:
/// - GetCharacterMediaMetadataFileAsync → GetCharacterMediaMetadataFileQuery
/// - GetCharacterMediaMetadataEntryAsync → GetCharacterMediaMetadataEntryQuery
/// - Other methods → Create corresponding CQRS commands/queries as needed
/// See ARCHITECTURAL_REFACTORING_PLAN.md for details.
/// </remarks>
[Obsolete("Use IMediator with CQRS queries/commands instead. See ARCHITECTURAL_REFACTORING_PLAN.md")]
public interface ICharacterMediaMetadataService
{
    /// <summary>
    /// Gets the character media metadata file
    /// </summary>
    /// <returns>The character media metadata file</returns>
    Task<CharacterMediaMetadataFile> GetCharacterMediaMetadataFileAsync();

    /// <summary>
    /// Updates the character media metadata file
    /// </summary>
    /// <param name="metadataFile">The updated character media metadata file</param>
    /// <returns>The updated character media metadata file</returns>
    Task<CharacterMediaMetadataFile> UpdateCharacterMediaMetadataFileAsync(CharacterMediaMetadataFile metadataFile);

    /// <summary>
    /// Adds a new character media metadata entry
    /// </summary>
    /// <param name="entry">The character media metadata entry to add</param>
    /// <returns>The updated character media metadata file</returns>
    Task<CharacterMediaMetadataFile> AddCharacterMediaMetadataEntryAsync(CharacterMediaMetadataEntry entry);

    /// <summary>
    /// Updates an existing character media metadata entry
    /// </summary>
    /// <param name="entryId">The ID of the entry to update</param>
    /// <param name="entry">The updated character media metadata entry</param>
    /// <returns>The updated character media metadata file</returns>
    Task<CharacterMediaMetadataFile> UpdateCharacterMediaMetadataEntryAsync(string entryId, CharacterMediaMetadataEntry entry);

    /// <summary>
    /// Removes a character media metadata entry
    /// </summary>
    /// <param name="entryId">The ID of the entry to remove</param>
    /// <returns>The updated character media metadata file</returns>
    Task<CharacterMediaMetadataFile> RemoveCharacterMediaMetadataEntryAsync(string entryId);

    /// <summary>
    /// Gets a specific character media metadata entry by ID
    /// </summary>
    /// <param name="entryId">The ID of the entry</param>
    /// <returns>The character media metadata entry or null if not found</returns>
    Task<CharacterMediaMetadataEntry?> GetCharacterMediaMetadataEntryAsync(string entryId);

    /// <summary>
    /// Imports character media metadata entries from a JSON file
    /// </summary>
    /// <param name="jsonData">The JSON data containing character media metadata entries</param>
    /// <param name="overwriteExisting">Whether to overwrite existing entries</param>
    /// <returns>The updated character media metadata file</returns>
    Task<CharacterMediaMetadataFile> ImportCharacterMediaMetadataEntriesAsync(string jsonData, bool overwriteExisting = false);
}
