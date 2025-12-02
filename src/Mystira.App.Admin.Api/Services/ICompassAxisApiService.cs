using Mystira.App.Domain.Models;

namespace Mystira.App.Admin.Api.Services;

public interface ICompassAxisApiService
{
    Task<List<CompassAxis>> GetAllCompassAxesAsync();
    Task<CompassAxis?> GetCompassAxisByIdAsync(string id);
    Task<CompassAxis> CreateCompassAxisAsync(CompassAxis axis);
    Task<CompassAxis?> UpdateCompassAxisAsync(string id, CompassAxis axis);
    Task<bool> DeleteCompassAxisAsync(string id);
}
