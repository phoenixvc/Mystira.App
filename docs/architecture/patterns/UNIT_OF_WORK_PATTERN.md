# Unit of Work Pattern

## Overview

The Unit of Work pattern maintains a list of objects affected by a business transaction and coordinates writing out changes and resolving concurrency problems. It ensures that all changes are committed together or rolled back together.

## Implementation in Mystira.App

### Location
- **Interface**: `src/Mystira.App.Infrastructure.Data/UnitOfWork/IUnitOfWork.cs`
- **Implementation**: `src/Mystira.App.Infrastructure.Data/UnitOfWork/UnitOfWork.cs`

## Purpose

1. **Transaction Management**: Ensures all repository changes are committed atomically
2. **Change Tracking**: Coordinates changes across multiple repositories
3. **Consistency**: Maintains data consistency across related operations

## Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

## Usage Example

```csharp
public class GameSessionApiService
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<GameSession> StartSessionAsync(StartGameSessionRequest request)
    {
        // Multiple repository operations
        await _repository.AddAsync(session);
        await _repository.UpdateAsync(existingSession);
        
        // Single commit point
        await _unitOfWork.SaveChangesAsync();
        
        return session;
    }
}
```

## Transaction Example

```csharp
public async Task TransferDataAsync(string fromId, string toId)
{
    await _unitOfWork.BeginTransactionAsync();
    try
    {
        var fromEntity = await _repository.GetByIdAsync(fromId);
        var toEntity = await _repository.GetByIdAsync(toId);
        
        // Make changes
        await _repository.UpdateAsync(fromEntity);
        await _repository.UpdateAsync(toEntity);
        
        await _unitOfWork.CommitTransactionAsync();
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

## Benefits

1. **Atomicity**: All changes succeed or fail together
2. **Consistency**: Maintains data integrity
3. **Performance**: Single database round-trip for multiple changes
4. **Error Handling**: Automatic rollback on errors

## Best Practices

1. **One UnitOfWork per request**: Typically scoped per HTTP request
2. **Use transactions for multiple operations**: When coordinating changes across repositories
3. **Handle exceptions**: Always rollback transactions on errors
4. **Dispose properly**: UnitOfWork implements IDisposable for cleanup

## Registration

```csharp
// In Program.cs
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

## References

- [Unit of Work Pattern - Martin Fowler](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Unit of Work with Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#the-repository-pattern)

