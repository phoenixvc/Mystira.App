# Quick Start: Creating Your First Specification

**Estimated Time**: 7 minutes
**Difficulty**: Beginner
**Pattern**: [Specification Pattern](SPECIFICATION_PATTERN.md)

---

## What You'll Build

By the end of this guide, you'll have created specifications that:
- ✅ Encapsulate complex query logic
- ✅ Are reusable across multiple queries
- ✅ Support filtering, sorting, paging, and eager loading
- ✅ Are composable and testable
- ✅ Follow the Specification Pattern

---

## Prerequisites

- ✅ Completed [Creating Your First Query](QUICKSTART_QUERY.md) (recommended)
- ✅ Basic understanding of LINQ and lambda expressions
- ✅ Familiarity with Entity Framework Core (helpful)

---

## What is a Specification?

A **Specification** is a reusable query object that encapsulates filtering, sorting, paging, and eager loading logic.

**Without Specifications** (❌ Bad):
```csharp
// Query logic scattered across handlers
var bundles = await _context.ContentBundles
    .Where(b => !b.IsDeleted && b.AgeGroup == ageGroup)
    .OrderBy(b => b.Title)
    .Include(b => b.Scenarios)
    .ToListAsync();
```

**With Specifications** (✅ Good):
```csharp
// Reusable, testable, composable
var spec = new ContentBundlesByAgeGroupSpecification(ageGroup);
var bundles = await _repository.ListAsync(spec);
```

**Benefits**:
- 🔄 **Reusable** - Use the same spec in multiple queries
- 🧪 **Testable** - Test query logic independently
- 📦 **Composable** - Combine multiple specifications
- 📖 **Readable** - Named specifications are self-documenting
- 🛠️ **Maintainable** - Change query logic in one place

---

## Step 1: Understand BaseSpecification (2 minutes)

All specifications inherit from `BaseSpecification<T>`, which provides a fluent API.

**Location**: `Domain/Specifications/BaseSpecification.cs`

### Available Methods

| Method | Purpose | Example |
|--------|---------|---------|
| **Constructor** | WHERE clause | `base(b => b.IsActive)` |
| `AddInclude()` | Eager loading (JOIN) | `AddInclude(b => b.Scenarios)` |
| `ApplyOrderBy()` | Sort ascending | `ApplyOrderBy(b => b.Title)` |
| `ApplyOrderByDescending()` | Sort descending | `ApplyOrderByDescending(b => b.CreatedAt)` |
| `ApplyPaging()` | OFFSET/LIMIT | `ApplyPaging(skip: 0, take: 10)` |
| `ApplyGroupBy()` | GROUP BY | `ApplyGroupBy(b => b.AgeGroup)` |

**Example**:
```csharp
public class MySpecification : BaseSpecification<ContentBundle>
{
    public MySpecification()
        : base(b => !b.IsDeleted)  // WHERE IsDeleted = false
    {
        AddInclude(b => b.Scenarios);           // INNER JOIN Scenarios
        ApplyOrderBy(b => b.Title);              // ORDER BY Title ASC
        ApplyPaging(skip: 0, take: 10);          // OFFSET 0 LIMIT 10
    }
}
```

This translates to:
```sql
SELECT * FROM ContentBundles cb
INNER JOIN Scenarios s ON cb.Id = s.ContentBundleId
WHERE cb.IsDeleted = 0
ORDER BY cb.Title ASC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
```

---

## Step 2: Create Your First Specification (2 minutes)

Let's create a specification for active content bundles with their scenarios loaded.

