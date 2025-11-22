using Mystira.App.Api.Models;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing the single character map file
/// </summary>
public interface ICharacterMapFileService
{
    /// <summary>
    /// Gets the character map file
    /// </summary>
    /// <returns>The character map file</returns>
    Task<CharacterMapFile> GetCharacterMapFileAsync();

    /// <summary>
    /// Updates the character map file
    /// </summary>
    /// <param name="characterMapFile">The updated character map file</param>
    /// <returns>The updated character map file</returns>
    Task<CharacterMapFile> UpdateCharacterMapFileAsync(CharacterMapFile characterMapFile);

    /// <summary>
    /// Gets a specific character by ID
    /// </summary>
    /// <param name="characterId">The character ID</param>
    /// <returns>The character or null if not found</returns>
    Task<Character?> GetCharacterAsync(string characterId);

    /// <summary>
    /// Adds a new character
    /// </summary>
    /// <param name="character">The character to add</param>
    /// <returns>The updated character map file</returns>
    Task<CharacterMapFile> AddCharacterAsync(Character character);

    /// <summary>
    /// Updates an existing character
    /// </summary>
    /// <param name="characterId">The ID of the character to update</param>
    /// <param name="character">The updated character</param>
    /// <returns>The updated character map file</returns>
    Task<CharacterMapFile> UpdateCharacterAsync(string characterId, Character character);

    /// <summary>
    /// Removes a character
    /// </summary>
    /// <param name="characterId">The ID of the character to remove</param>
    /// <returns>The updated character map file</returns>
    Task<CharacterMapFile> RemoveCharacterAsync(string characterId);

    /// <summary>
    /// Exports the character map as JSON
    /// </summary>
    /// <returns>JSON representation of the character map</returns>
    Task<string> ExportCharacterMapAsync();

    /// <summary>
    /// Imports characters from JSON data
    /// </summary>
    /// <param name="jsonData">JSON data containing characters</param>
    /// <param name="overwriteExisting">Whether to overwrite existing characters</param>
    /// <returns>The updated character map file</returns>
    Task<CharacterMapFile> ImportCharacterMapAsync(string jsonData, bool overwriteExisting = false);
}
