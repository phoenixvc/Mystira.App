using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for CharacterMediaMetadataFile singleton entity
/// </summary>
public interface ICharacterMediaMetadataFileRepository
{
    Task<CharacterMediaMetadataFile?> GetAsync();
    Task<CharacterMediaMetadataFile> AddOrUpdateAsync(CharacterMediaMetadataFile entity);
    Task DeleteAsync();
}

