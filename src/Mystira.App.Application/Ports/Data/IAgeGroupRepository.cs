using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface IAgeGroupRepository
{
    Task<List<AgeGroupDefinition>> GetAllAsync();
    Task<AgeGroupDefinition?> GetByIdAsync(string id);
    Task<AgeGroupDefinition?> GetByNameAsync(string name);
    Task<AgeGroupDefinition?> GetByValueAsync(string value);
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsByValueAsync(string value);
    Task AddAsync(AgeGroupDefinition ageGroup);
    Task UpdateAsync(AgeGroupDefinition ageGroup);
    Task DeleteAsync(string id);
}
