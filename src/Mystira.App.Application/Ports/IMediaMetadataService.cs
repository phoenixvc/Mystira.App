using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports;

/// <summary>
/// Port interface for media metadata service (file-based metadata management)
/// Uses Domain models as per Hexagonal Architecture - Application layer depends on Domain, not Contracts
/// </summary>
public interface IMediaMetadataService
{
    Task<MediaMetadataFile?> GetMediaMetadataFileAsync();
    Task<MediaMetadataFile> UpdateMediaMetadataFileAsync(MediaMetadataFile metadataFile);
    Task<MediaMetadataFile> AddMediaMetadataEntryAsync(MediaMetadataEntry entry);
    Task<MediaMetadataFile> UpdateMediaMetadataEntryAsync(string entryId, MediaMetadataEntry entry);
    Task<MediaMetadataFile> RemoveMediaMetadataEntryAsync(string entryId);
    Task<MediaMetadataEntry?> GetMediaMetadataEntryAsync(string entryId);
    Task<MediaMetadataFile> ImportMediaMetadataEntriesAsync(string jsonData, bool overwriteExisting = false);
}

