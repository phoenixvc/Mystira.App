using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mystira.Shared.Data.Repositories;

namespace Mystira.App.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation following the Repository pattern.
/// Implements both Mystira.Shared.Data.Repositories.IRepository&lt;T&gt; for shared infrastructure compatibility
/// and Ardalis.Specification.IRepositoryBase&lt;T&gt; for specification pattern and caching decorator support.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity>, IRepositoryBase<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
    }

    #region IRepository<T> Implementation (Mystira.Shared)

    public virtual async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        return entity != null;
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<TEntity?> GetBySpecAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async IAsyncEnumerable<TEntity> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var entity in _dbSet.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return entity;
        }
    }

    public virtual async IAsyncEnumerable<TEntity> StreamAsync(ISpecification<TEntity> spec, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var entity in ApplySpecification(spec).AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return entity;
        }
    }

    // Explicit interface implementations for IRepository<T> where signatures differ from IRepositoryBase<T>

    /// <summary>
    /// Explicit implementation for IRepository&lt;T&gt;.ListAsync which returns IEnumerable&lt;T&gt;
    /// </summary>
    async Task<IEnumerable<TEntity>> IRepository<TEntity>.ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken)
    {
        return await ListAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Explicit implementation for IRepository&lt;T&gt;.AddRangeAsync which returns Task (void)
    /// </summary>
    async Task IRepository<TEntity>.AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        await AddRangeAsync(entities, cancellationToken);
    }

    #endregion

    #region IRepositoryBase<T> Implementation (Ardalis.Specification)

    public virtual async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        return await _dbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> SingleOrDefaultAsync(ISingleResultSpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification((ISpecification<TEntity>)specification).SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<TEntity, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification((ISpecification<TEntity, TResult>)specification).SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TResult>> ListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).AnyAsync(cancellationToken);
    }

    public virtual IAsyncEnumerable<TEntity> AsAsyncEnumerable(ISpecification<TEntity> specification)
    {
        return ApplySpecification(specification).AsAsyncEnumerable();
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        await _dbSet.AddRangeAsync(entityList, cancellationToken);
        return entityList;
    }

    public virtual Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Update(entity);
        return Task.FromResult(1);
    }

    public virtual Task<int> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        _dbSet.UpdateRange(entityList);
        return Task.FromResult(entityList.Count);
    }

    public virtual Task<int> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Remove(entity);
        return Task.FromResult(1);
    }

    public virtual Task<int> DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var entityList = entities.ToList();
        _dbSet.RemoveRange(entityList);
        return Task.FromResult(entityList.Count);
    }

    public virtual async Task<int> DeleteRangeAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        var entities = await ListAsync(specification, cancellationToken);
        _dbSet.RemoveRange(entities);
        return entities.Count;
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Helper Methods

    protected IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec)
    {
        return SpecificationEvaluator.Default.GetQuery(_dbSet.AsQueryable(), spec);
    }

    protected IQueryable<TResult> ApplySpecification<TResult>(ISpecification<TEntity, TResult> spec)
    {
        return SpecificationEvaluator.Default.GetQuery(_dbSet.AsQueryable(), spec);
    }

    #endregion
}
