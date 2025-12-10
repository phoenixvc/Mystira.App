# Specification Pattern

## Status: ✅ IMPLEMENTED

**Implementation Date**: November 2025
**Related Patterns**: [CQRS Pattern](CQRS_PATTERN.md), [Repository Pattern](REPOSITORY_PATTERN.md)

---

## Overview

The Specification Pattern encapsulates **query logic** into reusable, composable objects. Instead of scattering query logic across repositories and services, specifications centralize and standardize how entities are queried.

**Key Principle**: *"Encapsulate business rules in specifications that can be combined and reused."* - Eric Evans & Martin Fowler

---

## Benefits

✅ **Reusability** - Define query logic once, use everywhere
✅ **Composability** - Combine specifications for complex queries
✅ **Testability** - Test query logic independently of infrastructure
✅ **Maintainability** - Centralized query definitions
✅ **Type Safety** - Compile-time validation of queries
✅ **Domain-Driven** - Specifications live in Domain layer
✅ **DRY Principle** - No duplicate query logic

---

## Architecture

```
┌─────────────────────────────────────────────────┐
│           Query Handler (Application)           │
│  - Receives query request                       │
│  - Creates specification                        │
│  - Calls repository with spec                   │
└──────────────┬──────────────────────────────────┘
               ↓ creates
┌──────────────┴──────────────────────────────────┐
│          Specification (Domain)                 │
│  - Criteria (WHERE clause)                      │
│  - Includes (eager loading)                     │
│  - OrderBy/OrderByDescending (sorting)          │
│  - Paging (Skip/Take)                           │
│  - GroupBy (grouping)                           │
└──────────────┬──────────────────────────────────┘
               ↓ passed to
┌──────────────┴──────────────────────────────────┐
│         Repository (Infrastructure)             │
│  - GetBySpecAsync(spec)                         │
│  - ListAsync(spec)                              │
│  - CountAsync(spec)                             │
└──────────────┬──────────────────────────────────┘
               ↓ evaluates
┌──────────────┴──────────────────────────────────┐
│      SpecificationEvaluator (Infrastructure)    │
│  - Applies criteria to EF Core IQueryable       │
│  - Converts specification to SQL                │
└─────────────────────────────────────────────────┘
```

---

## Implementation

### Project Structure

```
Domain/Specifications/
├── ISpecification.cs                    # Specification interface
├── BaseSpecification.cs                 # Base implementation
└── ScenarioSpecifications.cs            # Domain-specific specifications
    ├── ScenariosByAgeGroupSpecification
    ├── ScenariosByTagSpecification
    ├── ScenariosByDifficultySpecification
    ├── ActiveScenariosSpecification
    ├── PaginatedScenariosSpecification
    ├── ScenariosByCreatorSpecification
    ├── ScenariosByArchetypeSpecification
    └── FeaturedScenariosSpecification

Infrastructure.Data/Specifications/
└── SpecificationEvaluator.cs            # EF Core specification evaluator
```

### Core Interfaces

#### ISpecification<T>

```csharp
using System.Linq.Expressions;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification pattern for encapsulating query logic
/// Specifications are composable and reusable query definitions
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Criteria expression for filtering entities (WHERE clause)
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Include expressions for eager loading related entities
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Include strings for eager loading related entities using ThenInclude
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression for ascending sort
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by expression for descending sort
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Group by expression
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Number of records to skip (for pagination)
    /// </summary>
    int Skip { get; }

    /// <summary>
    /// Number of records to take (for pagination)
    /// </summary>
    int Take { get; }

    /// <summary>
    /// Enable pagination
    /// </summary>
    bool IsPagingEnabled { get; }
}
```

#### BaseSpecification<T>

```csharp
using System.Linq.Expressions;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Base implementation of the Specification pattern
/// Provides a fluent API for building query specifications
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification()
    {
    }

    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public Expression<Func<T, object>>? GroupBy { get; private set; }
    public int Skip { get; private set; }
    public int Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    /// <summary>
    /// Add an include expression for eager loading
    /// </summary>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Add an include string for eager loading with ThenInclude
    /// </summary>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Apply ascending ordering
    /// </summary>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Apply descending ordering
    /// </summary>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    /// <summary>
    /// Apply grouping
    /// </summary>
    protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
    }

    /// <summary>
    /// Apply pagination
    /// </summary>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
```

---

## Creating Specifications

### Simple Specification

