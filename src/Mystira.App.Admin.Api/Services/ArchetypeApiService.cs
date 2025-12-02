using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;

namespace Mystira.App.Admin.Api.Services;

public class ArchetypeApiService : IArchetypeApiService
{
    private readonly MystiraAppDbContext _dbContext;
    private readonly ILogger<ArchetypeApiService> _logger;

    public ArchetypeApiService(MystiraAppDbContext dbContext, ILogger<ArchetypeApiService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ArchetypeDefinition>> GetAllArchetypesAsync()
    {
        _logger.LogInformation("Retrieving all archetypes");
        return await _dbContext.ArchetypeDefinitions
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<ArchetypeDefinition?> GetArchetypeByIdAsync(string id)
    {
        _logger.LogInformation("Retrieving archetype with id: {Id}", id);
        return await _dbContext.ArchetypeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ArchetypeDefinition> CreateArchetypeAsync(ArchetypeDefinition archetype)
    {
        _logger.LogInformation("Creating archetype: {Name}", archetype.Name);
        
        if (string.IsNullOrWhiteSpace(archetype.Id))
        {
            archetype.Id = Guid.NewGuid().ToString();
        }

        archetype.CreatedAt = DateTime.UtcNow;
        archetype.UpdatedAt = DateTime.UtcNow;

        _dbContext.ArchetypeDefinitions.Add(archetype);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully created archetype with id: {Id}", archetype.Id);
        return archetype;
    }

    public async Task<ArchetypeDefinition?> UpdateArchetypeAsync(string id, ArchetypeDefinition archetype)
    {
        _logger.LogInformation("Updating archetype with id: {Id}", id);
        
        var existingArchetype = await _dbContext.ArchetypeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (existingArchetype == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", id);
            return null;
        }

        existingArchetype.Name = archetype.Name;
        existingArchetype.Description = archetype.Description;
        existingArchetype.UpdatedAt = DateTime.UtcNow;

        _dbContext.ArchetypeDefinitions.Update(existingArchetype);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully updated archetype with id: {Id}", id);
        return existingArchetype;
    }

    public async Task<bool> DeleteArchetypeAsync(string id)
    {
        _logger.LogInformation("Deleting archetype with id: {Id}", id);
        
        var archetype = await _dbContext.ArchetypeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (archetype == null)
        {
            _logger.LogWarning("Archetype with id {Id} not found", id);
            return false;
        }

        _dbContext.ArchetypeDefinitions.Remove(archetype);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted archetype with id: {Id}", id);
        return true;
    }
}
