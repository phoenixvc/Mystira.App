using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Infrastructure.Data;
using Mystira.App.Admin.Api.Models;
using Mystira.App.Domain.Models;
using YamlDotNet.Serialization;
using ApiModels = Mystira.App.Admin.Api.Models;
using DomainModels = Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

/// <summary>
/// Service for managing character media metadata files
/// </summary>
public class CharacterMediaMetadataService : ICharacterMediaMetadataService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<CharacterMediaMetadataService> _logger;

    public CharacterMediaMetadataService(MystiraAppDbContext context, ILogger<CharacterMediaMetadataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the character media metadata file
    /// </summary>
    public async Task<ApiModels.CharacterMediaMetadataFile> GetCharacterMediaMetadataFileAsync()
    {
        try
        {
            var domainFile = await _context.CharacterMediaMetadataFiles.FirstOrDefaultAsync();
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

            var existingFile = await _context.CharacterMediaMetadataFiles.FirstOrDefaultAsync();
            if (existingFile != null)
            {
                _context.Entry(existingFile).CurrentValues.SetValues(domainFile);
                existingFile.Entries = domainFile.Entries;
            }
            else
            {
                await _context.CharacterMediaMetadataFiles.AddAsync(domainFile);
            }

            await _context.SaveChangesAsync();
            return ConvertToApiModel(domainFile);
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
            var apiFile = await GetCharacterMediaMetadataFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            // Check if entry already exists
            var existingEntry = domainFile.Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existingEntry != null)
            {
                throw new InvalidOperationException($"Character media metadata entry with ID '{entry.Id}' already exists");
            }

            domainFile.Entries.Add(ConvertToDomainEntry(entry));
            return await UpdateCharacterMediaMetadataFileAsync(ConvertToApiModel(domainFile));
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
            var apiFile = await GetCharacterMediaMetadataFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            var existingEntry = domainFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Character media metadata entry with ID '{entryId}' not found");
            }

            // Update the entry
            var index = domainFile.Entries.IndexOf(existingEntry);
            entry.Id = entryId; // Ensure ID stays the same
            domainFile.Entries[index] = ConvertToDomainEntry(entry);

            return await UpdateCharacterMediaMetadataFileAsync(ConvertToApiModel(domainFile));
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
            var apiFile = await GetCharacterMediaMetadataFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            var existingEntry = domainFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Character media metadata entry with ID '{entryId}' not found");
            }

            domainFile.Entries.Remove(existingEntry);
            return await UpdateCharacterMediaMetadataFileAsync(ConvertToApiModel(domainFile));
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
            var apiFile = await GetCharacterMediaMetadataFileAsync();
            if (apiFile == null)
            {
                return null;
            }

            var domainFile = ConvertToDomainModel(apiFile);
            var domainEntry = domainFile.Entries.FirstOrDefault(e => e.Id == entryId);
            return domainEntry == null ? null : ConvertToApiEntry(domainEntry);
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

            var apiFile = await GetCharacterMediaMetadataFileAsync();
            var domainFile = ConvertToDomainModel(apiFile);

            foreach (var entry in importedEntries)
            {
                var domainEntry = ConvertToDomainEntry(entry);
                var existingEntry = domainFile.Entries.FirstOrDefault(e => e.Id == domainEntry.Id);
                if (existingEntry != null)
                {
                    if (overwriteExisting)
                    {
                        var index = domainFile.Entries.IndexOf(existingEntry);
                        domainFile.Entries[index] = domainEntry;
                    }
                    else
                    {
                        _logger.LogWarning("Skipping existing character media metadata entry: {EntryId}", domainEntry.Id);
                    }
                }
                else
                {
                    domainFile.Entries.Add(domainEntry);
                }
            }

            return await UpdateCharacterMediaMetadataFileAsync(ConvertToApiModel(domainFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing character media metadata entries");
            throw;
        }
    }

    private static ApiModels.CharacterMediaMetadataFile ConvertToApiModel(DomainModels.CharacterMediaMetadataFile domainFile)
    {
        return new ApiModels.CharacterMediaMetadataFile
        {
            Id = domainFile.Id,
            Entries = domainFile.Entries.Select(ConvertToApiEntry).ToList(),
            CreatedAt = domainFile.CreatedAt,
            UpdatedAt = domainFile.UpdatedAt,
            Version = domainFile.Version
        };
    }

    private static DomainModels.CharacterMediaMetadataFile ConvertToDomainModel(ApiModels.CharacterMediaMetadataFile apiFile)
    {
        return new DomainModels.CharacterMediaMetadataFile
        {
            Id = apiFile.Id,
            Entries = apiFile.Entries.Select(ConvertToDomainEntry).ToList(),
            CreatedAt = apiFile.CreatedAt,
            UpdatedAt = apiFile.UpdatedAt,
            Version = apiFile.Version
        };
    }

    private static ApiModels.CharacterMediaMetadataEntry ConvertToApiEntry(DomainModels.CharacterMediaMetadataEntry domainEntry)
    {
        return new ApiModels.CharacterMediaMetadataEntry
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

    private static DomainModels.CharacterMediaMetadataEntry ConvertToDomainEntry(ApiModels.CharacterMediaMetadataEntry apiEntry)
    {
        return new DomainModels.CharacterMediaMetadataEntry
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
