using Ardalis.Specification;

namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Read-only repository interface using Ardalis.Specification.
/// Provides a clean separation between read and write operations.
///
/// This interface is designed to work with both the existing IRepository
/// and the new Ardalis.Specification-based queries.
///
/// Usage:
///   var spec = new AccountByEmailSpec(email);
///   var account = await _readRepository.FirstOrDefaultAsync(spec);
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class
{
    /// <summary>
    /// Get entity by its string ID.
    /// Convenience method for common ID-based lookups.
    /// </summary>
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an entity with the given ID exists.
    /// </summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Full repository interface with read and write operations using Ardalis.Specification.
/// Extends IReadRepository with mutation operations.
///
/// Usage:
///   await _repository.AddAsync(newAccount);
///   await _repository.SaveChangesAsync();
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface ISpecRepository<T> : IReadRepository<T>, IRepositoryBase<T> where T : class
{
}
