using Mystira.App.Api.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for MediaMetadataFile singleton entity
/// </summary>
public interface IMediaMetadataFileRepository
{
    Task<MediaMetadataFile?> GetAsync();
    Task<MediaMetadataFile> AddOrUpdateAsync(MediaMetadataFile entity);
    Task DeleteAsync();
}

