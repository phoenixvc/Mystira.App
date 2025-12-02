using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class CompassAxisRepository : ICompassAxisRepository
{
    private readonly MystiraAppDbContext _context;

    public CompassAxisRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompassAxis>> GetAllAsync()
    {
        return await _context.CompassAxes.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<CompassAxis?> GetByIdAsync(string id)
    {
        return await _context.CompassAxes.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CompassAxis?> GetByNameAsync(string name)
    {
        return await _context.CompassAxes.FirstOrDefaultAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.CompassAxes.AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(CompassAxis axis)
    {
        await _context.CompassAxes.AddAsync(axis);
    }

    public Task UpdateAsync(CompassAxis axis)
    {
        _context.CompassAxes.Update(axis);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var axis = await _context.CompassAxes.FirstOrDefaultAsync(x => x.Id == id);
        if (axis != null)
        {
            _context.CompassAxes.Remove(axis);
        }
    }
}
