using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;
using Mystira.App.Infrastructure.Data.Specifications;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class BadgeRepository : IBadgeRepository
{
    private readonly MystiraAppDbContext _context;

    public BadgeRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<Badge?> GetByIdAsync(string id)
    {
        return await _context.Badges.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<Badge>> GetAllAsync()
    {
        return await _context.Badges.ToListAsync();
    }

    public async Task<IEnumerable<Badge>> FindAsync(System.Linq.Expressions.Expression<Func<Badge, bool>> predicate)
    {
        return await _context.Badges.Where(predicate).ToListAsync();
    }

    public async Task<Badge> AddAsync(Badge entity)
    {
        await _context.Badges.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(Badge entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Badges.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.Badges.Remove(entity);
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.Badges.AnyAsync(x => x.Id == id);
    }

    public async Task<Badge?> GetBySpecAsync(ISpecification<Badge> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Badge>> ListAsync(ISpecification<Badge> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<int> CountAsync(ISpecification<Badge> spec)
    {
        return await ApplySpecification(spec).CountAsync();
    }

    public async Task<IEnumerable<Badge>> GetByAgeGroupAsync(string ageGroupId)
    {
        return await _context.Badges
            .Where(x => x.AgeGroupId == ageGroupId)
            .OrderBy(x => x.Tier)
            .ThenBy(x => x.TierOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Badge>> GetByCompassAxisAsync(string compassAxisId)
    {
        return await _context.Badges
            .Where(x => x.CompassAxisId == compassAxisId)
            .OrderBy(x => x.AgeGroupId)
            .ThenBy(x => x.Tier)
            .ThenBy(x => x.TierOrder)
            .ToListAsync();
    }

    public async Task<Badge?> GetByAgeGroupAxisAndTierAsync(string ageGroupId, string compassAxisId, int tierOrder)
    {
        return await _context.Badges
            .FirstOrDefaultAsync(x => x.AgeGroupId == ageGroupId 
                                   && x.CompassAxisId == compassAxisId 
                                   && x.TierOrder == tierOrder);
    }

    private IQueryable<Badge> ApplySpecification(ISpecification<Badge> spec)
    {
        return SpecificationEvaluator<Badge>.GetQuery(_context.Badges.AsQueryable(), spec);
    }
}
