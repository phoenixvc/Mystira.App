# Repository Pattern

## Overview

The Repository Pattern provides an abstraction layer between the business logic and data access layers. It encapsulates the logic needed to access data sources and provides a more object-oriented view of the persistence layer.

**Key Benefits**:
- 🔒 **Abstraction** - Decouples domain logic from data access
- 🧪 **Testability** - Easy to mock for unit testing
- 🔄 **Flexibility** - Can swap data sources without changing business logic
- 📝 **CQRS Support** - Works seamlessly with Command/Query handlers
- 🔍 **Specification Support** - Integrates with [Specification Pattern](SPECIFICATION_PATTERN.md)

---

## Implementation in Mystira.App

### Location

- **Port Interfaces**: `src/Mystira.App.Application/Ports/Data/` (Application layer)
- **Implementations**: `src/Mystira.App.Infrastructure.Data/Repositories/` (Infrastructure layer)

### Structure

```
Application/
└── Ports/
    └── Data/
        ├── IRepository<TEntity>.cs           # Generic repository port interface
        ├── IGameSessionRepository.cs         # Domain-specific repository port
        ├── IUserProfileRepository.cs
        ├── IScenarioRepository.cs
        ├── IContentBundleRepository.cs
        └── IUnitOfWork.cs

Infrastructure.Data/
├── Repositories/
│   ├── Repository<TEntity>.cs            # Generic repository implementation
│   ├── GameSessionRepository.cs          # Domain-specific implementation
│   ├── UserProfileRepository.cs
│   ├── ScenarioRepository.cs
│   └── ContentBundleRepository.cs
├── Specifications/
│   └── SpecificationEvaluator.cs         # EF Core specification evaluator
└── UnitOfWork/
    └── UnitOfWork.cs
```

**Architecture Note**: Following [Hexagonal Architecture](HEXAGONAL_ARCHITECTURE.md), repository interfaces (ports) are defined in the Application layer, while implementations (adapters) are in the Infrastructure layer.

---

## Generic Repository Interface

The generic repository interface provides common CRUD operations and **Specification Pattern support**.

```csharp
public interface IRepository<TEntity> where TEntity : class
{
    // Basic CRUD operations
    Task<TEntity?> GetByIdAsync(string id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);

    // Specification Pattern support (added for CQRS queries)
    Task<IEnumerable<TEntity>> ListAsync(ISpecification<TEntity> spec);
    Task<TEntity?> GetBySpecAsync(ISpecification<TEntity> spec);
    Task<int> CountAsync(ISpecification<TEntity> spec);
}
```

### Specification Methods

- **`ListAsync(spec)`** - Returns all entities matching the specification
- **`GetBySpecAsync(spec)`** - Returns a single entity matching the specification (or null)
- **`CountAsync(spec)`** - Returns count of entities matching the specification

These methods integrate with the [Specification Pattern](SPECIFICATION_PATTERN.md) for reusable query logic.

---

## Usage in CQRS Handlers

Repositories are used differently in **Command Handlers** (write operations) and **Query Handlers** (read operations).

### Query Handler Example (Read Operations)

Query handlers use repositories to **read data**. They typically use **Specifications** for complex queries.

```csharp
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

public class GetContentBundlesByAgeGroupQueryHandler
    : IQueryHandler<GetContentBundlesByAgeGroupQuery, IEnumerable<ContentBundle>>
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetContentBundlesByAgeGroupQueryHandler> _logger;

    public GetContentBundlesByAgeGroupQueryHandler(
        IContentBundleRepository repository,
        ILogger<GetContentBundlesByAgeGroupQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ContentBundle>> Handle(
        GetContentBundlesByAgeGroupQuery request,
        CancellationToken cancellationToken)
    {
        // Create specification for reusable query logic
        var spec = new ContentBundlesByAgeGroupSpecification(request.AgeGroup);

        // Execute query using repository
        var bundles = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} bundles for age group {AgeGroup}",
            bundles.Count(), request.AgeGroup);

        return bundles;
    }
}
```

**Key Points**:
- ✅ Queries use repositories for data access
- ✅ Specifications encapsulate complex query logic
- ✅ No `IUnitOfWork` needed (read-only operations)
- ✅ Logging for diagnostics

### Command Handler Example (Write Operations)

Command handlers use repositories to **modify data** and require `IUnitOfWork` for transactional consistency.

```csharp
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

public class CreateContentBundleCommandHandler
    : ICommandHandler<CreateContentBundleCommand, ContentBundle>
{
    private readonly IContentBundleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateContentBundleCommandHandler> _logger;

    public CreateContentBundleCommandHandler(
        IContentBundleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateContentBundleCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContentBundle> Handle(
        CreateContentBundleCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Create domain entity
        var bundle = new ContentBundle
        {
            Id = Guid.NewGuid().ToString(),
            Title = command.Request.Title,
            Description = command.Request.Description,
            AgeGroup = command.Request.AgeGroup,
            Price = command.Request.Price,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Add to repository
        await _repository.AddAsync(bundle);

        // 3. Save changes via Unit of Work
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating content bundle: {BundleId}", bundle.Id);
            throw;
        }

        _logger.LogInformation("Created content bundle: {BundleId} - {Title}",
            bundle.Id, bundle.Title);

        return bundle;
    }
}
```

