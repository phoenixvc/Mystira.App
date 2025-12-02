using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Services;

public interface ICompassAxisApiService
{
    Task<List<CompassAxis>> GetAllCompassAxesAsync();
    Task<CompassAxis?> GetCompassAxisByIdAsync(string id);
    Task<bool> IsValidCompassAxisAsync(string name);
}
