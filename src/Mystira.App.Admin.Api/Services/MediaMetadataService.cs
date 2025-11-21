using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Admin.Api.Data;
using Mystira.App.Admin.Api.Models;
using YamlDotNet.Serialization;

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
    public async Task<MediaMetadataFile?> GetMediaMetadataFileAsync()
    {
        try
        {
            // Attempt with normal EF Core approach first
            try
            {
                var metadataFile = await _context.MediaMetadataFiles.FirstOrDefaultAsync();
                if (metadataFile != null)
                {
                    // Ensure Entries is initialized
                    if (metadataFile.Entries == null)
                    {
                        metadataFile.Entries = new List<MediaMetadataEntry>();
                    }
                    return metadataFile;
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
    public async Task<MediaMetadataFile> UpdateMediaMetadataFileAsync(MediaMetadataFile metadataFile)
    {
        try
        {
            metadataFile.UpdatedAt = DateTime.UtcNow;

            var existingFile = await _context.MediaMetadataFiles.FirstOrDefaultAsync();
            if (existingFile != null)
            {
                _context.Entry(existingFile).CurrentValues.SetValues(metadataFile);
                existingFile.Entries = metadataFile.Entries;
            }
            else
            {
                await _context.MediaMetadataFiles.AddAsync(metadataFile);
            }

            await _context.SaveChangesAsync();
            return metadataFile;
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
    public async Task<MediaMetadataFile> AddMediaMetadataEntryAsync(MediaMetadataEntry entry)
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
            metadataFile ??= new MediaMetadataFile();
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
    public async Task<MediaMetadataFile> UpdateMediaMetadataEntryAsync(string entryId, MediaMetadataEntry entry)
    {
        try
        {
            var metadataFile = await GetMediaMetadataFileAsync();

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
    public async Task<MediaMetadataFile> RemoveMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var metadataFile = await GetMediaMetadataFileAsync();

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
    public async Task<MediaMetadataEntry?> GetMediaMetadataEntryAsync(string entryId)
    {
        try
        {
            var metadataFile = await GetMediaMetadataFileAsync();
            return metadataFile.Entries.FirstOrDefault(e => e.Id == entryId);
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
    public async Task<MediaMetadataFile> ImportMediaMetadataEntriesAsync(string data, bool overwriteExisting = false)
    {
        try
        {
            List<MediaMetadataEntry> importedEntries;

            // Try to determine if data is JSON or YAML
            if (data.TrimStart().StartsWith('[') || data.TrimStart().StartsWith('{'))
            {
                // JSON format
                importedEntries = JsonSerializer.Deserialize<List<MediaMetadataEntry>>(data);
            }
            else
            {
                // YAML format
                var deserializer = new DeserializerBuilder().Build();
                importedEntries = deserializer.Deserialize<List<MediaMetadataEntry>>(data);
            }

            if (importedEntries == null || importedEntries.Count == 0)
            {
                throw new ArgumentException("No valid media metadata entries found in data");
            }

            var metadataFile = await GetMediaMetadataFileAsync() ?? new MediaMetadataFile();

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
