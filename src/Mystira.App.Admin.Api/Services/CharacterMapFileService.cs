using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Domain.Models;
using ApiModels = Mystira.App.Admin.Api.Models;
using DomainModels = Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

/// <summary>
/// Service for managing the single character map file
/// </summary>
public class CharacterMapFileService : ICharacterMapFileService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<CharacterMapFileService> _logger;

    public CharacterMapFileService(MystiraAppDbContext context, ILogger<CharacterMapFileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the character map file
    /// </summary>
    public async Task<ApiModels.CharacterMapFile> GetCharacterMapFileAsync()
    {
        try
        {
            var domainFile = await _context.CharacterMapFiles.FirstOrDefaultAsync();
            return domainFile == null ? new ApiModels.CharacterMapFile() : ConvertToApiModel(domainFile);
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
    public async Task<ApiModels.CharacterMapFile> UpdateCharacterMapFileAsync(ApiModels.CharacterMapFile characterMapFile)
    {
        try
        {
            var domainFile = ConvertToDomainModel(characterMapFile);
            domainFile.UpdatedAt = DateTime.UtcNow;

            var existingFile = await _context.CharacterMapFiles.FirstOrDefaultAsync();
            if (existingFile != null)
            {
                _context.Entry(existingFile).CurrentValues.SetValues(domainFile);
                existingFile.Characters = domainFile.Characters;
            }
            else
            {
                await _context.CharacterMapFiles.AddAsync(domainFile);
            }

            await _context.SaveChangesAsync();
            return ConvertToApiModel(domainFile);
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
    public async Task<ApiModels.Character?> GetCharacterAsync(string characterId)
    {
        try
        {
            var apiFile = await GetCharacterMapFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);
            var domainCharacter = domainFile.Characters.FirstOrDefault(c => c.Id == characterId);
            return domainCharacter == null ? null : ConvertToApiCharacter(domainCharacter);
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
    public async Task<ApiModels.CharacterMapFile> AddCharacterAsync(ApiModels.Character character)
    {
        try
        {
            var apiFile = await GetCharacterMapFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            // Check if character already exists
            var existingCharacter = domainFile.Characters.FirstOrDefault(c => c.Id == character.Id);
            if (existingCharacter != null)
            {
                throw new InvalidOperationException($"Character with ID '{character.Id}' already exists");
            }

            domainFile.Characters.Add(ConvertToDomainCharacter(character));
            return await UpdateCharacterMapFileAsync(ConvertToApiModel(domainFile));
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
    public async Task<ApiModels.CharacterMapFile> UpdateCharacterAsync(string characterId, ApiModels.Character character)
    {
        try
        {
            var apiFile = await GetCharacterMapFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            var existingCharacter = domainFile.Characters.FirstOrDefault(c => c.Id == characterId);
            if (existingCharacter == null)
            {
                throw new KeyNotFoundException($"Character with ID '{characterId}' not found");
            }

            // Update the character
            var index = domainFile.Characters.IndexOf(existingCharacter);
            character.Id = characterId; // Ensure ID stays the same
            domainFile.Characters[index] = ConvertToDomainCharacter(character);

            return await UpdateCharacterMapFileAsync(ConvertToApiModel(domainFile));
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
    public async Task<ApiModels.CharacterMapFile> RemoveCharacterAsync(string characterId)
    {
        try
        {
            var apiFile = await GetCharacterMapFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            var existingCharacter = domainFile.Characters.FirstOrDefault(c => c.Id == characterId);
            if (existingCharacter == null)
            {
                throw new KeyNotFoundException($"Character with ID '{characterId}' not found");
            }

            domainFile.Characters.Remove(existingCharacter);
            return await UpdateCharacterMapFileAsync(ConvertToApiModel(domainFile));
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
    public async Task<ApiModels.CharacterMapFile> ImportCharacterMapAsync(string jsonData, bool overwriteExisting = false)
    {
        try
        {
            var importData = JsonSerializer.Deserialize<Dictionary<string, List<ApiModels.Character>>>(jsonData, new JsonSerializerOptions
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

            var apiFile = await GetCharacterMapFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            foreach (var character in importedCharacters)
            {
                var domainCharacter = ConvertToDomainCharacter(character);
                var existingCharacter = domainFile.Characters.FirstOrDefault(c => c.Id == domainCharacter.Id);
                if (existingCharacter != null)
                {
                    if (overwriteExisting)
                    {
                        var index = domainFile.Characters.IndexOf(existingCharacter);
                        domainFile.Characters[index] = domainCharacter;
                    }
                    else
                    {
                        _logger.LogWarning("Skipping existing character: {CharacterId}", domainCharacter.Id);
                    }
                }
                else
                {
                    domainFile.Characters.Add(domainCharacter);
                }
            }

            return await UpdateCharacterMapFileAsync(ConvertToApiModel(domainFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing character map");
            throw;
        }
    }

    private static ApiModels.CharacterMapFile ConvertToApiModel(DomainModels.CharacterMapFile domainFile)
    {
        return new ApiModels.CharacterMapFile
        {
            Id = domainFile.Id,
            Characters = domainFile.Characters.Select(ConvertToApiCharacter).ToList(),
            CreatedAt = domainFile.CreatedAt,
            UpdatedAt = domainFile.UpdatedAt,
            Version = domainFile.Version
        };
    }

    private static DomainModels.CharacterMapFile ConvertToDomainModel(ApiModels.CharacterMapFile apiFile)
    {
        return new DomainModels.CharacterMapFile
        {
            Id = apiFile.Id,
            Characters = apiFile.Characters.Select(ConvertToDomainCharacter).ToList(),
            CreatedAt = apiFile.CreatedAt,
            UpdatedAt = apiFile.UpdatedAt,
            Version = apiFile.Version
        };
    }

    private static ApiModels.Character ConvertToApiCharacter(DomainModels.CharacterMapFileCharacter domainCharacter)
    {
        return new ApiModels.Character
        {
            Id = domainCharacter.Id,
            Name = domainCharacter.Name,
            Image = domainCharacter.Image,
            Metadata = ConvertToApiMetadata(domainCharacter.Metadata)
        };
    }

    private static DomainModels.CharacterMapFileCharacter ConvertToDomainCharacter(ApiModels.Character apiCharacter)
    {
        return new DomainModels.CharacterMapFileCharacter
        {
            Id = apiCharacter.Id,
            Name = apiCharacter.Name,
            Image = apiCharacter.Image,
            Metadata = ConvertToDomainMetadata(apiCharacter.Metadata)
        };
    }

    private static ApiModels.CharacterMetadata ConvertToApiMetadata(DomainModels.CharacterMetadata domainMetadata)
    {
        return new ApiModels.CharacterMetadata
        {
            Roles = domainMetadata.Roles,
            Archetypes = domainMetadata.Archetypes,
            Species = domainMetadata.Species,
            Age = domainMetadata.Age,
            Traits = domainMetadata.Traits,
            Backstory = domainMetadata.Backstory
        };
    }

    private static DomainModels.CharacterMetadata ConvertToDomainMetadata(ApiModels.CharacterMetadata apiMetadata)
    {
        return new DomainModels.CharacterMetadata
        {
            Roles = apiMetadata.Roles,
            Archetypes = apiMetadata.Archetypes,
            Species = apiMetadata.Species,
            Age = apiMetadata.Age,
            Traits = apiMetadata.Traits,
            Backstory = apiMetadata.Backstory
        };
    }
}
