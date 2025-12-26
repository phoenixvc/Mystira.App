using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation following the Repository pattern
/// Supports both basic CRUD operations and specification-based queries
/// </summary>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(string id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual Task UpdateAsync(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task<bool> ExistsAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        return entity != null;
    }

    // Specification pattern operations

    public virtual async Task<TEntity?> GetBySpecAsync(ISpecification<TEntity> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> ListAsync(ISpecification<TEntity> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public virtual async Task<int> CountAsync(ISpecification<TEntity> spec)
    {
        return await ApplySpecification(spec).CountAsync();
    }

    private IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec)
    {
        return SpecificationEvaluator.Default.GetQuery(_dbSet.AsQueryable(), spec);
    }
}

