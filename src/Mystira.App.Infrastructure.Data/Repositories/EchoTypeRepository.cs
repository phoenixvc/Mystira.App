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
        return await _context.EchoTypeDefinitions
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<EchoTypeDefinition?> GetByIdAsync(string id)
    {
        return await _context.EchoTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<EchoTypeDefinition?> GetByNameAsync(string name)
    {
        return await _context.EchoTypeDefinitions.FirstOrDefaultAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.EchoTypeDefinitions.AnyAsync(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
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
            // Soft delete instead of hard delete
            echoType.IsDeleted = true;
            echoType.UpdatedAt = DateTime.UtcNow;
            _context.EchoTypeDefinitions.Update(echoType);
        }
    }
}
