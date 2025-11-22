using Mystira.App.Admin.Api.Models;

namespace Mystira.App.Admin.Api.Repositories;

/// <summary>
/// Repository interface for CharacterMediaMetadataFile singleton entity
/// </summary>
public interface ICharacterMediaMetadataFileRepository
{
    Task<CharacterMediaMetadataFile?> GetAsync();
    Task<CharacterMediaMetadataFile> AddOrUpdateAsync(CharacterMediaMetadataFile entity);
    Task DeleteAsync();
}

