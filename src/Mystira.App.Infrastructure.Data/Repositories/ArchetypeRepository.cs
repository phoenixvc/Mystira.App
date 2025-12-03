using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class ArchetypeRepository : IArchetypeRepository
{
    private readonly MystiraAppDbContext _context;

    public ArchetypeRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ArchetypeDefinition>> GetAllAsync()
    {
        return await _context.ArchetypeDefinitions
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<ArchetypeDefinition?> GetByIdAsync(string id)
    {
        return await _context.ArchetypeDefinitions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<ArchetypeDefinition?> GetByNameAsync(string name)
    {
        return await _context.ArchetypeDefinitions.FirstOrDefaultAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.ArchetypeDefinitions.AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
    }

    public async Task AddAsync(ArchetypeDefinition archetype)
    {
        await _context.ArchetypeDefinitions.AddAsync(archetype);
    }

    public Task UpdateAsync(ArchetypeDefinition archetype)
    {
        _context.ArchetypeDefinitions.Update(archetype);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var archetype = await _context.ArchetypeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (archetype != null)
        {
            // Soft delete instead of hard delete
            archetype.IsDeleted = true;
            archetype.UpdatedAt = DateTime.UtcNow;
            _context.ArchetypeDefinitions.Update(archetype);
        }
    }
}
