# Unit of Work Pattern

## Overview

The Unit of Work pattern maintains a list of objects affected by a business transaction and coordinates writing out changes and resolving concurrency problems. It ensures that all changes are committed together or rolled back together.

**Key Points**:
- ✅ **Used in Commands** - Write operations that modify state
- ❌ **NOT used in Queries** - Read operations don't need transactions
- 🔒 **Transactional Consistency** - All changes succeed or fail together
- 🎯 **Single Responsibility** - Coordinates persistence, nothing else

---

## Implementation in Mystira.App

### Location

- **Port Interface**: `src/Mystira.App.Application/Ports/Data/IUnitOfWork.cs` (Application layer)
- **Adapter Implementation**: `src/Mystira.App.Infrastructure.Data/UnitOfWork/UnitOfWork.cs` (Infrastructure layer)

### Purpose

1. **Transaction Management** 🔒
   - Ensures all repository changes are committed atomically
   - All changes succeed together or fail together
   - Prevents partial updates

2. **Change Tracking** 📝
   - Coordinates changes across multiple repositories
   - Tracks all modifications in a single transaction
   - Efficient batch updates

3. **Data Consistency** ✅
   - Maintains data integrity across related operations
   - Prevents inconsistent state
   - Supports ACID properties

---

## Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all pending changes to the database
    /// Returns the number of state entries written to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction
    /// Use for explicit transaction control
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction
    /// All changes are permanently saved
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the current transaction
    /// All changes are discarded
    /// </summary>
    Task RollbackTransactionAsync();
}
```

---

## When to Use Unit of Work

| Scenario | Use UnitOfWork? | Reason |
|----------|-----------------|--------|
| **Command Handler** (Create) | ✅ Yes | Persisting new entities |
| **Command Handler** (Update) | ✅ Yes | Modifying existing entities |
| **Command Handler** (Delete) | ✅ Yes | Removing entities |
| **Query Handler** | ❌ No | Read-only, no changes to persist |
| **Multiple Repository Operations** | ✅ Yes | Coordinate changes atomically |
| **Single Repository Operation** | ✅ Yes | Still need to persist changes |

**Rule of Thumb**: If you're using a **Command Handler**, you need `IUnitOfWork`. If you're using a **Query Handler**, you don't.

---

## Usage in CQRS Command Handlers

### Example 1: Simple Command (Create Entity)

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
        // 1. Create entity
        var bundle = new ContentBundle
        {
            Id = Guid.NewGuid().ToString(),
            Title = command.Request.Title,
            Description = command.Request.Description,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Add to repository
        await _repository.AddAsync(bundle);

        // 3. Persist changes via Unit of Work
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create content bundle: {Title}", bundle.Title);
            throw;
        }

        _logger.LogInformation("Created content bundle: {BundleId}", bundle.Id);

        return bundle;
    }
}
```

**Key Points**:
- ✅ Repository adds entity to context
- ✅ UnitOfWork persists changes
- ✅ Try-catch for error handling
- ✅ Logging for success and failure

### Example 2: Command with Multiple Operations

```csharp
public class UpdateScenarioCommandHandler
    : ICommandHandler<UpdateScenarioCommand, Scenario>
{
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IContentBundleRepository _bundleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateScenarioCommandHandler> _logger;

    public async Task<Scenario> Handle(
        UpdateScenarioCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Get existing scenario
        var scenario = await _scenarioRepository.GetByIdAsync(command.ScenarioId);
        if (scenario == null)
            throw new NotFoundException($"Scenario {command.ScenarioId} not found");

        // 2. Update scenario properties
        scenario.Title = command.Request.Title;
        scenario.Description = command.Request.Description;
        scenario.UpdatedAt = DateTime.UtcNow;

        await _scenarioRepository.UpdateAsync(scenario);

        // 3. Update related content bundle (if needed)
        if (command.Request.UpdateBundle)
        {
            var bundle = await _bundleRepository.GetByIdAsync(scenario.ContentBundleId);
            bundle.UpdatedAt = DateTime.UtcNow;
            await _bundleRepository.UpdateAsync(bundle);
        }

        // 4. Single SaveChanges for all operations
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scenario: {ScenarioId}", scenario.Id);
            throw;
        }

        _logger.LogInformation("Updated scenario: {ScenarioId}", scenario.Id);

        return scenario;
    }
}
```

