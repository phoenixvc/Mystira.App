using Mystira.App.Api.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for CharacterMapFile singleton entity
/// </summary>
public interface ICharacterMapFileRepository
{
    Task<CharacterMapFile?> GetAsync();
    Task<CharacterMapFile> AddOrUpdateAsync(CharacterMapFile entity);
    Task DeleteAsync();
}

