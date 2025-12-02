using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;

namespace Mystira.App.Admin.Api.Services;

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
        _logger.LogInformation("Retrieving all compass axes");
        return await _dbContext.CompassAxes
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<CompassAxis?> GetCompassAxisByIdAsync(string id)
    {
        _logger.LogInformation("Retrieving compass axis with id: {Id}", id);
        return await _dbContext.CompassAxes.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CompassAxis> CreateCompassAxisAsync(CompassAxis axis)
    {
        _logger.LogInformation("Creating compass axis: {Name}", axis.Name);
        
        if (string.IsNullOrWhiteSpace(axis.Id))
        {
            axis.Id = Guid.NewGuid().ToString();
        }

        axis.CreatedAt = DateTime.UtcNow;
        axis.UpdatedAt = DateTime.UtcNow;

        _dbContext.CompassAxes.Add(axis);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully created compass axis with id: {Id}", axis.Id);
        return axis;
    }

    public async Task<CompassAxis?> UpdateCompassAxisAsync(string id, CompassAxis axis)
    {
        _logger.LogInformation("Updating compass axis with id: {Id}", id);
        
        var existingAxis = await _dbContext.CompassAxes.FirstOrDefaultAsync(x => x.Id == id);
        if (existingAxis == null)
        {
            _logger.LogWarning("Compass axis with id {Id} not found", id);
            return null;
        }

        existingAxis.Name = axis.Name;
        existingAxis.Description = axis.Description;
        existingAxis.UpdatedAt = DateTime.UtcNow;

        _dbContext.CompassAxes.Update(existingAxis);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully updated compass axis with id: {Id}", id);
        return existingAxis;
    }

    public async Task<bool> DeleteCompassAxisAsync(string id)
    {
        _logger.LogInformation("Deleting compass axis with id: {Id}", id);
        
        var axis = await _dbContext.CompassAxes.FirstOrDefaultAsync(x => x.Id == id);
        if (axis == null)
        {
            _logger.LogWarning("Compass axis with id {Id} not found", id);
            return false;
        }

        _dbContext.CompassAxes.Remove(axis);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted compass axis with id: {Id}", id);
        return true;
    }
}
