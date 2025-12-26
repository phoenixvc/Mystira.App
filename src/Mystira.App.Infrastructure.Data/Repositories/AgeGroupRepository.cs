using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class AgeGroupRepository : IAgeGroupRepository
{
    private readonly MystiraAppDbContext _context;

    public AgeGroupRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AgeGroupDefinition>> GetAllAsync()
    {
        return await _context.AgeGroupDefinitions
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.MinimumAge)
            .ToListAsync();
    }

    public async Task<AgeGroupDefinition?> GetByIdAsync(string id)
    {
        return await _context.AgeGroupDefinitions
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<AgeGroupDefinition?> GetByNameAsync(string name)
    {
        return await _context.AgeGroupDefinitions.FirstOrDefaultAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
    }

    public async Task<AgeGroupDefinition?> GetByValueAsync(string value)
    {
        return await _context.AgeGroupDefinitions.FirstOrDefaultAsync(x => x.Value == value && !x.IsDeleted);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.AgeGroupDefinitions.AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
    }

    public async Task<bool> ExistsByValueAsync(string value)
    {
        return await _context.AgeGroupDefinitions.AnyAsync(x => x.Value == value && !x.IsDeleted);
    }

    public async Task AddAsync(AgeGroupDefinition ageGroup)
    {
        await _context.AgeGroupDefinitions.AddAsync(ageGroup);
    }

    public Task UpdateAsync(AgeGroupDefinition ageGroup)
    {
        _context.AgeGroupDefinitions.Update(ageGroup);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var ageGroup = await _context.AgeGroupDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (ageGroup != null)
        {
            // Soft delete instead of hard delete
            ageGroup.IsDeleted = true;
            ageGroup.UpdatedAt = DateTime.UtcNow;
            _context.AgeGroupDefinitions.Update(ageGroup);
        }
    }
}
