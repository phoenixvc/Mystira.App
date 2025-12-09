using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class FantasyThemeRepository : IFantasyThemeRepository
{
    private readonly MystiraAppDbContext _context;

    public FantasyThemeRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<FantasyThemeDefinition>> GetAllAsync()
    {
        return await _context.FantasyThemeDefinitions
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<FantasyThemeDefinition?> GetByIdAsync(string id)
    {
        return await _context.FantasyThemeDefinitions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<FantasyThemeDefinition?> GetByNameAsync(string name)
    {
        return await _context.FantasyThemeDefinitions.FirstOrDefaultAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.FantasyThemeDefinitions.AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
    }

    public async Task AddAsync(FantasyThemeDefinition fantasyTheme)
    {
        await _context.FantasyThemeDefinitions.AddAsync(fantasyTheme);
    }

    public Task UpdateAsync(FantasyThemeDefinition fantasyTheme)
    {
        _context.FantasyThemeDefinitions.Update(fantasyTheme);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var fantasyTheme = await _context.FantasyThemeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (fantasyTheme != null)
        {
            // Soft delete instead of hard delete
            fantasyTheme.IsDeleted = true;
            fantasyTheme.UpdatedAt = DateTime.UtcNow;
            _context.FantasyThemeDefinitions.Update(fantasyTheme);
        }
    }
}
