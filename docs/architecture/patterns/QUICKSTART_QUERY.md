# Quick Start: Creating Your First Query

**Estimated Time**: 8 minutes
**Difficulty**: Beginner
**Pattern**: [CQRS Pattern](CQRS_PATTERN.md)

---

## What You'll Build

By the end of this guide, you'll have created a complete query that:
- ✅ Retrieves content bundles by age group
- ✅ Uses the Specification Pattern for filtering
- ✅ Returns filtered, sorted results
- ✅ Is cacheable and performant
- ✅ Is fully testable

---

## Prerequisites

- ✅ MediatR is installed (already done in this project)
- ✅ Completed [Creating Your First Command](QUICKSTART_COMMAND.md) (recommended)
- ✅ Basic understanding of LINQ and async/await

---

## Step 1: Create the Query (2 minutes)

Queries are **records** that represent a read operation. They're immutable and contain the parameters needed to retrieve data.

**Create**: `Application/CQRS/ContentBundles/Queries/GetContentBundlesByAgeGroupQuery.cs`

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Query to retrieve content bundles by age group (read operation)
/// </summary>
public record GetContentBundlesByAgeGroupQuery(string AgeGroup) : IQuery<IEnumerable<ContentBundle>>;
```

**That's it!** A query is just a data container. The logic goes in the handler.

### Query Naming Convention

✅ **Use noun-based names**: `GetContentBundlesByAgeGroupQuery`
✅ **Start with `Get`**: Clearly indicates it's a read operation
✅ **End with `Query`**: Distinguishes from commands
✅ **Use `record`**: Queries should be immutable

---

## Step 2: Create the Query Handler (4 minutes)

The handler contains the logic to retrieve data. Queries often use the Specification Pattern for complex filtering.

**Create**: `Application/CQRS/ContentBundles/Queries/GetContentBundlesByAgeGroupQueryHandler.cs`

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.ContentBundles.Queries;

/// <summary>
/// Handler for GetContentBundlesByAgeGroupQuery
/// Retrieves content bundles filtered by age group
/// </summary>
public class GetContentBundlesByAgeGroupQueryHandler : IQueryHandler<GetContentBundlesByAgeGroupQuery, IEnumerable<ContentBundle>>
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

    public async Task<IEnumerable<ContentBundle>> Handle(GetContentBundlesByAgeGroupQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.AgeGroup))
            throw new ArgumentException("Age group cannot be null or empty", nameof(request.AgeGroup));

        // 2. Create specification (encapsulates query logic)
        var spec = new ContentBundlesByAgeGroupSpecification(request.AgeGroup);

        // 3. Query repository using specification
        var bundles = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} content bundles for age group: {AgeGroup}",
            bundles.Count(), request.AgeGroup);

        // 4. Return results
        return bundles;
    }
}
```

### Handler Best Practices

✅ **Inject Repository + Logger** - No UnitOfWork needed (read-only!)
✅ **Use Specifications** - For complex queries (see Step 3)
✅ **Validate parameters** - Early validation prevents errors
✅ **Log results** - Debug-level logging for diagnostics
✅ **Never modify state** - Queries are read-only

---

## Step 3: Create the Specification (3 minutes)

Specifications encapsulate query logic and make it reusable.

**Create**: `Domain/Specifications/ContentBundleSpecifications.cs`

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

/// <summary>
/// Specification for content bundles by age group
/// </summary>
public class ContentBundlesByAgeGroupSpecification : BaseSpecification<ContentBundle>
{
    public ContentBundlesByAgeGroupSpecification(string ageGroup)
        : base(b => b.AgeGroup == ageGroup && !b.IsDeleted)  // WHERE clause
    {
        ApplyOrderBy(b => b.Title);  // ORDER BY Title ASC
    }
}
```

**What this does**:
- **Filters**: Only bundles matching the age group
- **Excludes deleted**: Soft-delete filtering
- **Sorts**: Alphabetically by title

### Specification Benefits

✅ **Reusable**: Use in multiple queries
✅ **Testable**: Test query logic independently
✅ **Composable**: Combine multiple specifications
✅ **Maintainable**: Query logic in one place

**Learn more**: [Creating Your First Specification](QUICKSTART_SPECIFICATION.md)

---

## Step 4: Use the Query in a Controller (2 minutes)

Controllers dispatch queries using `IMediator`.

**Update**: `Api/Controllers/ContentBundlesController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.ContentBundles.Queries;

