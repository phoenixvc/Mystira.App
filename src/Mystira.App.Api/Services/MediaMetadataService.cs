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
/// Service for managing media metadata files
/// </summary>
public class MediaMetadataService : IMediaMetadataService
{
    private readonly IMediaMetadataFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MediaMetadataService> _logger;

    public MediaMetadataService(
        IMediaMetadataFileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<MediaMetadataService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
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
                var domainFile = await _repository.GetAsync();
                if (domainFile != null)
                {
                    // Ensure Entries is initialized
                    if (domainFile.Entries == null)
                    {
                        domainFile.Entries = new List<Domain.Models.MediaMetadataEntry>();
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

            var result = await _repository.AddOrUpdateAsync(domainFile);
            await _unitOfWork.SaveChangesAsync();
            return ConvertToApiModel(result);
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
            var metadataFile = await GetMediaMetadataFileAsync();

            // Check if entry already exists
            var existingEntry = metadataFile?.Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existingEntry != null)
            {
                throw new InvalidOperationException($"Media metadata entry with ID '{entry.Id}' already exists");
            }

            // Ensure Entries is initialized
            metadataFile ??= new ApiModels.MediaMetadataFile();
            metadataFile.Entries.Add(entry);
            return await UpdateMediaMetadataFileAsync(metadataFile);
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
            var metadataFile = await GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                throw new InvalidOperationException("Media metadata file not found");
            }

            var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Media metadata entry with ID '{entryId}' not found");
            }

            // Update the entry
            var index = metadataFile.Entries.IndexOf(existingEntry);
            entry.Id = entryId; // Ensure ID stays the same
            metadataFile.Entries[index] = entry;

            return await UpdateMediaMetadataFileAsync(metadataFile);
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
            var metadataFile = await GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                throw new InvalidOperationException("Media metadata file not found");
            }

            var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Media metadata entry with ID '{entryId}' not found");
            }

            metadataFile.Entries.Remove(existingEntry);
            return await UpdateMediaMetadataFileAsync(metadataFile);
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
            var metadataFile = await GetMediaMetadataFileAsync();
            return metadataFile?.Entries.FirstOrDefault(e => e.Id == entryId);
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

            var metadataFile = await GetMediaMetadataFileAsync() ?? new ApiModels.MediaMetadataFile();

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
                        _logger.LogWarning("Skipping existing media metadata entry: {EntryId}", entry.Id);
                    }
                }
                else
                {
                    metadataFile.Entries.Add(entry);
                }
            }

            return await UpdateMediaMetadataFileAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing media metadata entries");
            throw;
        }
    }

    private static Models.MediaMetadataFile ConvertToApiModel(Domain.Models.MediaMetadataFile domainFile)
    {
        return new Models.MediaMetadataFile
        {
            Id = domainFile.Id,
            Entries = domainFile.Entries.Select(ConvertToApiEntry).ToList(),
            CreatedAt = domainFile.CreatedAt,
            UpdatedAt = domainFile.UpdatedAt,
            Version = domainFile.Version
        };
    }

    private static Domain.Models.MediaMetadataFile ConvertToDomainModel(Models.MediaMetadataFile apiFile)
    {
        return new Domain.Models.MediaMetadataFile
        {
            Id = apiFile.Id,
            Entries = apiFile.Entries.Select(ConvertToDomainEntry).ToList(),
            CreatedAt = apiFile.CreatedAt,
            UpdatedAt = apiFile.UpdatedAt,
            Version = apiFile.Version
        };
    }

    private static Models.MediaMetadataEntry ConvertToApiEntry(Domain.Models.MediaMetadataEntry domainEntry)
    {
        return new Models.MediaMetadataEntry
        {
            Id = domainEntry.Id,
            Title = domainEntry.Title,
            FileName = domainEntry.FileName,
            Type = domainEntry.Type,
            Description = domainEntry.Description,
            AgeRating = domainEntry.AgeRating,
            SubjectReferenceId = domainEntry.SubjectReferenceId,
            ClassificationTags = domainEntry.ClassificationTags.Select(t => new Models.ClassificationTag
            {
                Key = t.Key,
                Value = t.Value
            }).ToList(),
            Modifiers = domainEntry.Modifiers.Select(m => new Models.Modifier
            {
                Key = m.Key,
                Value = m.Value
            }).ToList(),
            Loopable = domainEntry.Loopable
        };
    }

    private static Domain.Models.MediaMetadataEntry ConvertToDomainEntry(Models.MediaMetadataEntry apiEntry)
    {
        return new Domain.Models.MediaMetadataEntry
        {
            Id = apiEntry.Id,
            Title = apiEntry.Title,
            FileName = apiEntry.FileName,
            Type = apiEntry.Type,
            Description = apiEntry.Description,
            AgeRating = apiEntry.AgeRating,
            SubjectReferenceId = apiEntry.SubjectReferenceId,
            ClassificationTags = apiEntry.ClassificationTags.Select(t => new Domain.Models.ClassificationTag
            {
                Key = t.Key,
                Value = t.Value
            }).ToList(),
            Modifiers = apiEntry.Modifiers.Select(m => new Domain.Models.Modifier
            {
                Key = m.Key,
                Value = m.Value
            }).ToList(),
            Loopable = apiEntry.Loopable
        };
    }
}
