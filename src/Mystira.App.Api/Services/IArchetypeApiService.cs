using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public interface IArchetypeApiService
{
    Task<List<ArchetypeDefinition>> GetAllArchetypesAsync();
    Task<ArchetypeDefinition?> GetArchetypeByIdAsync(string id);
    Task<bool> IsValidArchetypeAsync(string name);
}
