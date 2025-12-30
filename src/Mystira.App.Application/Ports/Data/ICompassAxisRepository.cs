using Mystira.Domain.Models;

namespace Mystira.App.Application.Ports.Data;

public interface ICompassAxisRepository
{
    Task<List<CompassAxis>> GetAllAsync();
    Task<CompassAxis?> GetByIdAsync(string id);
    Task<CompassAxis?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
    Task AddAsync(CompassAxis axis);
    Task UpdateAsync(CompassAxis axis);
    Task DeleteAsync(string id);
}