**Key Points**:
- ✅ Multiple repository operations
- ✅ Single `SaveChangesAsync()` call
- ✅ All changes committed atomically
- ✅ If any operation fails, all are rolled back

---

## NOT Used in Query Handlers

Query handlers are **read-only** and should **NOT** inject or use `IUnitOfWork`.

```csharp
// ✅ CORRECT - Query handler without UnitOfWork
public class GetContentBundlesQueryHandler
    : IQueryHandler<GetContentBundlesQuery, IEnumerable<ContentBundle>>
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetContentBundlesQueryHandler> _logger;

    public GetContentBundlesQueryHandler(
        IContentBundleRepository repository,
        ILogger<GetContentBundlesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
        // NO IUnitOfWork - queries are read-only!
    }

    public async Task<IEnumerable<ContentBundle>> Handle(
        GetContentBundlesQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new ActiveContentBundlesSpecification();
        var bundles = await _repository.ListAsync(spec);

        // No SaveChangesAsync needed - read-only operation
        return bundles;
    }
}
```

```csharp
// ❌ WRONG - Query handler with UnitOfWork (don't do this!)
public class GetContentBundlesQueryHandler
{
    private readonly IContentBundleRepository _repository;
    private readonly IUnitOfWork _unitOfWork; // ❌ NOT NEEDED

    public async Task<IEnumerable<ContentBundle>> Handle(...)
    {
        var bundles = await _repository.ListAsync(spec);
        // await _unitOfWork.SaveChangesAsync(); // ❌ NEVER DO THIS IN QUERIES
        return bundles;
    }
}
```

---

## Explicit Transaction Example

For operations requiring explicit transaction control (rare), use `BeginTransactionAsync()`:

```csharp
public class TransferPurchaseCommandHandler
    : ICommandHandler<TransferPurchaseCommand>
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUserProfileRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransferPurchaseCommandHandler> _logger;

    public async Task Handle(
        TransferPurchaseCommand command,
        CancellationToken cancellationToken)
    {
        // Begin explicit transaction for multi-step operation
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // 1. Get purchase and validate
            var purchase = await _purchaseRepository.GetByIdAsync(command.PurchaseId);
            if (purchase == null)
                throw new NotFoundException($"Purchase {command.PurchaseId} not found");

            // 2. Get source and target users
            var sourceUser = await _userRepository.GetByIdAsync(purchase.UserId);
            var targetUser = await _userRepository.GetByIdAsync(command.TargetUserId);

            // 3. Update purchase ownership
            purchase.UserId = command.TargetUserId;
            purchase.TransferredAt = DateTime.UtcNow;
            await _purchaseRepository.UpdateAsync(purchase);

            // 4. Update user statistics
            sourceUser.TotalPurchases--;
            targetUser.TotalPurchases++;
            await _userRepository.UpdateAsync(sourceUser);
            await _userRepository.UpdateAsync(targetUser);

            // 5. Commit all changes atomically
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Transferred purchase {PurchaseId} from user {SourceUserId} to {TargetUserId}",
                command.PurchaseId, purchase.UserId, command.TargetUserId);
        }
        catch (Exception ex)
        {
            // Rollback on any error
            await _unitOfWork.RollbackTransactionAsync();

            _logger.LogError(ex, "Failed to transfer purchase {PurchaseId}", command.PurchaseId);
            throw;
        }
    }
}
```

**Key Points**:
- ✅ Explicit transaction for complex multi-step operations
- ✅ All changes committed together
- ✅ Rollback on any failure
- ✅ Prevents partial updates

**Note**: For most commands, `SaveChangesAsync()` is sufficient. Only use explicit transactions for complex multi-repository operations.

---

## Benefits

### 1. Atomicity ⚛️
- All changes succeed or fail together
- No partial updates
- Database remains consistent

### 2. Consistency ✅
- Maintains data integrity
- Related changes coordinated
- Prevents orphaned data

