using Mystira.App.Api.Models;

namespace Mystira.App.Api.Repositories;

/// <summary>
/// Repository interface for CharacterMediaMetadataFile singleton entity
/// </summary>
public interface ICharacterMediaMetadataFileRepository
{
    Task<CharacterMediaMetadataFile?> GetAsync();
    Task<CharacterMediaMetadataFile> AddOrUpdateAsync(CharacterMediaMetadataFile entity);
    Task DeleteAsync();
}

