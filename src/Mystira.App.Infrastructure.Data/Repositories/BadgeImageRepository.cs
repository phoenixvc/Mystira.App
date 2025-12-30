using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.Domain.Models;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class BadgeImageRepository : IBadgeImageRepository
{
    private readonly MystiraAppDbContext _context;

    public BadgeImageRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<BadgeImage?> GetByIdAsync(string id)
    {
        return await _context.BadgeImages.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<BadgeImage>> GetAllAsync()
    {
        return await _context.BadgeImages.ToListAsync();
    }

    public async Task<IEnumerable<BadgeImage>> FindAsync(System.Linq.Expressions.Expression<Func<BadgeImage, bool>> predicate)
    {
        return await _context.BadgeImages.Where(predicate).ToListAsync();
    }

    public async Task<BadgeImage> AddAsync(BadgeImage entity)
    {
        await _context.BadgeImages.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(BadgeImage entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.BadgeImages.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.BadgeImages.Remove(entity);
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.BadgeImages.AnyAsync(x => x.Id == id);
    }

    public async Task<BadgeImage?> GetBySpecAsync(ISpecification<BadgeImage> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BadgeImage>> ListAsync(ISpecification<BadgeImage> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<int> CountAsync(ISpecification<BadgeImage> spec)
    {
        return await ApplySpecification(spec).CountAsync();
    }

    public async Task<BadgeImage?> GetByImageIdAsync(string imageId)
    {
        return await _context.BadgeImages
            .FirstOrDefaultAsync(x => x.ImageId == imageId);
    }

    private IQueryable<BadgeImage> ApplySpecification(ISpecification<BadgeImage> spec)
    {
        return SpecificationEvaluator.Default.GetQuery(_context.BadgeImages.AsQueryable(), spec);
    }
}
