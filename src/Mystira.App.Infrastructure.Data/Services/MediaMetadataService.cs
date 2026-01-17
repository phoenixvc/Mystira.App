using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Services;

/// <summary>
/// Infrastructure service implementation of Application.Ports.IMediaMetadataService
/// Uses repository pattern to access media metadata files
/// </summary>
public class MediaMetadataService : IMediaMetadataService
{
    private readonly IMediaMetadataFileRepository _repository;
    private readonly ILogger<MediaMetadataService> _logger;

    public MediaMetadataService(
        IMediaMetadataFileRepository repository,
        ILogger<MediaMetadataService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MediaMetadataFile?> GetMediaMetadataFileAsync()
    {
        try
        {
            var metadataFile = await _repository.GetAsync();
            if (metadataFile != null && metadataFile.Entries == null)
            {
                metadataFile.Entries = new List<MediaMetadataEntry>();
            }
            return metadataFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media metadata file");
            throw;
        }
    }

    public async Task<MediaMetadataFile> UpdateMediaMetadataFileAsync(MediaMetadataFile metadataFile)
    {
        try
        {
            metadataFile.UpdatedAt = DateTime.UtcNow;
            if (metadataFile.Entries == null)
            {
                metadataFile.Entries = new List<MediaMetadataEntry>();
            }
            return await _repository.AddOrUpdateAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media metadata file");
            throw;
        }
    }

    public async Task<MediaMetadataFile> AddMediaMetadataEntryAsync(MediaMetadataEntry entry)
    {
        try
        {
            var metadataFile = await GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                metadataFile = new MediaMetadataFile
                {
                    Id = Guid.NewGuid().ToString(),
                    Entries = new List<MediaMetadataEntry>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };
            }

            if (metadataFile.Entries == null)
            {
                metadataFile.Entries = new List<MediaMetadataEntry>();
            }

            var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existingEntry != null)
            {
                throw new InvalidOperationException($"Media metadata entry with ID '{entry.Id}' already exists");
            }

            metadataFile.Entries.Add(entry);
            return await UpdateMediaMetadataFileAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding media metadata entry: {EntryId}", entry.Id);
            throw;
        }
    }

    public async Task<MediaMetadataFile> UpdateMediaMetadataEntryAsync(string entryId, MediaMetadataEntry entry)
    {
        try
        {
            var metadataFile = await GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                throw new InvalidOperationException("Media metadata file not found");
            }

            if (metadataFile.Entries == null)
            {
                metadataFile.Entries = new List<MediaMetadataEntry>();
            }

            var existingEntry = metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"Media metadata entry with ID '{entryId}' not found");
            }

            var index = metadataFile.Entries.IndexOf(existingEntry);
            entry.Id = entryId;
            metadataFile.Entries[index] = entry;

            return await UpdateMediaMetadataFileAsync(metadataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    public async Task<MediaMetadataFile> RemoveMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var metadataFile = await GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                throw new InvalidOperationException("Media metadata file not found");
            }

            if (metadataFile.Entries == null)
            {
                metadataFile.Entries = new List<MediaMetadataEntry>();
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

    public async Task<MediaMetadataEntry?> GetMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var metadataFile = await GetMediaMetadataFileAsync();
            if (metadataFile == null || metadataFile.Entries == null)
            {
                return null;
            }

            return metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media metadata entry: {EntryId}", entryId);
            throw;
        }
    }

    public async Task<MediaMetadataFile> ImportMediaMetadataEntriesAsync(string jsonData, bool overwriteExisting = false)
    {
        try
        {
            List<MediaMetadataEntry> importedEntries;

            if (string.IsNullOrWhiteSpace(jsonData))
            {
                throw new ArgumentException("JSON data cannot be empty");
            }

            importedEntries = JsonSerializer.Deserialize<List<MediaMetadataEntry>>(jsonData)
                ?? new List<MediaMetadataEntry>();

            if (importedEntries.Count == 0)
            {
                throw new ArgumentException("No valid media metadata entries found in data");
            }

            var metadataFile = await GetMediaMetadataFileAsync();
            if (metadataFile == null)
            {
                metadataFile = new MediaMetadataFile
                {
                    Id = Guid.NewGuid().ToString(),
                    Entries = new List<MediaMetadataEntry>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };
            }

            if (metadataFile.Entries == null)
            {
                metadataFile.Entries = new List<MediaMetadataEntry>();
            }

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
}

