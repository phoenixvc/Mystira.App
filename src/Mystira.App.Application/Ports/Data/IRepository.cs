using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Generic repository interface following the Repository pattern
/// Supports both basic operations and specification-based queries
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    // Basic CRUD operations
    Task<TEntity?> GetByIdAsync(string id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);

    // Specification pattern operations
    Task<TEntity?> GetBySpecAsync(ISpecification<TEntity> spec);
    Task<IEnumerable<TEntity>> ListAsync(ISpecification<TEntity> spec);
    Task<int> CountAsync(ISpecification<TEntity> spec);
}

