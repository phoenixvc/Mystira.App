using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface IEchoTypeRepository
{
    Task<List<EchoTypeDefinition>> GetAllAsync();
    Task<EchoTypeDefinition?> GetByIdAsync(string id);
    Task<EchoTypeDefinition?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
    Task AddAsync(EchoTypeDefinition echoType);
    Task UpdateAsync(EchoTypeDefinition echoType);
    Task DeleteAsync(string id);
}