**Create**: `Domain/Specifications/ContentBundleSpecifications.cs`

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification for active content bundles with scenarios
/// </summary>
public class ActiveContentBundlesWithScenariosSpecification : BaseSpecification<ContentBundle>
{
    public ActiveContentBundlesWithScenariosSpecification()
        : base(b => !b.IsDeleted)  // Filter: Only non-deleted bundles
    {
        // Eager load scenarios (avoids N+1 queries)
        AddInclude(b => b.Scenarios);

        // Sort alphabetically by title
        ApplyOrderBy(b => b.Title);
    }
}
```

**That's it!** Your first specification is ready to use.

### Specification Naming Convention

✅ **Descriptive names**: `ActiveContentBundlesWithScenariosSpecification`
✅ **State what they do**: `PaginatedContentBundlesSpecification`
✅ **End with `Specification`**: Clearly identifies the pattern
✅ **Use singular file names**: Group related specs in one file (e.g., `ContentBundleSpecifications.cs`)

---

## Step 3: Use the Specification in a Query Handler (2 minutes)

Now let's use the specification in a query handler.

**Create**: `Application/CQRS/ContentBundles/Queries/GetActiveContentBundlesQuery.cs`

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Query to retrieve all active content bundles with scenarios
/// </summary>
public record GetActiveContentBundlesQuery() : IQuery<IEnumerable<ContentBundle>>;
```

**Create**: `Application/CQRS/ContentBundles/Queries/GetActiveContentBundlesQueryHandler.cs`

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Handler for GetActiveContentBundlesQuery
/// </summary>
public class GetActiveContentBundlesQueryHandler : IQueryHandler<GetActiveContentBundlesQuery, IEnumerable<ContentBundle>>
{
    private readonly IContentBundleRepository _repository;
    private readonly ILogger<GetActiveContentBundlesQueryHandler> _logger;

    public GetActiveContentBundlesQueryHandler(
        IContentBundleRepository repository,
        ILogger<GetActiveContentBundlesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ContentBundle>> Handle(
        GetActiveContentBundlesQuery request,
        CancellationToken cancellationToken)
    {
        // Create specification
        var spec = new ActiveContentBundlesWithScenariosSpecification();

        // Execute query using specification
        var bundles = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} active content bundles", bundles.Count());

        return bundles;
    }
}
```

**See how clean the handler is?** All the query logic is in the specification!

---

## Step 4: Create a Parameterized Specification (2 minutes)

Specifications can accept parameters for dynamic filtering.

**Add to**: `Domain/Specifications/ContentBundleSpecifications.cs`

```csharp
/// <summary>
/// Specification for content bundles by age group
/// </summary>
public class ContentBundlesByAgeGroupSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundlesByAgeGroupSpecification(string ageGroup)
        : base(b => b.AgeGroup == ageGroup && !b.IsDeleted)
    {
        AddInclude(b => b.Scenarios);
        ApplyOrderBy(b => b.Title);
    }
}

/// <summary>
/// Specification for content bundles by price range
/// </summary>
public class ContentBundlesByPriceRangeSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundlesByPriceRangeSpecification(decimal minPrice, decimal maxPrice)
        : base(b => b.Price >= minPrice && b.Price <= maxPrice && !b.IsDeleted)
    {
        ApplyOrderBy(b => b.Price);
    }
}