[ApiController]
[Route("api/content-bundles")]
public class ContentBundlesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentBundlesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get content bundles by age group
    /// </summary>
    /// <param name="ageGroup">Age group (e.g., "Ages7to9")</param>
    [HttpGet("age-group/{ageGroup}")]
    public async Task<IActionResult> GetByAgeGroup(string ageGroup)
    {
        // Create query
        var query = new GetContentBundlesByAgeGroupQuery(ageGroup);

        // Send to mediator (routes to handler)
        var bundles = await _mediator.Send(query);

        // Return 200 OK with results
        return Ok(bundles);
    }
}
```

### Controller Best Practices

✅ **Use route parameters** - For filtering (ageGroup)
✅ **Use query strings** - For pagination (page, size)
✅ **Return Ok(data)** - For successful queries
✅ **Add XML comments** - For Swagger documentation

---

## Step 5: Test Your Query (Bonus - 5 minutes)

Queries are easy to unit test.

**Create**: `Application.Tests/CQRS/ContentBundles/Queries/GetContentBundlesByAgeGroupQueryHandlerTests.cs`

```csharp
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.ContentBundles.Queries;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.Tests.CQRS.ContentBundles.Queries;

public class GetContentBundlesByAgeGroupQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithValidAgeGroup_ReturnsBundles()
    {
        // Arrange
        var mockRepo = new Mock<IContentBundleRepository>();
        var mockLogger = new Mock<ILogger<GetContentBundlesByAgeGroupQueryHandler>>();

        var expectedBundles = new List<ContentBundle>
        {
            new() { Id = "1", Title = "Bundle 1", AgeGroup = "Ages7to9" },
            new() { Id = "2", Title = "Bundle 2", AgeGroup = "Ages7to9" }
        };

        mockRepo.Setup(r => r.ListAsync(It.IsAny<ContentBundlesByAgeGroupSpecification>()))
            .ReturnsAsync(expectedBundles);

        var handler = new GetContentBundlesByAgeGroupQueryHandler(
            mockRepo.Object,
            mockLogger.Object);

        var query = new GetContentBundlesByAgeGroupQuery("Ages7to9");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal("Ages7to9", b.AgeGroup));
    }

    [Fact]
    public async Task Handle_WithEmptyAgeGroup_ThrowsArgumentException()
    {
        // Arrange
        var mockRepo = new Mock<IContentBundleRepository>();
        var mockLogger = new Mock<ILogger<GetContentBundlesByAgeGroupQueryHandler>>();

        var handler = new GetContentBundlesByAgeGroupQueryHandler(
            mockRepo.Object,
            mockLogger.Object);

        var query = new GetContentBundlesByAgeGroupQuery(""); // Invalid

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(query, CancellationToken.None));
    }
}
```

### Testing Best Practices

✅ **Mock repository** - Return test data
✅ **Test success path** - Verify correct data returned
✅ **Test validation** - Verify exceptions for invalid input
✅ **Test empty results** - Verify handling of no results

---

## Complete Example Flow

**1. User makes HTTP GET request**:
```http
GET /api/content-bundles/age-group/Ages7to9
```

**2. Controller receives request**:
```csharp
var query = new GetContentBundlesByAgeGroupQuery(ageGroup);
var bundles = await _mediator.Send(query);
```

**3. MediatR routes to handler**:
```
IMediator.Send() → GetContentBundlesByAgeGroupQueryHandler.Handle()
```

**4. Handler executes**:
```csharp
// Validate
// Create specification
// Query repository
// Return results
```

**5. Controller returns response**:
```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": "bundle-1",
    "title": "Dragon Adventures Bundle",
    "ageGroup": "Ages7to9",
    "price": 14.99
  },
  {
    "id": "bundle-2",
    "title": "Unicorn Quest Bundle",
    "ageGroup": "Ages7to9",
    "price": 12.99
  }
]
```

---

## Bonus: Add Pagination

For large result sets, add pagination using a Specification.

**Create**: `Domain/Specifications/ContentBundleSpecifications.cs`

```csharp
/// <summary>
/// Specification for paginated content bundles
/// </summary>
public class PaginatedContentBundlesSpecification : BaseSpecification<ContentBundle>
{
    public PaginatedContentBundlesSpecification(int pageNumber, int pageSize)
        : base(b => !b.IsDeleted)
    {
        ApplyOrderByDescending(b => b.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);  // OFFSET/LIMIT
    }
}
```

**Create**: `Application/CQRS/ContentBundles/Queries/GetPaginatedContentBundlesQuery.cs`

```csharp
public record GetPaginatedContentBundlesQuery(int PageNumber, int PageSize) : IQuery<IEnumerable<ContentBundle>>;
```

**Handler**:

```csharp
public async Task<IEnumerable<ContentBundle>> Handle(GetPaginatedContentBundlesQuery request, CancellationToken cancellationToken)
{
    var spec = new PaginatedContentBundlesSpecification(request.PageNumber, request.PageSize);
    return await _repository.ListAsync(spec);
}
```

**Controller**:

```csharp
[HttpGet]
public async Task<IActionResult> GetBundles([FromQuery] int page = 1, [FromQuery] int size = 10)
{
    var query = new GetPaginatedContentBundlesQuery(page, size);
    var bundles = await _mediator.Send(query);
    return Ok(bundles);
}
```

**Usage**:
```http
GET /api/content-bundles?page=2&size=20
```

---

## Checklist

Before moving on, make sure you have:

- [ ] Created the query record (`GetContentBundlesByAgeGroupQuery`)
- [ ] Created the query handler (`GetContentBundlesByAgeGroupQueryHandler`)
- [ ] Created the specification (`ContentBundlesByAgeGroupSpecification`)
- [ ] Injected dependencies (Repository, Logger)
- [ ] Implemented validation
- [ ] Updated the controller to use `IMediator.Send()`
- [ ] (Optional) Created unit tests
- [ ] (Bonus) Added pagination support

---

## Common Mistakes to Avoid

❌ **Modifying state in queries** - Queries are read-only!
❌ **Injecting UnitOfWork** - Not needed for read operations
❌ **Querying without specifications** - Use specs for reusability
❌ **Not logging results** - Logs help with debugging
❌ **Returning null** - Return empty collection instead

---

## Query vs Command Quick Reference

| Aspect | **Query** | **Command** |
|--------|-----------|-------------|
| **Purpose** | Read data | Write/modify data |
| **Returns** | Data (entities, DTOs) | Entity or void |
| **Modifies State** | ❌ No | ✅ Yes |
| **Cacheable** | ✅ Yes | ❌ No |
| **Needs UnitOfWork** | ❌ No | ✅ Yes |
| **HTTP Method** | GET | POST, PUT, DELETE |
| **Example** | `GetContentBundlesQuery` | `CreateContentBundleCommand` |

---

## Next Steps

Now that you've created a query, try:

1. **[Creating Your First Specification](QUICKSTART_SPECIFICATION.md)** - Advanced query patterns
2. **[Creating Your First Command](QUICKSTART_COMMAND.md)** - Write operations
3. **[CQRS Pattern Documentation](CQRS_PATTERN.md)** - Deep dive into CQRS

---

## Getting Help

If you're stuck:

1. **Check existing queries** - Look at `GetScenarioQuery` for examples
2. **Read CQRS docs** - [CQRS_PATTERN.md](CQRS_PATTERN.md)
3. **Read Specification docs** - [SPECIFICATION_PATTERN.md](SPECIFICATION_PATTERN.md)
4. **Ask the team** - We're here to help!

---

## Summary

You've learned how to:
- ✅ Create a query using `record` syntax
- ✅ Implement a query handler for read operations
- ✅ Use specifications for filtering
- ✅ Use queries in controllers via `IMediator`
- ✅ Test queries with unit tests
- ✅ Add pagination to queries
- ✅ Follow CQRS best practices

**Congratulations!** You've created your first CQRS query. 🎉

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
