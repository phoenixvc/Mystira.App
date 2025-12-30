using Mystira.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface IFantasyThemeRepository
{
    Task<List<FantasyThemeDefinition>> GetAllAsync();
    Task<FantasyThemeDefinition?> GetByIdAsync(string id);
    Task<FantasyThemeDefinition?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
    Task AddAsync(FantasyThemeDefinition fantasyTheme);
    Task UpdateAsync(FantasyThemeDefinition fantasyTheme);
    Task DeleteAsync(string id);
}
