using Mystira.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for MediaMetadataFile singleton entity
/// </summary>
public interface IMediaMetadataFileRepository
{
    Task<MediaMetadataFile?> GetAsync();
    Task<MediaMetadataFile> AddOrUpdateAsync(MediaMetadataFile entity);
    Task DeleteAsync();
}

