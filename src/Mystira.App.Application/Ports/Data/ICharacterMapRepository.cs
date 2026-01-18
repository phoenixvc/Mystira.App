using Ardalis.Specification;
using Mystira.App.Domain.Models;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Repository interface for CharacterMap entity with domain-specific queries
/// </summary>
public interface ICharacterMapRepository : IRepository<CharacterMap, string>, IRepositoryBase<CharacterMap>
{
    Task<CharacterMap?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
}

