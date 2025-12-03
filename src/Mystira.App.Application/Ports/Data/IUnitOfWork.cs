namespace Mystira.App.Application.Ports.Data;

/// <summary>
/// Unit of Work pattern interface for managing transactions and repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
