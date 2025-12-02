using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;

namespace Mystira.App.Api.Services;

public class CompassAxisApiService : ICompassAxisApiService
{
    private readonly MystiraAppDbContext _dbContext;
    private readonly ILogger<CompassAxisApiService> _logger;

    public CompassAxisApiService(MystiraAppDbContext dbContext, ILogger<CompassAxisApiService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<CompassAxis>> GetAllCompassAxesAsync()
    {
        _logger.LogInformation("Retrieving all compass axes for validation");
        return await _dbContext.CompassAxes
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<CompassAxis?> GetCompassAxisByIdAsync(string id)
    {
        _logger.LogInformation("Retrieving compass axis with id: {Id}", id);
        return await _dbContext.CompassAxes.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> IsValidCompassAxisAsync(string name)
    {
        _logger.LogInformation("Validating compass axis: {Name}", name);
        var exists = await _dbContext.CompassAxes
            .AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return exists;
    }
}