**Key Points**:
- ✅ Commands use repositories to modify data
- ✅ `IUnitOfWork` ensures transactional consistency
- ✅ Error handling with try-catch
- ✅ Logging for success and failure

---

## Repository vs Direct LINQ

| Aspect | **Repository** | **Direct LINQ** |
|--------|----------------|-----------------|
| **Abstraction** | ✅ Decoupled from EF Core | ❌ Coupled to DbContext |
| **Testability** | ✅ Easy to mock | ❌ Requires DbContext mock |
| **Reusability** | ✅ Reusable query methods | ❌ Repeated LINQ everywhere |
| **Specification Support** | ✅ `ListAsync(spec)` | ❌ No specification support |
| **CQRS Fit** | ✅ Perfect for handlers | ⚠️ Mixes concerns |
| **Flexibility** | ✅ Can swap data source | ❌ Tightly coupled to EF |

**Recommendation**: Always use repositories in Command/Query handlers. Never inject `DbContext` directly.

## Domain-Specific Repositories

Domain-specific repositories extend the generic repository with entity-specific query methods.

**Example**: `IGameSessionRepository.cs`

```csharp
public interface IGameSessionRepository : IRepository<GameSession>
{
    // Custom query methods (consider creating Specifications instead)
    Task<IEnumerable<GameSession>> GetByAccountIdAsync(string accountId);
    Task<IEnumerable<GameSession>> GetInProgressSessionsAsync(string accountId);
    Task<GameSession?> GetActiveSessionForScenarioAsync(string accountId, string scenarioId);
}
```

**Example**: `IContentBundleRepository.cs`

```csharp
public interface IContentBundleRepository : IRepository<ContentBundle>
{
    // Inherits all generic repository methods
    // Plus specification support: ListAsync(spec), GetBySpecAsync(spec), CountAsync(spec)
}
```

**Modern Approach**: Instead of adding custom query methods, prefer creating **Specifications** for complex queries:

```csharp
// Instead of this:
public interface IContentBundleRepository : IRepository<ContentBundle>
{
    Task<IEnumerable<ContentBundle>> GetByAgeGroupAsync(string ageGroup);
}

// Do this:
public class ContentBundlesByAgeGroupSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundlesByAgeGroupSpecification(string ageGroup)
        : base(b => b.AgeGroup == ageGroup && !b.IsDeleted)
    {
        ApplyOrderBy(b => b.Title);
    }
}

// Usage in query handler:
var spec = new ContentBundlesByAgeGroupSpecification("Ages7to9");
var bundles = await _repository.ListAsync(spec);
```

**Benefits of Specifications**:
- ✅ Reusable across multiple handlers
- ✅ Testable independently
- ✅ Composable
- ✅ No need to add methods to repository interfaces

---

## Unit of Work Pattern

The Unit of Work pattern coordinates changes across multiple repositories and ensures transactional consistency. **Used in Commands, not Queries**.

