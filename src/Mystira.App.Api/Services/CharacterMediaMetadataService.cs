using System.Text.Json;
using Mystira.App.Api.Models;
using Mystira.App.Api.Repositories;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Infrastructure.Data.UnitOfWork;
using YamlDotNet.Serialization;

namespace Mystira.App.Api.Services;

/// <summary>
/// Service for managing character media metadata files
/// </summary>
public class CharacterMediaMetadataService : ICharacterMediaMetadataService
{
    private readonly ICharacterMediaMetadataFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CharacterMediaMetadataService> _logger;

    public CharacterMediaMetadataService(
        ICharacterMediaMetadataFileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CharacterMediaMetadataService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets the character media metadata file
    /// </summary>
    public async Task<CharacterMediaMetadataFile> GetCharacterMediaMetadataFileAsync()
    {
        try
        {
            var metadataFile = await _repository.GetAsync();
            return metadataFile ?? new CharacterMediaMetadataFile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving character media metadata file");
            throw;
        }
    }

    /// <summary>
    /// Updates the character media metadata file
    /// </summary>
    public async Task<CharacterMediaMetadataFile> UpdateCharacterMediaMetadataFileAsync(CharacterMediaMetadataFile metadataFile)
    {
        try
        {
            metadataFile.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.AddOrUpdateAsync(metadataFile);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character media metadata file");
            throw;
        }
    }

    /// <summary>
    /// Adds a new character media metadata entry
    /// </summary>
    public async Task<CharacterMediaMetadataFile> AddCharacterMediaMetadataEntryAsync(CharacterMediaMetadataEntry entry)
    {
        try
        {
            var metadataFile = await GetCharacterMediaMetadataFileAsync();

            // Check if entry already exists
            var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existingEntry != null)
            {
                throw new InvalidOperationException($"Character media metadata entry with ID '{entry.Id}' already exists");
            }

            metadataFile.Entries.Add(entry);
            return await UpdateCharacterMediaMetadataFileAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character media metadata entry: {EntryId}", entry.Id);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing character media metadata entry
    /// </summary>
    public async Task<CharacterMediaMetadataFile> UpdateCharacterMediaMetadataEntryAsync(string entryId, CharacterMediaMetadataEntry entry)
    {
        try
        {
            var metadataFile = await GetCharacterMediaMetadataFileAsync();

            var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Character media metadata entry with ID '{entryId}' not found");
            }

            // Update the entry
            var index = metadataFile.Entries.IndexOf(existingEntry);
            entry.Id = entryId; // Ensure ID stays the same
            metadataFile.Entries[index] = entry;

            return await UpdateCharacterMediaMetadataFileAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    /// <summary>
    /// Removes a character media metadata entry
    /// </summary>
    public async Task<CharacterMediaMetadataFile> RemoveCharacterMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var metadataFile = await GetCharacterMediaMetadataFileAsync();

            var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Character media metadata entry with ID '{entryId}' not found");
            }

            metadataFile.Entries.Remove(existingEntry);
            return await UpdateCharacterMediaMetadataFileAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing character media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific character media metadata entry by ID
    /// </summary>
    public async Task<CharacterMediaMetadataEntry?> GetCharacterMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var metadataFile = await GetCharacterMediaMetadataFileAsync();
            return metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving character media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    /// <summary>
    /// Imports character media metadata entries from JSON or YAML data
    /// </summary>
    public async Task<CharacterMediaMetadataFile> ImportCharacterMediaMetadataEntriesAsync(string data, bool overwriteExisting = false)
    {
        try
        {
            List<CharacterMediaMetadataEntry> importedEntries;

            // Try to determine if data is JSON or YAML
            if (data.TrimStart().StartsWith('[') || data.TrimStart().StartsWith('{'))
            {
                // JSON format
                importedEntries = JsonSerializer.Deserialize<List<CharacterMediaMetadataEntry>>(data) ?? new List<CharacterMediaMetadataEntry>();
            }
            else
            {
                // YAML format
                var deserializer = new DeserializerBuilder().Build();
                importedEntries = deserializer.Deserialize<List<CharacterMediaMetadataEntry>>(data) ?? new List<CharacterMediaMetadataEntry>();
            }

            if (importedEntries == null || importedEntries.Count == 0)
            {
                throw new ArgumentException("No valid character media metadata entries found in data");
            }

            var metadataFile = await GetCharacterMediaMetadataFileAsync();

            foreach (var entry in importedEntries)
            {
                var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entry.Id);
                if (existingEntry != null)
                {
                    if (overwriteExisting)
                    {
                        var index = metadataFile.Entries.IndexOf(existingEntry);
                        metadataFile.Entries[index] = entry;
                    }
                    else
                    {
                        _logger.LogWarning("Skipping existing character media metadata entry: {EntryId}", entry.Id);
                    }
                }
                else
                {
                    metadataFile.Entries.Add(entry);
                }
            }

            return await UpdateCharacterMediaMetadataFileAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing character media metadata entries");
            throw;
        }
    }
}
