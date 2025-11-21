using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository interface for CharacterMap entity with domain-specific queries
/// </summary>
public interface ICharacterMapRepository : IRepository<CharacterMap>
{
    Task<CharacterMap?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
}

