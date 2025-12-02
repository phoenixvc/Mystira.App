using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Models;
using Mystira.App.Infrastructure.Data;

namespace Mystira.App.Api.Services;

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
        _logger.LogInformation("Retrieving all archetypes for validation");
        return await _dbContext.ArchetypeDefinitions
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<ArchetypeDefinition?> GetArchetypeByIdAsync(string id)
    {
        _logger.LogInformation("Retrieving archetype with id: {Id}", id);
        return await _dbContext.ArchetypeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> IsValidArchetypeAsync(string name)
    {
        _logger.LogInformation("Validating archetype: {Name}", name);
        var exists = await _dbContext.ArchetypeDefinitions
            .AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return exists;
    }
}
