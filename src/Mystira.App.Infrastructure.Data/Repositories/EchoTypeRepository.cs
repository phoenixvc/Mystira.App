using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class EchoTypeRepository : IEchoTypeRepository
{
    private readonly MystiraAppDbContext _context;

    public EchoTypeRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<EchoTypeDefinition>> GetAllAsync()
    {
        return await _context.EchoTypeDefinitions.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<EchoTypeDefinition?> GetByIdAsync(string id)
    {
        return await _context.EchoTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<EchoTypeDefinition?> GetByNameAsync(string name)
    {
        return await _context.EchoTypeDefinitions.FirstOrDefaultAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.EchoTypeDefinitions.AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(EchoTypeDefinition echoType)
    {
        await _context.EchoTypeDefinitions.AddAsync(echoType);
    }

    public Task UpdateAsync(EchoTypeDefinition echoType)
    {
        _context.EchoTypeDefinitions.Update(echoType);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var echoType = await _context.EchoTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (echoType != null)
        {
            _context.EchoTypeDefinitions.Remove(echoType);
        }
    }
}