### 3. Performance ⚡
- Single database round-trip for multiple changes
- Batch updates more efficient
- Reduces database load

### 4. Error Handling 🛡️
- Automatic rollback on errors
- Clear error boundaries
- Simplified error recovery

---

## Best Practices

### General Practices

1. **✅ One UnitOfWork per request**
   - Typically scoped per HTTP request
   - Registered as `Scoped` in DI container
   - Automatic disposal at end of request

2. **✅ Always wrap SaveChangesAsync in try-catch**
   - Catch and log database errors
   - Re-throw exceptions after logging
   - Provides clear error messages

3. **✅ Dispose properly**
   - UnitOfWork implements `IDisposable`
   - Use `using` statements or rely on DI disposal
   - Ensures database connections are closed

### CQRS-Specific Practices

4. **✅ Use UnitOfWork ONLY in Command Handlers**
   - Commands modify state → use UnitOfWork
   - Queries read data → don't use UnitOfWork
   - Clear separation of concerns

5. **✅ Save changes AFTER all repository operations**
   - Add/Update/Delete entities first
   - Call `SaveChangesAsync()` once at the end
   - Batches all changes into one transaction

6. **✅ Use explicit transactions sparingly**
   - Most commands only need `SaveChangesAsync()`
   - Only use `BeginTransactionAsync()` for complex multi-step operations
   - Adds overhead and complexity

### Error Handling Practices

7. **✅ Log all save failures**
   - Log at Error level with context
   - Include entity IDs and operation details
   - Helps with debugging

8. **✅ Don't swallow exceptions**
   - Re-throw after logging
   - Let exception propagate to caller
   - Allows higher-level error handling

---

## Common Mistakes to Avoid

❌ **Using UnitOfWork in Query Handlers**
- Queries are read-only
- No changes to persist
- Wastes resources

❌ **Calling SaveChangesAsync multiple times**
- Defeats the purpose of Unit of Work
- Creates multiple database round-trips
- Potential for partial updates

❌ **Not wrapping SaveChangesAsync in try-catch**
- Database errors are common
- No logging of failures
- Hard to debug

❌ **Forgetting to inject UnitOfWork in Command Handlers**
- Changes won't be persisted
- Silent failures
- Data loss

❌ **Using explicit transactions for simple commands**
- Unnecessary complexity
- `SaveChangesAsync()` is sufficient for most cases
- Performance overhead

---

## Registration in DI Container

```csharp
// In Program.cs or Startup.cs
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Scoped lifetime ensures:
// - One instance per HTTP request
// - Automatic disposal at end of request
// - Shared across all handlers in the same request
```

---

## Command vs Query Comparison

| Aspect | **Command Handler** | **Query Handler** |
|--------|---------------------|-------------------|
| **Purpose** | Modify data | Read data |
| **Uses IUnitOfWork** | ✅ Yes | ❌ No |
| **Calls SaveChangesAsync** | ✅ Yes | ❌ No |
| **Transactional** | ✅ Yes | ❌ No |
| **Error Handling** | Try-catch required | Optional |
| **Example** | `CreateContentBundleCommand` | `GetContentBundlesQuery` |

---

## Related Patterns

- **[Repository Pattern](REPOSITORY_PATTERN.md)** - UnitOfWork coordinates repository changes
- **[CQRS Pattern](CQRS_PATTERN.md)** - Commands use UnitOfWork, Queries don't
- **[Hexagonal Architecture](HEXAGONAL_ARCHITECTURE.md)** - UnitOfWork as port/adapter

---

## Quick Start Guides

- **[Creating Your First Command](QUICKSTART_COMMAND.md)** - Learn how to use UnitOfWork in commands
- **[Creating Your First Query](QUICKSTART_QUERY.md)** - Understand why queries don't use UnitOfWork

---

## References

- [Unit of Work Pattern - Martin Fowler](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Unit of Work with Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#the-repository-pattern)
- [Entity Framework Core - DbContext as Unit of Work](https://docs.microsoft.com/en-us/ef/core/saving/)
- [ADR-0001: Adopt CQRS Pattern](../adr/ADR-0001-adopt-cqrs-pattern.md)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.

