using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for AvatarConfigurationFile singleton entity
/// </summary>
public class AvatarConfigurationFileRepository : IAvatarConfigurationFileRepository
{
    private readonly DbContext _context;
    private readonly DbSet<AvatarConfigurationFile> _dbSet;

    public AvatarConfigurationFileRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<AvatarConfigurationFile>();
    }

    public async Task<AvatarConfigurationFile?> GetAsync()
    {
        return await _dbSet.FirstOrDefaultAsync();
    }

    public async Task<AvatarConfigurationFile> AddOrUpdateAsync(AvatarConfigurationFile entity)
    {
        var existing = await GetAsync();
        if (existing != null)
        {
            existing.AgeGroupAvatars = entity.AgeGroupAvatars;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Version = entity.Version;
            _dbSet.Update(existing);
            return existing;
        }

        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task DeleteAsync()
    {
        var existing = await GetAsync();
        if (existing != null)
        {
            _dbSet.Remove(existing);
        }
    }
}

