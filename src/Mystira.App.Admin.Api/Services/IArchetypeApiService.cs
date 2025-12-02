using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

public interface IArchetypeApiService
{
    Task<List<ArchetypeDefinition>> GetAllArchetypesAsync();
    Task<ArchetypeDefinition?> GetArchetypeByIdAsync(string id);
    Task<ArchetypeDefinition> CreateArchetypeAsync(ArchetypeDefinition archetype);
    Task<ArchetypeDefinition?> UpdateArchetypeAsync(string id, ArchetypeDefinition archetype);
    Task<bool> DeleteArchetypeAsync(string id);
}
