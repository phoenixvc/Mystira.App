using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Repositories;

/// <summary>
/// Repository interface for MediaMetadataFile singleton entity
/// </summary>
public interface IMediaMetadataFileRepository
{
    Task<MediaMetadataFile?> GetAsync();
    Task<MediaMetadataFile> AddOrUpdateAsync(MediaMetadataFile entity);
    Task DeleteAsync();
}

