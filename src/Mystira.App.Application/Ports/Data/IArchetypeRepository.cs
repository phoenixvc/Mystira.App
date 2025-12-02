using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface IArchetypeRepository
{
    Task<List<ArchetypeDefinition>> GetAllAsync();
    Task<ArchetypeDefinition?> GetByIdAsync(string id);
    Task<ArchetypeDefinition?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
}
