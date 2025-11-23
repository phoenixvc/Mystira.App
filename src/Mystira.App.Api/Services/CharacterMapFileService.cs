using System.Text.Json;
using Mystira.App.Api.Models;
using Mystira.App.Api.Repositories;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Infrastructure.Data.UnitOfWork;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing the single character map file
/// </summary>
public class CharacterMapFileService : ICharacterMapFileService
{
    private readonly ICharacterMapFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CharacterMapFileService> _logger;

    public CharacterMapFileService(
        ICharacterMapFileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CharacterMapFileService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets the character map file
    /// </summary>
    public async Task<CharacterMapFile> GetCharacterMapFileAsync()
    {
        try
        {
            var characterMapFile = await _repository.GetAsync();
            return characterMapFile ?? new CharacterMapFile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving character map file");
            throw;
        }
    }

    /// <summary>
    /// Updates the character map file
    /// </summary>
    public async Task<CharacterMapFile> UpdateCharacterMapFileAsync(CharacterMapFile characterMapFile)
    {
        try
        {
            characterMapFile.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.AddOrUpdateAsync(characterMapFile);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character map file");
            throw;
        }
    }

    /// <summary>
    /// Gets a specific character by ID
    /// </summary>
    public async Task<Character?> GetCharacterAsync(string characterId)
    {
        try
        {
            var characterMapFile = await GetCharacterMapFileAsync();
            return characterMapFile.Characters.FirstOrDefault(c => c.Id == characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving character: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Adds a new character
    /// </summary>
    public async Task<CharacterMapFile> AddCharacterAsync(Character character)
    {
        try
        {
            var characterMapFile = await GetCharacterMapFileAsync();

            // Check if character already exists
            var existingCharacter = characterMapFile.Characters.FirstOrDefault(c => c.Id == character.Id);
            if (existingCharacter != null)
            {
                throw new InvalidOperationException($"Character with ID '{character.Id}' already exists");
            }

            characterMapFile.Characters.Add(character);
            return await UpdateCharacterMapFileAsync(characterMapFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character: {CharacterId}", character.Id);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing character
    /// </summary>
    public async Task<CharacterMapFile> UpdateCharacterAsync(string characterId, Character character)
    {
        try
        {
            var characterMapFile = await GetCharacterMapFileAsync();

            var existingCharacter = characterMapFile.Characters.FirstOrDefault(c => c.Id == characterId);
            if (existingCharacter == null)
            {
                throw new KeyNotFoundException($"Character with ID '{characterId}' not found");
            }

            // Update the character
            var index = characterMapFile.Characters.IndexOf(existingCharacter);
            character.Id = characterId; // Ensure ID stays the same
            characterMapFile.Characters[index] = character;

            return await UpdateCharacterMapFileAsync(characterMapFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Removes a character
    /// </summary>
    public async Task<CharacterMapFile> RemoveCharacterAsync(string characterId)
    {
        try
        {
            var characterMapFile = await GetCharacterMapFileAsync();

            var existingCharacter = characterMapFile.Characters.FirstOrDefault(c => c.Id == characterId);
            if (existingCharacter == null)
            {
                throw new KeyNotFoundException($"Character with ID '{characterId}' not found");
            }

            characterMapFile.Characters.Remove(existingCharacter);
            return await UpdateCharacterMapFileAsync(characterMapFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing character: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Exports the character map as JSON
    /// </summary>
    public async Task<string> ExportCharacterMapAsync()
    {
        try
        {
            var characterMapFile = await GetCharacterMapFileAsync();

            var exportData = new
            {
                characters = characterMapFile.Characters
            };

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting character map");
            throw;
        }
    }

    /// <summary>
    /// Imports characters from JSON data
    /// </summary>
    public async Task<CharacterMapFile> ImportCharacterMapAsync(string jsonData, bool overwriteExisting = false)
    {
        try
        {
            var importData = JsonSerializer.Deserialize<Dictionary<string, List<Character>>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (importData == null || !importData.ContainsKey("characters"))
            {
                throw new ArgumentException("Invalid JSON format. Expected 'characters' array");
            }

            var importedCharacters = importData["characters"];
            if (importedCharacters == null || importedCharacters.Count == 0)
            {
                throw new ArgumentException("No valid characters found in JSON data");
            }

            var characterMapFile = await GetCharacterMapFileAsync();

            foreach (var character in importedCharacters)
            {
                var existingCharacter = characterMapFile.Characters.FirstOrDefault(c => c.Id == character.Id);
                if (existingCharacter != null)
                {
                    if (overwriteExisting)
                    {
                        var index = characterMapFile.Characters.IndexOf(existingCharacter);
                        characterMapFile.Characters[index] = character;
                    }
                    else
                    {
                        _logger.LogWarning("Skipping existing character: {CharacterId}", character.Id);
                    }
                }
                else
                {
                    characterMapFile.Characters.Add(character);
                }
            }

            return await UpdateCharacterMapFileAsync(characterMapFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing character map");
            throw;
        }
    }
}