/// <summary>
/// Specification for paginated content bundles
/// </summary>
public class PaginatedContentBundlesSpecification : BaseSpecification<ContentBundle>
{
    public PaginatedContentBundlesSpecification(int pageNumber, int pageSize)
        : base(b => !b.IsDeleted)
    {
        ApplyOrderByDescending(b => b.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
```

**Usage**:
```csharp
// Filter by age group
var spec1 = new ContentBundlesByAgeGroupSpecification("Ages7to9");

// Filter by price range
var spec2 = new ContentBundlesByPriceRangeSpecification(minPrice: 10.00m, maxPrice: 20.00m);

// Paginate results
var spec3 = new PaginatedContentBundlesSpecification(pageNumber: 2, pageSize: 10);

// Execute
var bundles = await _repository.ListAsync(spec1);
```

---

## Bonus: Advanced Specification Features (5 minutes)

### Multiple Includes (Eager Loading)

Load related entities to avoid N+1 query problems.

```csharp
public class ContentBundleWithAllRelationsSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundleWithAllRelationsSpecification(string bundleId)
        : base(b => b.Id == bundleId && !b.IsDeleted)
    {
        // Load multiple related entities
        AddInclude(b => b.Scenarios);
        AddInclude(b => b.Tags);
        AddInclude(b => b.Reviews);
    }
}
```

**Generates**:
```sql
SELECT * FROM ContentBundles cb
LEFT JOIN Scenarios s ON cb.Id = s.ContentBundleId
LEFT JOIN Tags t ON cb.Id = t.ContentBundleId
LEFT JOIN Reviews r ON cb.Id = r.ContentBundleId
WHERE cb.Id = @bundleId AND cb.IsDeleted = 0
```

### Conditional Specifications

Build specifications conditionally based on parameters.

```csharp
public class ContentBundleSearchSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundleSearchSpecification(
        string? ageGroup = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
        : base(b => !b.IsDeleted)
    {
        // Conditional filtering
        if (!string.IsNullOrEmpty(ageGroup))
        {
            Criteria = Criteria.And(b => b.AgeGroup == ageGroup);
        }

        if (minPrice.HasValue)
        {
            Criteria = Criteria.And(b => b.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            Criteria = Criteria.And(b => b.Price <= maxPrice.Value);
        }

        ApplyOrderBy(b => b.Title);
    }
}
```

**Usage**:
```csharp
// All bundles
var spec1 = new ContentBundleSearchSpecification();

// Filter by age group only
var spec2 = new ContentBundleSearchSpecification(ageGroup: "Ages7to9");

// Filter by age group and price
var spec3 = new ContentBundleSearchSpecification(
    ageGroup: "Ages7to9",
    minPrice: 10.00m,
    maxPrice: 20.00m);
```

### Count Specifications

Get the count of entities matching a specification.

```csharp
public class ActiveContentBundlesCountSpecification : BaseSpecification<ContentBundle>
{
    public ActiveContentBundlesCountSpecification()
        : base(b => !b.IsDeleted)
    {
        // No includes, ordering, or paging needed for counting
    }
}
```

**Usage in Repository**:
```csharp
var spec = new ActiveContentBundlesCountSpecification();
var count = await _repository.CountAsync(spec);
```

### Scenario-Specific Specifications

Create specifications for related entities (Scenarios, Purchases, etc.).

**Create**: `Domain/Specifications/ScenarioSpecifications.cs`

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification for scenarios by content bundle
/// </summary>
public class ScenariosByContentBundleSpecification : BaseSpecification<Scenario>
{
    public ScenariosByContentBundleSpecification(string contentBundleId)
        : base(s => s.ContentBundleId == contentBundleId && !s.IsDeleted)
    {
        AddInclude(s => s.ContentBundle);
        ApplyOrderBy(s => s.Order);
    }
}

/// <summary>
/// Specification for active scenarios with user progress
/// </summary>
public class ActiveScenariosWithProgressSpecification : BaseSpecification<Scenario>
{
    public ActiveScenariosWithProgressSpecification(string userId)
        : base(s => !s.IsDeleted)
    {
        AddInclude(s => s.ContentBundle);
        AddInclude(s => s.UserProgress.Where(up => up.UserId == userId));
        ApplyOrderByDescending(s => s.CreatedAt);
    }
}
```

---

## Step 5: Test Your Specification (Bonus - 5 minutes)

Specifications are easy to unit test.

**Create**: `Domain.Tests/Specifications/ContentBundleSpecificationsTests.cs`

```csharp
using Xunit;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Domain.Tests.Specifications;

public class ContentBundleSpecificationsTests
{
    [Fact]
    public void ContentBundlesByAgeGroupSpecification_FiltersByAgeGroup()
    {
        // Arrange
        var bundles = new List<ContentBundle>
        {
            new() { Id = "1", AgeGroup = "Ages7to9", IsDeleted = false },
            new() { Id = "2", AgeGroup = "Ages10to12", IsDeleted = false },
            new() { Id = "3", AgeGroup = "Ages7to9", IsDeleted = true }, // Deleted
        };

        var spec = new ContentBundlesByAgeGroupSpecification("Ages7to9");

        // Act
        var filtered = bundles.AsQueryable().Where(spec.Criteria.Compile());

        // Assert
        Assert.Single(filtered);
        Assert.Equal("1", filtered.First().Id);
    }

    [Fact]
    public void PaginatedContentBundlesSpecification_AppliesPaging()
    {
        // Arrange
        var spec = new PaginatedContentBundlesSpecification(pageNumber: 2, pageSize: 10);

        // Assert
        Assert.NotNull(spec.Skip);
        Assert.NotNull(spec.Take);
        Assert.Equal(10, spec.Skip); // Page 2 = skip first 10
        Assert.Equal(10, spec.Take);  // Take next 10
    }

    [Fact]
    public void ContentBundlesByPriceRangeSpecification_FiltersByPrice()
    {
        // Arrange
        var bundles = new List<ContentBundle>
        {
            new() { Id = "1", Price = 5.00m, IsDeleted = false },
            new() { Id = "2", Price = 15.00m, IsDeleted = false },
            new() { Id = "3", Price = 25.00m, IsDeleted = false },
        };

        var spec = new ContentBundlesByPriceRangeSpecification(minPrice: 10.00m, maxPrice: 20.00m);

        // Act
        var filtered = bundles.AsQueryable().Where(spec.Criteria.Compile());

        // Assert
        Assert.Single(filtered);
        Assert.Equal("2", filtered.First().Id);
    }
}
```

### Testing Best Practices

✅ **Test criteria logic** - Verify filtering works correctly
✅ **Test paging logic** - Verify Skip/Take calculations
✅ **Test ordering** - Verify OrderBy/OrderByDescending
✅ **Use in-memory collections** - No database needed for spec tests

---

## Complete Example: Search with Multiple Filters

**Query**:
```csharp
public record SearchContentBundlesQuery(
    string? AgeGroup,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page,
    int PageSize
) : IQuery<IEnumerable<ContentBundle>>;
```

**Specification**:
```csharp
public class SearchContentBundlesSpecification : BaseSpecification<ContentBundle>
{
    public SearchContentBundlesSpecification(
        string? ageGroup,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize)
        : base(b => !b.IsDeleted)
    {
        // Apply filters conditionally
        if (!string.IsNullOrEmpty(ageGroup))
        {
            Criteria = Criteria.And(b => b.AgeGroup == ageGroup);
        }

        if (minPrice.HasValue)
        {
            Criteria = Criteria.And(b => b.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            Criteria = Criteria.And(b => b.Price <= maxPrice.Value);
        }

        // Always include scenarios
        AddInclude(b => b.Scenarios);

        // Sort by relevance (price, then title)
        ApplyOrderBy(b => b.Price);
        ThenBy(b => b.Title);

        // Apply pagination
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
```

**Handler**:
```csharp
public class SearchContentBundlesQueryHandler
    : IQueryHandler<SearchContentBundlesQuery, IEnumerable<ContentBundle>>
{
    private readonly IContentBundleRepository _repository;

    public async Task<IEnumerable<ContentBundle>> Handle(
        SearchContentBundlesQuery request,
        CancellationToken cancellationToken)
    {
        var spec = new SearchContentBundlesSpecification(
            request.AgeGroup,
            request.MinPrice,
            request.MaxPrice,
            request.Page,
            request.PageSize);

        return await _repository.ListAsync(spec);
    }
}
```

**Controller**:
```csharp
[HttpGet("search")]
public async Task<IActionResult> Search(
    [FromQuery] string? ageGroup,
    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    var query = new SearchContentBundlesQuery(
        ageGroup, minPrice, maxPrice, page, pageSize);

    var bundles = await _mediator.Send(query);

    return Ok(bundles);
}
```

**Usage**:
```http
GET /api/content-bundles/search?ageGroup=Ages7to9&minPrice=10&maxPrice=20&page=1&pageSize=10
```

---

## Checklist

Before moving on, make sure you have:

- [ ] Understood `BaseSpecification<T>` and its methods
- [ ] Created at least one specification
- [ ] Used the specification in a query handler
- [ ] Created a parameterized specification
- [ ] (Optional) Created specifications with multiple includes
- [ ] (Optional) Created conditional specifications
- [ ] (Optional) Written unit tests for specifications

---

## Common Mistakes to Avoid

❌ **Putting business logic in specifications** - Specs are for queries only
❌ **Not using specifications** - Don't write LINQ directly in handlers
❌ **Creating one-off specifications** - Only create specs you'll reuse
❌ **Over-complicating specifications** - Keep them simple and focused
❌ **Forgetting soft delete filter** - Always include `!b.IsDeleted`
❌ **Not testing specifications** - Test complex query logic

---

## Specification Pattern Quick Reference

| Feature | Purpose | Example |
|---------|---------|---------|
| **Criteria** | WHERE clause | `base(b => b.IsActive)` |
| **AddInclude** | Eager loading (JOIN) | `AddInclude(b => b.Scenarios)` |
| **ApplyOrderBy** | Sort ascending | `ApplyOrderBy(b => b.Title)` |
| **ApplyOrderByDescending** | Sort descending | `ApplyOrderByDescending(b => b.CreatedAt)` |
| **ApplyPaging** | Pagination (OFFSET/LIMIT) | `ApplyPaging(0, 10)` |
| **ApplyGroupBy** | Group results | `ApplyGroupBy(b => b.AgeGroup)` |

---

## When to Create a Specification

✅ **Create a specification when**:
- The query logic will be reused in multiple places
- The query has complex filtering or sorting
- You need to test query logic independently
- You want to compose multiple filters together

❌ **Don't create a specification for**:
- One-off queries used in a single handler
- Trivial queries (e.g., `GetById`)
- Queries that are simpler as direct LINQ

---

## Real-World Specification Examples

### Example 1: Recent Purchases with User Info
```csharp
public class RecentPurchasesWithUserSpecification : BaseSpecification<Purchase>
{
    public RecentPurchasesWithUserSpecification(int days)
        : base(p => p.PurchaseDate >= DateTime.UtcNow.AddDays(-days))
    {
        AddInclude(p => p.User);
        AddInclude(p => p.ContentBundle);
        ApplyOrderByDescending(p => p.PurchaseDate);
    }
}
```

### Example 2: Top-Rated Content Bundles
```csharp
public class TopRatedContentBundlesSpecification : BaseSpecification<ContentBundle>
{
    public TopRatedContentBundlesSpecification(int topCount)
        : base(b => !b.IsDeleted && b.AverageRating >= 4.0)
    {
        AddInclude(b => b.Reviews);
        ApplyOrderByDescending(b => b.AverageRating);
        ApplyPaging(0, topCount);
    }
}
```

### Example 3: User's Incomplete Scenarios
```csharp
public class UserIncompleteeScenariosSpecification : BaseSpecification<UserProgress>
{
    public UserIncompleteScenariosSpecification(string userId)
        : base(up => up.UserId == userId && !up.IsCompleted)
    {
        AddInclude(up => up.Scenario);
        AddInclude(up => up.Scenario.ContentBundle);
        ApplyOrderByDescending(up => up.LastAccessedAt);
    }
}
```

---

## Next Steps

Now that you've mastered specifications, try:

1. **[Creating Your First Command](QUICKSTART_COMMAND.md)** - Write operations
2. **[Creating Your First Query](QUICKSTART_QUERY.md)** - Read operations
3. **[Specification Pattern Documentation](SPECIFICATION_PATTERN.md)** - Deep dive
4. **[CQRS Pattern Documentation](CQRS_PATTERN.md)** - Overall architecture

---

## Getting Help

If you're stuck:

1. **Check existing specifications** - Look in `Domain/Specifications/`
2. **Read Specification docs** - [SPECIFICATION_PATTERN.md](SPECIFICATION_PATTERN.md)
3. **Read BaseSpecification** - `Domain/Specifications/BaseSpecification.cs`
4. **Ask the team** - We're here to help!

---

## Summary

You've learned how to:
- ✅ Create specifications using `BaseSpecification<T>`
- ✅ Use specifications for filtering, sorting, and paging
- ✅ Pass parameters to specifications
- ✅ Eager load related entities with `AddInclude()`
- ✅ Use specifications in query handlers
- ✅ Test specifications with unit tests
- ✅ Compose complex specifications
- ✅ Follow Specification Pattern best practices

**Congratulations!** You've mastered the Specification Pattern. 🎉

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
