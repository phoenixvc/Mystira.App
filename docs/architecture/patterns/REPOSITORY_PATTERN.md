# Repository Pattern

## Overview

The Repository Pattern provides an abstraction layer between the business logic and data access layers. It encapsulates the logic needed to access data sources and provides a more object-oriented view of the persistence layer.

## Implementation in Mystira.App

### Location
- **Interfaces**: `src/Mystira.App.Infrastructure.Data/Repositories/`
- **Implementations**: `src/Mystira.App.Infrastructure.Data/Repositories/`

### Structure

```
Infrastructure.Data/
├── Repositories/
│   ├── IRepository<TEntity>.cs          # Generic repository interface
│   ├── Repository<TEntity>.cs           # Generic repository implementation
│   ├── IGameSessionRepository.cs        # Domain-specific repository interface
│   ├── GameSessionRepository.cs         # Domain-specific repository implementation
│   ├── IUserProfileRepository.cs
│   ├── UserProfileRepository.cs
│   ├── IAccountRepository.cs
│   └── AccountRepository.cs
└── UnitOfWork/
    ├── IUnitOfWork.cs
    └── UnitOfWork.cs
```

## Benefits

1. **Separation of Concerns**: Business logic is decoupled from data access
2. **Testability**: Easy to mock repositories for unit testing
3. **Maintainability**: Changes to data access logic are centralized
4. **Flexibility**: Can swap data sources without changing business logic

## Usage Example

```csharp
public class GameSessionApiService
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public GameSessionApiService(
        IGameSessionRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GameSession?> GetSessionAsync(string sessionId)
    {
        return await _repository.GetByIdAsync(sessionId);
    }

    public async Task<GameSession> CreateSessionAsync(GameSession session)
    {
        await _repository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();
        return session;
    }
}
```

## Generic Repository Interface

```csharp
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(string id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}
```

## Domain-Specific Repositories

Domain-specific repositories extend the generic repository with queries specific to the entity:

```csharp
public interface IGameSessionRepository : IRepository<GameSession>
{
    Task<IEnumerable<GameSession>> GetByAccountIdAsync(string accountId);
    Task<IEnumerable<GameSession>> GetInProgressSessionsAsync(string accountId);
    Task<GameSession?> GetActiveSessionForScenarioAsync(string accountId, string scenarioId);
}
```

## Unit of Work Pattern

The Unit of Work pattern coordinates changes across multiple repositories and ensures transactional consistency:

```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

## Best Practices

1. **One repository per aggregate root**: Create repositories for entities that are aggregate roots
2. **Domain-specific queries**: Add methods to repository interfaces for common queries
3. **Use UnitOfWork for transactions**: Coordinate multiple repository operations through UnitOfWork
4. **Keep repositories focused**: Don't add business logic to repositories
5. **Return domain entities**: Repositories should return domain models, not DTOs

## Migration Strategy

When migrating existing services to use repositories:

1. Create repository interface with domain-specific queries
2. Implement repository using DbContext
3. Register repository in DI container
4. Update service to inject repository instead of DbContext
5. Replace direct DbContext calls with repository methods
6. Use UnitOfWork for SaveChanges operations

## References

- [Repository Pattern - Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Unit of Work Pattern - Martin Fowler](https://martinfowler.com/eaaCatalog/unitOfWork.html)