```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

**See**: [Unit of Work Pattern](UNIT_OF_WORK_PATTERN.md) for detailed documentation.

---

## When to Use Repository Methods

| Method | When to Use | Example |
|--------|-------------|---------|
| **`GetByIdAsync(id)`** | Simple ID lookup | Get scenario by ID |
| **`GetAllAsync()`** | Get all entities (small tables) | Get all age groups |
| **`AddAsync(entity)`** | Create new entity (commands) | Create content bundle |
| **`UpdateAsync(entity)`** | Update existing entity (commands) | Update scenario |
| **`DeleteAsync(id)`** | Delete entity (commands) | Delete user profile |
| **`ListAsync(spec)`** | Complex queries (queries) | Get bundles by age group |
| **`GetBySpecAsync(spec)`** | Single entity with complex criteria | Get scenario with relations |
| **`CountAsync(spec)`** | Count matching entities | Count active bundles |

---

## Specification Pattern Integration

Repositories support the **Specification Pattern** for reusable query logic. This is the recommended approach for complex queries in CQRS.

### Example Workflow

**1. Create Specification** (Domain layer):
```csharp
public class ActiveContentBundlesWithScenariosSpecification : BaseSpecification<ContentBundle>
{
    public ActiveContentBundlesWithScenariosSpecification()
        : base(b => !b.IsDeleted)
    {
        AddInclude(b => b.Scenarios);
        ApplyOrderBy(b => b.Title);
    }
}
```

**2. Use in Query Handler** (Application layer):
```csharp
public class GetActiveContentBundlesQueryHandler
    : IQueryHandler<GetActiveContentBundlesQuery, IEnumerable<ContentBundle>>
{
    private readonly IContentBundleRepository _repository;

    public async Task<IEnumerable<ContentBundle>> Handle(
        GetActiveContentBundlesQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new ActiveContentBundlesWithScenariosSpecification();
        return await _repository.ListAsync(spec);
    }
}
```

**3. Repository Implementation** (Infrastructure layer):
```csharp
public class ContentBundleRepository : Repository<ContentBundle>, IContentBundleRepository
{
    public ContentBundleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ContentBundle>> ListAsync(ISpecification<ContentBundle> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    private IQueryable<ContentBundle> ApplySpecification(ISpecification<ContentBundle> spec)
    {
        return SpecificationEvaluator<ContentBundle>.GetQuery(_context.Set<ContentBundle>(), spec);
    }
}
```

**See**: [Specification Pattern](SPECIFICATION_PATTERN.md) for detailed documentation.

---

## Best Practices

### General Principles

1. **✅ One repository per aggregate root**
   - Create repositories for entities that are aggregate roots (Scenario, ContentBundle, User)
   - Don't create repositories for every entity (e.g., no need for `IScenarioChoiceRepository`)

2. **✅ Use Specifications over custom query methods**
   - Prefer creating `Specification` classes for complex queries
   - Only add custom methods to repository interfaces if truly entity-specific
   - Specifications are reusable, testable, and composable

3. **✅ Keep repositories focused on data access**
   - No business logic in repositories
   - No validation in repositories
   - No mapping/transformation in repositories
   - Pure data access only

4. **✅ Return domain entities**
   - Repositories return domain models (entities)
   - Don't return DTOs from repositories
   - Mapping to DTOs happens in handlers or controllers

5. **✅ Use UnitOfWork for commands**
   - Command handlers always use `IUnitOfWork.SaveChangesAsync()`
   - Query handlers never use `IUnitOfWork`
   - Ensures transactional consistency

### CQRS-Specific Practices

6. **✅ Queries use specifications**
   - Query handlers use `ListAsync(spec)`, `GetBySpecAsync(spec)`, `CountAsync(spec)`
   - Encapsulates complex query logic
   - Reusable across multiple query handlers

7. **✅ Commands use Add/Update/Delete**
   - Command handlers use `AddAsync()`, `UpdateAsync()`, `DeleteAsync()`
   - Always followed by `_unitOfWork.SaveChangesAsync()`
   - Wrapped in try-catch for error handling

8. **✅ Never inject DbContext in handlers**
   - Always use repository interfaces
   - Maintains abstraction and testability
   - Supports hexagonal architecture

### Testing Practices

9. **✅ Mock repositories in unit tests**
   - Use `Mock<IContentBundleRepository>` in handler tests
   - Setup repository methods to return test data
   - Verify repository method calls with `Verify()`

10. **✅ Test specifications independently**
    - Specifications can be tested without a database
    - Use in-memory collections with `.Compile()`
    - Fast, focused unit tests

---

## Migration Strategy

When migrating existing services to use repositories:

### Step 1: Create Repository Interface
```csharp
// Application/Ports/Data/IContentBundleRepository.cs
public interface IContentBundleRepository : IRepository<ContentBundle>
{
    // Only add methods if not achievable with specifications
}
```

### Step 2: Implement Repository
```csharp
// Infrastructure.Data/Repositories/ContentBundleRepository.cs
public class ContentBundleRepository : Repository<ContentBundle>, IContentBundleRepository
{
    public ContentBundleRepository(ApplicationDbContext context) : base(context)
    {
    }
}
```

### Step 3: Register in DI
```csharp
// Program.cs
builder.Services.AddScoped<IContentBundleRepository, ContentBundleRepository>();
```

### Step 4: Replace DbContext with Repository
```csharp
// Before:
public class CreateContentBundleCommandHandler
{
    private readonly ApplicationDbContext _context;

    public async Task<ContentBundle> Handle(...)
    {
        _context.ContentBundles.Add(bundle);
        await _context.SaveChangesAsync();
    }
}

// After:
public class CreateContentBundleCommandHandler
{
    private readonly IContentBundleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<ContentBundle> Handle(...)
    {
        await _repository.AddAsync(bundle);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

---

## Related Patterns

- **[CQRS Pattern](CQRS_PATTERN.md)** - Commands and Queries use repositories
- **[Specification Pattern](SPECIFICATION_PATTERN.md)** - Reusable query logic for repositories
- **[Unit of Work Pattern](UNIT_OF_WORK_PATTERN.md)** - Transactional consistency with repositories
- **[Hexagonal Architecture](HEXAGONAL_ARCHITECTURE.md)** - Repositories as ports and adapters

---

## Quick Start Guides

- **[Creating Your First Command](QUICKSTART_COMMAND.md)** - Learn how to use repositories in commands
- **[Creating Your First Query](QUICKSTART_QUERY.md)** - Learn how to use repositories in queries
- **[Creating Your First Specification](QUICKSTART_SPECIFICATION.md)** - Learn how to use specifications with repositories

---

## References

- [Repository Pattern - Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Unit of Work Pattern - Martin Fowler](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Specification Pattern - Martin Fowler](https://martinfowler.com/apsupp/spec.pdf)
- [ADR-0001: Adopt CQRS Pattern](../adr/ADR-0001-adopt-cqrs-pattern.md)
- [ADR-0002: Adopt Specification Pattern](../adr/ADR-0002-adopt-specification-pattern.md)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.