Filter by single property:

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification for scenarios by age group
/// </summary>
public class ScenariosByAgeGroupSpecification : BaseSpecification<Scenario>
{
    public ScenariosByAgeGroupSpecification(string ageGroup)
        : base(s => s.AgeGroup == ageGroup)  // WHERE AgeGroup = @ageGroup
    {
        ApplyOrderBy(s => s.Title);  // ORDER BY Title ASC
    }
}
```

**Generated SQL**:
```sql
SELECT * FROM Scenarios
WHERE AgeGroup = 'Ages7to9'
ORDER BY Title ASC
```

### Composite Specification

Multiple filter criteria:

```csharp
public class FeaturedScenariosSpecification : BaseSpecification<Scenario>
{
    public FeaturedScenariosSpecification()
        : base(s =>
            !s.IsDeleted &&
            s.Tags != null &&
            s.Tags.Contains("featured"))
    {
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}
```

**Generated SQL**:
```sql
SELECT * FROM Scenarios
WHERE IsDeleted = 0
  AND Tags IS NOT NULL
  AND Tags LIKE '%featured%'
ORDER BY CreatedAt DESC
```

### Specification with Eager Loading

Include related entities:

```csharp
public class ScenarioWithScenesSpecification : BaseSpecification<Scenario>
{
    public ScenarioWithScenesSpecification(string scenarioId)
        : base(s => s.Id == scenarioId)
    {
        AddInclude(s => s.Scenes);  // Include Scenes navigation property
        AddInclude("Scenes.Choices");  // ThenInclude Scenes.Choices
    }
}
```

**Generated SQL**:
```sql
SELECT s.*, sc.*, c.*
FROM Scenarios s
LEFT JOIN Scenes sc ON s.Id = sc.ScenarioId
LEFT JOIN Choices c ON sc.Id = c.SceneId
WHERE s.Id = @scenarioId
```

### Specification with Paging

Paginated results:

```csharp
public class PaginatedScenariosSpecification : BaseSpecification<Scenario>
{
    public PaginatedScenariosSpecification(int pageNumber, int pageSize)
        : base(s => !s.IsDeleted)
    {
        ApplyOrderByDescending(s => s.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
```

**Generated SQL**:
```sql
SELECT * FROM Scenarios
WHERE IsDeleted = 0
ORDER BY CreatedAt DESC
OFFSET 20 ROWS    -- Page 3, size 10
FETCH NEXT 10 ROWS ONLY
```

---

## Repository Integration

### Repository Interface

```csharp
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.Ports.Data;

public interface IRepository<TEntity> where TEntity : class
{
    // Basic CRUD operations
    Task<TEntity?> GetByIdAsync(string id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(string id);

    // Specification pattern operations
    Task<TEntity?> GetBySpecAsync(ISpecification<TEntity> spec);
    Task<IEnumerable<TEntity>> ListAsync(ISpecification<TEntity> spec);
    Task<int> CountAsync(ISpecification<TEntity> spec);
}
```

### Repository Implementation

```csharp
using Microsoft.EntityFrameworkCore;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Specifications;
using Mystira.App.Infrastructure.Data.Specifications;

namespace Mystira.App.Infrastructure.Data.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // Basic operations...

    // Specification operations
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
        return SpecificationEvaluator<TEntity>.GetQuery(_dbSet.AsQueryable(), spec);
    }
}
```

### SpecificationEvaluator

Converts specifications to EF Core queries:

```csharp
using Microsoft.EntityFrameworkCore;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Infrastructure.Data.Specifications;

public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        // Apply criteria (WHERE clause)
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes (eager loading)
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply include strings (for ThenInclude scenarios)
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply grouping
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}
```

---

## Using Specifications

### In Query Handlers (CQRS)

```csharp
using Mystira.App.Application.CQRS;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

public class GetScenariosByAgeGroupQueryHandler : IQueryHandler<GetScenariosByAgeGroupQuery, IEnumerable<Scenario>>
{
    private readonly IScenarioRepository _repository;

    public GetScenariosByAgeGroupQueryHandler(IScenarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Scenario>> Handle(GetScenariosByAgeGroupQuery request, CancellationToken cancellationToken)
    {
        // Create specification
        var spec = new ScenariosByAgeGroupSpecification(request.AgeGroup);

        // Use specification to query repository
        var scenarios = await _repository.ListAsync(spec);

        return scenarios;
    }
}
```

### In Use Cases

```csharp
public class GetPaginatedScenariosUseCase
{
    private readonly IScenarioRepository _repository;

