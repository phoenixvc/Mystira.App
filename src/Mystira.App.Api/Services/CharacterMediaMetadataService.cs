using System.Text.Json;
using Mystira.App.Api.Models;
using Mystira.App.Api.Repositories;
using Mystira.App.Contracts.Responses.Common;
using Mystira.App.Domain.Models;
using Mystira.App.Application.Ports.Data;
using YamlDotNet.Serialization;
using ApiModels = Mystira.App.Api.Models;

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
    public async Task<ApiModels.CharacterMediaMetadataFile> GetCharacterMediaMetadataFileAsync()
    {
        try
        {
            var domainFile = await _repository.GetAsync();
            return domainFile == null ? new ApiModels.CharacterMediaMetadataFile() : ConvertToApiModel(domainFile);
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
    public async Task<ApiModels.CharacterMediaMetadataFile> UpdateCharacterMediaMetadataFileAsync(ApiModels.CharacterMediaMetadataFile metadataFile)
    {
        try
        {
            var domainFile = ConvertToDomainModel(metadataFile);
            domainFile.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.AddOrUpdateAsync(domainFile);
            await _unitOfWork.SaveChangesAsync();
            return ConvertToApiModel(result);
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
    public async Task<ApiModels.CharacterMediaMetadataFile> AddCharacterMediaMetadataEntryAsync(ApiModels.CharacterMediaMetadataEntry entry)
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
    public async Task<ApiModels.CharacterMediaMetadataFile> UpdateCharacterMediaMetadataEntryAsync(string entryId, ApiModels.CharacterMediaMetadataEntry entry)
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
    public async Task<ApiModels.CharacterMediaMetadataFile> RemoveCharacterMediaMetadataEntryAsync(string entryId)
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
    public async Task<ApiModels.CharacterMediaMetadataEntry?> GetCharacterMediaMetadataEntryAsync(string entryId)
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
    public async Task<ApiModels.CharacterMediaMetadataFile> ImportCharacterMediaMetadataEntriesAsync(string data, bool overwriteExisting = false)
    {
        try
        {
            List<ApiModels.CharacterMediaMetadataEntry> importedEntries;

            // Try to determine if data is JSON or YAML
            if (data.TrimStart().StartsWith('[') || data.TrimStart().StartsWith('{'))
            {
                // JSON format
                importedEntries = JsonSerializer.Deserialize<List<ApiModels.CharacterMediaMetadataEntry>>(data) ?? new List<ApiModels.CharacterMediaMetadataEntry>();
            }
            else
            {
                // YAML format
                var deserializer = new DeserializerBuilder().Build();
                importedEntries = deserializer.Deserialize<List<ApiModels.CharacterMediaMetadataEntry>>(data) ?? new List<ApiModels.CharacterMediaMetadataEntry>();
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

    private static Models.CharacterMediaMetadataFile ConvertToApiModel(Domain.Models.CharacterMediaMetadataFile domainFile)
    {
        return new Models.CharacterMediaMetadataFile
        {
            Id = domainFile.Id,
            Entries = domainFile.Entries.Select(ConvertToApiEntry).ToList(),
            CreatedAt = domainFile.CreatedAt,
            UpdatedAt = domainFile.UpdatedAt,
            Version = domainFile.Version
        };
    }

    private static Domain.Models.CharacterMediaMetadataFile ConvertToDomainModel(Models.CharacterMediaMetadataFile apiFile)
    {
        return new Domain.Models.CharacterMediaMetadataFile
        {
            Id = apiFile.Id,
            Entries = apiFile.Entries.Select(ConvertToDomainEntry).ToList(),
            CreatedAt = apiFile.CreatedAt,
            UpdatedAt = apiFile.UpdatedAt,
            Version = apiFile.Version
        };
    }

    private static Models.CharacterMediaMetadataEntry ConvertToApiEntry(Domain.Models.CharacterMediaMetadataEntry domainEntry)
    {
        return new Models.CharacterMediaMetadataEntry
        {
            Id = domainEntry.Id,
            Title = domainEntry.Title,
            FileName = domainEntry.FileName,
            Type = domainEntry.Type,
            Description = domainEntry.Description,
            AgeRating = domainEntry.AgeRating,
            Tags = domainEntry.Tags,
            Loopable = domainEntry.Loopable
        };
    }

    private static Domain.Models.CharacterMediaMetadataEntry ConvertToDomainEntry(Models.CharacterMediaMetadataEntry apiEntry)
    {
        return new Domain.Models.CharacterMediaMetadataEntry
        {
            Id = apiEntry.Id,
            Title = apiEntry.Title,
            FileName = apiEntry.FileName,
            Type = apiEntry.Type,
            Description = apiEntry.Description,
            AgeRating = apiEntry.AgeRating,
            Tags = apiEntry.Tags,
            Loopable = apiEntry.Loopable
        };
    }
}
