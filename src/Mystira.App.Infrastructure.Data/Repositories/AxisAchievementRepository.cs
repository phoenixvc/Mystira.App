using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class AxisAchievementRepository : IAxisAchievementRepository
{
    private readonly MystiraAppDbContext _context;

    public AxisAchievementRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<AxisAchievement?> GetByIdAsync(string id)
    {
        return await _context.AxisAchievements.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<AxisAchievement>> GetAllAsync()
    {
        return await _context.AxisAchievements.ToListAsync();
    }

    public async Task<IEnumerable<AxisAchievement>> FindAsync(System.Linq.Expressions.Expression<Func<AxisAchievement, bool>> predicate)
    {
        return await _context.AxisAchievements.Where(predicate).ToListAsync();
    }

    public async Task<AxisAchievement> AddAsync(AxisAchievement entity)
    {
        await _context.AxisAchievements.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(AxisAchievement entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.AxisAchievements.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.AxisAchievements.Remove(entity);
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.AxisAchievements.AnyAsync(x => x.Id == id);
    }

    public async Task<AxisAchievement?> GetBySpecAsync(ISpecification<AxisAchievement> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AxisAchievement>> ListAsync(ISpecification<AxisAchievement> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<int> CountAsync(ISpecification<AxisAchievement> spec)
    {
        return await ApplySpecification(spec).CountAsync();
    }

    public async Task<IEnumerable<AxisAchievement>> GetByAgeGroupAsync(string ageGroupId)
    {
        return await _context.AxisAchievements
            .Where(x => x.AgeGroupId == ageGroupId)
            .ToListAsync();
    }

    public async Task<IEnumerable<AxisAchievement>> GetByCompassAxisAsync(string compassAxisId)
    {
        return await _context.AxisAchievements
            .Where(x => x.CompassAxisId == compassAxisId)
            .ToListAsync();
    }

    private IQueryable<AxisAchievement> ApplySpecification(ISpecification<AxisAchievement> spec)
    {
        return SpecificationEvaluator.Default.GetQuery(_context.AxisAchievements.AsQueryable(), spec);
    }
}
