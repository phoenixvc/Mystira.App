using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Infrastructure.Data;
using YamlDotNet.Serialization;
using ApiModels = Mystira.App.Admin.Api.Models;
using DomainModels = Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

/// <summary>
/// Service for managing media metadata files
/// </summary>
public class MediaMetadataService : IMediaMetadataService
{
    private readonly MystiraAppDbContext _context;
    private readonly ILogger<MediaMetadataService> _logger;

    public MediaMetadataService(MystiraAppDbContext context, ILogger<MediaMetadataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the media metadata file
    /// </summary>
    public async Task<ApiModels.MediaMetadataFile?> GetMediaMetadataFileAsync()
    {
        try
        {
            // Attempt with normal EF Core approach first
            try
            {
                var domainFile = await _context.MediaMetadataFiles.FirstOrDefaultAsync();
                if (domainFile != null)
                {
                    // Ensure Entries is initialized
                    if (domainFile.Entries == null)
                    {
                        domainFile.Entries = new List<DomainModels.MediaMetadataEntry>();
                    }
                    return ConvertToApiModel(domainFile);
                }

                // No metadata file found
                return null;
            }
            catch (InvalidCastException ex)
            {
                // Log the specific error about the cast exception
                _logger.LogError(ex, "Cast exception occurred when retrieving metadata file. This likely indicates data format issues in Cosmos DB.");

                // Return null instead of creating a new instance
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media metadata file");
            throw;
        }
    }

    /// <summary>
    /// Updates the media metadata file
    /// </summary>
    public async Task<ApiModels.MediaMetadataFile> UpdateMediaMetadataFileAsync(ApiModels.MediaMetadataFile metadataFile)
    {
        try
        {
            var domainFile = ConvertToDomainModel(metadataFile);
            domainFile.UpdatedAt = DateTime.UtcNow;

            var existingFile = await _context.MediaMetadataFiles.FirstOrDefaultAsync();
            if (existingFile != null)
            {
                _context.Entry(existingFile).CurrentValues.SetValues(domainFile);
                existingFile.Entries = domainFile.Entries;
            }
            else
            {
                await _context.MediaMetadataFiles.AddAsync(domainFile);
            }

            await _context.SaveChangesAsync();
            return ConvertToApiModel(domainFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media metadata file");
            throw;
        }
    }

    /// <summary>
    /// Adds a new media metadata entry
    /// </summary>
    public async Task<ApiModels.MediaMetadataFile> AddMediaMetadataEntryAsync(ApiModels.MediaMetadataEntry entry)
    {
        try
        {
            var apiFile = await GetMediaMetadataFileAsync();
            var domainFile = apiFile == null ? new DomainModels.MediaMetadataFile() : ConvertToDomainModel(apiFile);

            // Check if entry already exists
            var existingEntry = domainFile.Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existingEntry != null)
            {
                throw new InvalidOperationException($"Media metadata entry with ID '{entry.Id}' already exists");
            }

            // Add the entry
            domainFile.Entries.Add(ConvertToDomainEntry(entry));
            return await UpdateMediaMetadataFileAsync(ConvertToApiModel(domainFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding media metadata entry: {EntryId}", entry.Id);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing media metadata entry
    /// </summary>
    public async Task<ApiModels.MediaMetadataFile> UpdateMediaMetadataEntryAsync(string entryId, ApiModels.MediaMetadataEntry entry)
    {
        try
        {
            var apiFile = await GetMediaMetadataFileAsync();
            if (apiFile == null)
            {
                throw new InvalidOperationException("Media metadata file not found");
            }

            var domainFile = ConvertToDomainModel(apiFile);
            var existingEntry = domainFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Media metadata entry with ID '{entryId}' not found");
            }

            // Update the entry
            var index = domainFile.Entries.IndexOf(existingEntry);
            entry.Id = entryId; // Ensure ID stays the same
            domainFile.Entries[index] = ConvertToDomainEntry(entry);

            return await UpdateMediaMetadataFileAsync(ConvertToApiModel(domainFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    /// <summary>
    /// Removes a media metadata entry
    /// </summary>
    public async Task<ApiModels.MediaMetadataFile> RemoveMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var apiFile = await GetMediaMetadataFileAsync();
            if (apiFile == null)
            {
                throw new InvalidOperationException("Media metadata file not found");
            }

            var domainFile = ConvertToDomainModel(apiFile);
            var existingEntry = domainFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Media metadata entry with ID '{entryId}' not found");
            }

            domainFile.Entries.Remove(existingEntry);
            return await UpdateMediaMetadataFileAsync(ConvertToApiModel(domainFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific media metadata entry by ID
    /// </summary>
    public async Task<ApiModels.MediaMetadataEntry?> GetMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var apiFile = await GetMediaMetadataFileAsync();
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
            _logger.LogError(ex, "Error retrieving media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    /// <summary>
    /// Imports media metadata entries from JSON or YAML data
    /// </summary>
    public async Task<ApiModels.MediaMetadataFile> ImportMediaMetadataEntriesAsync(string data, bool overwriteExisting = false)
    {
        try
        {
            List<ApiModels.MediaMetadataEntry> importedEntries;

            // Try to determine if data is JSON or YAML
            if (data.TrimStart().StartsWith('[') || data.TrimStart().StartsWith('{'))
            {
                // JSON format
                importedEntries = JsonSerializer.Deserialize<List<ApiModels.MediaMetadataEntry>>(data) ?? new List<ApiModels.MediaMetadataEntry>();
            }
            else
            {
                // YAML format
                var deserializer = new DeserializerBuilder().Build();
                importedEntries = deserializer.Deserialize<List<ApiModels.MediaMetadataEntry>>(data) ?? new List<ApiModels.MediaMetadataEntry>();
            }

            if (importedEntries == null || importedEntries.Count == 0)
            {
                throw new ArgumentException("No valid media metadata entries found in data");
            }

            var apiFile = await GetMediaMetadataFileAsync();
            var domainFile = apiFile == null ? new DomainModels.MediaMetadataFile() : ConvertToDomainModel(apiFile);

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
                        _logger.LogWarning("Skipping existing media metadata entry: {EntryId}", domainEntry.Id);
                    }
                }
                else
                {
                    domainFile.Entries.Add(domainEntry);
                }
            }

            return await UpdateMediaMetadataFileAsync(ConvertToApiModel(domainFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing media metadata entries");
            throw;
        }
    }

    private static ApiModels.MediaMetadataFile ConvertToApiModel(DomainModels.MediaMetadataFile domainFile)
    {
        return new ApiModels.MediaMetadataFile
        {
            Id = domainFile.Id,
            Entries = domainFile.Entries.Select(ConvertToApiEntry).ToList(),
            CreatedAt = domainFile.CreatedAt,
            UpdatedAt = domainFile.UpdatedAt,
            Version = domainFile.Version
        };
    }

    private static DomainModels.MediaMetadataFile ConvertToDomainModel(ApiModels.MediaMetadataFile apiFile)
    {
        return new DomainModels.MediaMetadataFile
        {
            Id = apiFile.Id,
            Entries = apiFile.Entries.Select(ConvertToDomainEntry).ToList(),
            CreatedAt = apiFile.CreatedAt,
            UpdatedAt = apiFile.UpdatedAt,
            Version = apiFile.Version
        };
    }

    private static ApiModels.MediaMetadataEntry ConvertToApiEntry(DomainModels.MediaMetadataEntry domainEntry)
    {
        return new ApiModels.MediaMetadataEntry
        {
            Id = domainEntry.Id,
            Title = domainEntry.Title,
            FileName = domainEntry.FileName,
            Type = domainEntry.Type,
            Description = domainEntry.Description,
            AgeRating = domainEntry.AgeRating,
            SubjectReferenceId = domainEntry.SubjectReferenceId,
            ClassificationTags = domainEntry.ClassificationTags.Select(t => new ApiModels.ClassificationTag
            {
                Key = t.Key,
                Value = t.Value
            }).ToList(),
            Modifiers = domainEntry.Modifiers.Select(m => new ApiModels.Modifier
            {
                Key = m.Key,
                Value = m.Value
            }).ToList(),
            Loopable = domainEntry.Loopable
        };
    }

    private static DomainModels.MediaMetadataEntry ConvertToDomainEntry(ApiModels.MediaMetadataEntry apiEntry)
    {
        return new DomainModels.MediaMetadataEntry
        {
            Id = apiEntry.Id,
            Title = apiEntry.Title,
            FileName = apiEntry.FileName,
            Type = apiEntry.Type,
            Description = apiEntry.Description,
            AgeRating = apiEntry.AgeRating,
            SubjectReferenceId = apiEntry.SubjectReferenceId,
            ClassificationTags = apiEntry.ClassificationTags.Select(t => new DomainModels.ClassificationTag
            {
                Key = t.Key,
                Value = t.Value
            }).ToList(),
            Modifiers = apiEntry.Modifiers.Select(m => new DomainModels.Modifier
            {
                Key = m.Key,
                Value = m.Value
            }).ToList(),
            Loopable = apiEntry.Loopable
        };
    }
}