    public async Task<IEnumerable<Scenario>> ExecuteAsync(int pageNumber, int pageSize)
    {
        var spec = new PaginatedScenariosSpecification(pageNumber, pageSize);
        return await _repository.ListAsync(spec);
    }
}
```

### Direct Usage

```csharp
// Get featured scenarios
var featuredSpec = new FeaturedScenariosSpecification();
var featuredScenarios = await _repository.ListAsync(featuredSpec);

// Get scenarios by creator
var creatorSpec = new ScenariosByCreatorSpecification("creator-123");
var creatorScenarios = await _repository.ListAsync(creatorSpec);

// Get count
var activeSpec = new ActiveScenariosSpecification();
var count = await _repository.CountAsync(activeSpec);
```

---

## Pre-Built Specifications

### ScenariosByAgeGroupSpecification

Filter scenarios by target age group:

```csharp
public class ScenariosByAgeGroupSpecification : BaseSpecification<Scenario>
{
    public ScenariosByAgeGroupSpecification(string ageGroup)
        : base(s => s.AgeGroup == ageGroup)
    {
        ApplyOrderBy(s => s.Title);
    }
}
```

### ScenariosByTagSpecification

Filter scenarios containing a specific tag:

```csharp
public class ScenariosByTagSpecification : BaseSpecification<Scenario>
{
    public ScenariosByTagSpecification(string tag)
        : base(s => s.Tags != null && s.Tags.Contains(tag))
    {
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}
```

### ScenariosByDifficultySpecification

Filter scenarios by difficulty level:

```csharp
public class ScenariosByDifficultySpecification : BaseSpecification<Scenario>
{
    public ScenariosByDifficultySpecification(string difficulty)
        : base(s => s.Difficulty == difficulty)
    {
        ApplyOrderBy(s => s.Title);
    }
}
```

### ActiveScenariosSpecification

Get all non-deleted scenarios:

```csharp
public class ActiveScenariosSpecification : BaseSpecification<Scenario>
{
    public ActiveScenariosSpecification()
        : base(s => !s.IsDeleted)
    {
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}
```

### PaginatedScenariosSpecification

Get paginated scenario results:

```csharp
public class PaginatedScenariosSpecification : BaseSpecification<Scenario>
{
    public PaginatedScenariosSpecification(int pageNumber, int pageSize)
        : base(s => !s.IsDeleted)
    {
        ApplyOrderByDescending(s => s.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
```

### ScenariosByCreatorSpecification

Filter scenarios by creator account:

```csharp
public class ScenariosByCreatorSpecification : BaseSpecification<Scenario>
{
    public ScenariosByCreatorSpecification(string creatorAccountId)
        : base(s => s.CreatedBy == creatorAccountId)
    {
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}
```

### ScenariosByArchetypeSpecification

Filter scenarios containing a specific character archetype:

```csharp
public class ScenariosByArchetypeSpecification : BaseSpecification<Scenario>
{
    public ScenariosByArchetypeSpecification(string archetypeName)
        : base(s => s.Archetypes != null && s.Archetypes.Any(a => a.Name == archetypeName))
    {
        ApplyOrderBy(s => s.Title);
    }
}
```

### FeaturedScenariosSpecification

Get featured, high-quality scenarios:

```csharp
public class FeaturedScenariosSpecification : BaseSpecification<Scenario>
{
    public FeaturedScenariosSpecification()
        : base(s =>
            !s.IsDeleted &&
            s.Tags != null &&
            s.Tags.Contains("featured"))
    {
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}
```

---

## Advanced Topics

### Composing Specifications

Combine multiple specifications:

```csharp
public class AndSpecification<T> : BaseSpecification<T>
{
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        : base(x => left.Criteria.Compile()(x) && right.Criteria.Compile()(x))
    {
    }
}

// Usage
var ageSpec = new ScenariosByAgeGroupSpecification("Ages7to9");
var tagSpec = new ScenariosByTagSpecification("adventure");
var combinedSpec = new AndSpecification<Scenario>(ageSpec, tagSpec);
```

### Dynamic Specifications

Build specifications at runtime:

```csharp
public class ScenarioSearchSpecification : BaseSpecification<Scenario>
{
    public ScenarioSearchSpecification(string? ageGroup, string? tag, string? difficulty)
        : base(BuildCriteria(ageGroup, tag, difficulty))
    {
        ApplyOrderBy(s => s.Title);
    }

    private static Expression<Func<Scenario, bool>> BuildCriteria(string? ageGroup, string? tag, string? difficulty)
    {
        return s =>
            (string.IsNullOrEmpty(ageGroup) || s.AgeGroup == ageGroup) &&
            (string.IsNullOrEmpty(tag) || (s.Tags != null && s.Tags.Contains(tag))) &&
            (string.IsNullOrEmpty(difficulty) || s.Difficulty == difficulty) &&
            !s.IsDeleted;
    }
}
```

---

## Best Practices

### 1. Naming Conventions

✅ **Descriptive Names** - `ScenariosByAgeGroupSpecification`
✅ **Suffix** - Always end with `Specification`
✅ **Specific** - Not too generic, not too specific
❌ **Avoid** - `GetScenariosSpec`, `ScenarioFilter`

### 2. Specification Design

✅ **Single Responsibility** - One query purpose per spec
✅ **Immutable** - Specifications should be immutable
✅ **Reusable** - Design for reuse across use cases
✅ **Testable** - Easy to unit test
✅ **Domain Layer** - Keep specs in Domain, not Infrastructure

### 3. Performance

✅ **Paging** - Always use paging for large result sets
✅ **Includes** - Only include needed navigation properties
✅ **Projection** - Use `.Select()` for specific fields
✅ **Indexes** - Ensure database indexes on filtered columns
✅ **Compilation** - Avoid compiling expressions in hot paths

### 4. Organization

✅ **One File Per Spec** - For complex specifications
✅ **Grouped Files** - Related specs in same file (ScenarioSpecifications.cs)
✅ **Namespace** - `Domain.Specifications`
✅ **Documentation** - XML comments explaining purpose

---

## Testing

### Specification Unit Tests

```csharp
public class ScenariosByAgeGroupSpecificationTests
{
    [Fact]
    public void Criteria_FiltersCorrectly()
    {
        // Arrange
        var spec = new ScenariosByAgeGroupSpecification("Ages7to9");
        var scenarios = new List<Scenario>
        {
            new() { Id = "1", AgeGroup = "Ages7to9", Title = "Match" },
            new() { Id = "2", AgeGroup = "Ages10to12", Title = "No Match" }
        };

        // Act
        var filtered = scenarios.Where(spec.Criteria.Compile());

        // Assert
        Assert.Single(filtered);
        Assert.Equal("1", filtered.First().Id);
    }
}
```

### Integration Tests with Repository

```csharp
public class SpecificationIntegrationTests
{
    [Fact]
    public async Task ListAsync_WithPaginatedSpec_ReturnsPaginatedResults()
    {
        // Arrange
        var repository = new ScenarioRepository(_context);
        var spec = new PaginatedScenariosSpecification(pageNumber: 1, pageSize: 10);

        // Act
        var scenarios = await repository.ListAsync(spec);

        // Assert
        Assert.True(scenarios.Count() <= 10);
    }
}
```

---

## Migration from Repository Methods

### Before (Repository Methods)

```csharp
public interface IScenarioRepository
{
    Task<IEnumerable<Scenario>> GetByAgeGroupAsync(string ageGroup);
    Task<IEnumerable<Scenario>> GetByTagAsync(string tag);
    Task<IEnumerable<Scenario>> GetByDifficultyAsync(string difficulty);
    Task<IEnumerable<Scenario>> GetActiveAsync();
    Task<IEnumerable<Scenario>> GetPaginatedAsync(int page, int size);
    Task<IEnumerable<Scenario>> GetByCreatorAsync(string creatorId);
}
```

### After (Specifications)

```csharp
public interface IRepository<T>
{
    Task<IEnumerable<T>> ListAsync(ISpecification<T> spec);
    Task<T?> GetBySpecAsync(ISpecification<T> spec);
    Task<int> CountAsync(ISpecification<T> spec);
}

// Usage
var ageGroupScenarios = await _repository.ListAsync(new ScenariosByAgeGroupSpecification("Ages7to9"));
var tagScenarios = await _repository.ListAsync(new ScenariosByTagSpecification("adventure"));
var difficultyScenarios = await _repository.ListAsync(new ScenariosByDifficultySpecification("easy"));
var activeScenarios = await _repository.ListAsync(new ActiveScenariosSpecification());
var paginatedScenarios = await _repository.ListAsync(new PaginatedScenariosSpecification(1, 10));
var creatorScenarios = await _repository.ListAsync(new ScenariosByCreatorSpecification("creator-123"));
```

---

## Related Patterns

- **[CQRS Pattern](CQRS_PATTERN.md)** - Queries use specifications
- **[Repository Pattern](REPOSITORY_PATTERN.md)** - Repositories accept specifications
- **[Unit of Work Pattern](UNIT_OF_WORK_PATTERN.md)** - Transaction management
- **Domain-Driven Design** - Specifications are domain concepts

---

## References

- [Specification Pattern - Martin Fowler](https://martinfowler.com/apsupp/spec.pdf)
- [Specification Pattern - Eric Evans (DDD)](https://www.domainlanguage.com/ddd/)
- [ardalis/Specification](https://github.com/ardalis/Specification) - .NET implementation
- [Enterprise Patterns - Martin Fowler](https://martinfowler.com/eaaCatalog/)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
