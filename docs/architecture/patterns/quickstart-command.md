# Quick Start: Creating Your First Command

**Estimated Time**: 10 minutes
**Difficulty**: Beginner
**Pattern**: [CQRS Pattern](CQRS_PATTERN.md)

---

## What You'll Build

By the end of this guide, you'll have created a complete command that:
- ✅ Creates a new Content Bundle
- ✅ Validates the input
- ✅ Persists to the database
- ✅ Returns the created bundle
- ✅ Is fully testable

---

## Prerequisites

- ✅ MediatR is installed (already done in this project)
- ✅ Basic understanding of async/await in C#
- ✅ Familiarity with dependency injection

---

## Step 1: Create the Command (2 minutes)

Commands are **records** that represent a write operation. They're immutable and contain all the data needed to perform the operation.

**Create**: `Application/CQRS/ContentBundles/Commands/CreateContentBundleCommand.cs`

```csharp
using Mystira.Contracts.App.Requests.ContentBundles;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Commands;

/// <summary>
/// Command to create a new content bundle (write operation)
/// </summary>
public record CreateContentBundleCommand(CreateContentBundleRequest Request) : ICommand<ContentBundle>;
```

**That's it!** A command is just a data container. The logic goes in the handler.

### Command Naming Convention

✅ **Use verb-based names**: `CreateContentBundleCommand`
✅ **End with `Command`**: Clearly indicates it's a write operation
✅ **Use `record`**: Commands should be immutable

---

## Step 2: Create the Command Handler (5 minutes)

The handler contains the business logic to execute the command.

**Create**: `Application/CQRS/ContentBundles/Commands/CreateContentBundleCommandHandler.cs`

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.ContentBundles.Commands;

/// <summary>
/// Handler for CreateContentBundleCommand
/// Executes the business logic to create a content bundle
/// </summary>
public class CreateContentBundleCommandHandler : ICommandHandler<CreateContentBundleCommand, ContentBundle>
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

    public async Task<ContentBundle> Handle(CreateContentBundleCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required", nameof(request.Title));

        // 2. Create domain entity
        var bundle = new ContentBundle
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            AgeGroup = request.AgeGroup,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        // 3. Persist to database
        await _repository.AddAsync(bundle);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving content bundle: {BundleId}", bundle.Id);
            throw;
        }

        _logger.LogInformation("Created content bundle: {BundleId} - {Title}", bundle.Id, bundle.Title);

        // 4. Return the created entity
        return bundle;
    }
}
```

### Handler Best Practices

✅ **Inject dependencies** - Repository, UnitOfWork, Logger
✅ **Validate input** - Early validation prevents invalid state
✅ **Log operations** - Info for success, Error for failures
✅ **Use UnitOfWork** - Ensures transactional consistency
✅ **Handle exceptions** - Catch and log database errors

---

## Step 3: Use the Command in a Controller (2 minutes)

Controllers dispatch commands using `IMediator`.

**Update**: `Api/Controllers/ContentBundlesController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.ContentBundles.Commands;
using Mystira.Contracts.App.Requests.ContentBundles;

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
    /// Create a new content bundle
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContentBundleRequest request)
    {
        // Create command
        var command = new CreateContentBundleCommand(request);

        // Send to mediator (routes to handler)
        var bundle = await _mediator.Send(command);

        // Return 201 Created with location header
        return CreatedAtAction(nameof(Get), new { id = bundle.Id }, bundle);
    }

    /// <summary>
    /// Get content bundle by ID (needed for CreatedAtAction)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        // Implementation here (use GetContentBundleQuery)
        return Ok();
    }
}
```

### Controller Best Practices

✅ **Inject `IMediator`** - Not individual handlers
✅ **Keep controllers thin** - Just route to commands/queries
✅ **Use CreatedAtAction** - For HTTP 201 responses
✅ **Add XML comments** - For Swagger documentation

---

## Step 4: Register with Dependency Injection (1 minute)

MediatR automatically discovers handlers, but ensure MediatR is registered.

**Check**: `Api/Program.cs` or `Admin.Api/Program.cs`

```csharp
// MediatR registration (should already be there)
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateContentBundleCommand).Assembly);
});

// Repositories and UnitOfWork (should already be there)
builder.Services.AddScoped<IContentBundleRepository, ContentBundleRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

If MediatR is already registered, you're done! Handlers are auto-discovered.

---

## Step 5: Test Your Command (Bonus - 5 minutes)

Commands are easy to unit test.

**Create**: `Application.Tests/CQRS/ContentBundles/Commands/CreateContentBundleCommandHandlerTests.cs`

```csharp
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Mystira.App.Application.CQRS.ContentBundles.Commands;
using Mystira.App.Application.Ports.Data;
using Mystira.Contracts.App.Requests.ContentBundles;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.Tests.CQRS.ContentBundles.Commands;

public class CreateContentBundleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CreatesBundle()
    {
        // Arrange
        var mockRepo = new Mock<IContentBundleRepository>();
        var mockUoW = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<CreateContentBundleCommandHandler>>();

        var handler = new CreateContentBundleCommandHandler(
            mockRepo.Object,
            mockUoW.Object,
            mockLogger.Object);

        var command = new CreateContentBundleCommand(new CreateContentBundleRequest
        {
            Title = "Test Bundle",
            Description = "Test Description",
            AgeGroup = "Ages7to9",
            Price = 9.99m
        });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Bundle", result.Title);
        Assert.Equal(9.99m, result.Price);

        mockRepo.Verify(r => r.AddAsync(It.IsAny<ContentBundle>()), Times.Once);
        mockUoW.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var mockRepo = new Mock<IContentBundleRepository>();
        var mockUoW = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<CreateContentBundleCommandHandler>>();

        var handler = new CreateContentBundleCommandHandler(
            mockRepo.Object,
            mockUoW.Object,
            mockLogger.Object);

        var command = new CreateContentBundleCommand(new CreateContentBundleRequest
        {
            Title = "", // Invalid
            Description = "Test"
        });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
```

### Testing Best Practices

✅ **Mock dependencies** - Repository, UnitOfWork, Logger
✅ **Test success path** - Verify entity is created and saved
✅ **Test validation** - Verify exceptions for invalid input
✅ **Verify interactions** - Use `Verify()` to ensure methods were called

---

## Complete Example Flow

**1. User makes HTTP POST request**:
```http
POST /api/content-bundles
Content-Type: application/json

{
  "title": "Dragon Adventures Bundle",
  "description": "5 epic dragon scenarios",
  "ageGroup": "Ages7to9",
  "price": 14.99
}
```

**2. Controller receives request**:
```csharp
var command = new CreateContentBundleCommand(request);
var bundle = await _mediator.Send(command);
```

**3. MediatR routes to handler**:
```
IMediator.Send() → CreateContentBundleCommandHandler.Handle()
```

**4. Handler executes**:
```csharp
// Validate
// Create entity
// Save to database
// Return entity
```

**5. Controller returns response**:
```http
HTTP/1.1 201 Created
Location: /api/content-bundles/abc123

{
  "id": "abc123",
  "title": "Dragon Adventures Bundle",
  "description": "5 epic dragon scenarios",
  "ageGroup": "Ages7to9",
  "price": 14.99,
  "createdAt": "2025-11-24T10:30:00Z"
}
```

---

## Checklist

Before moving on, make sure you have:

- [ ] Created the command record (`CreateContentBundleCommand`)
- [ ] Created the command handler (`CreateContentBundleCommandHandler`)
- [ ] Injected dependencies (Repository, UnitOfWork, Logger)
- [ ] Implemented validation
- [ ] Implemented persistence logic
- [ ] Updated the controller to use `IMediator.Send()`
- [ ] Verified MediatR registration in `Program.cs`
- [ ] (Optional) Created unit tests

---

## Common Mistakes to Avoid

❌ **Putting logic in the command** - Commands are just data containers
❌ **Not using UnitOfWork** - Always wrap SaveChanges in try-catch
❌ **Forgetting to log** - Logs are essential for debugging
❌ **Not validating input** - Validate early to prevent invalid state
❌ **Injecting handlers directly** - Always use `IMediator`

---

## Next Steps

Now that you've created a command, try:

1. **[Creating Your First Query](QUICKSTART_QUERY.md)** - Learn read operations
2. **[Creating Your First Specification](QUICKSTART_SPECIFICATION.md)** - Advanced queries
3. **[CQRS Pattern Documentation](CQRS_PATTERN.md)** - Deep dive into CQRS

---

## Getting Help

If you're stuck:

1. **Check existing commands** - Look at `CreateScenarioCommand` for examples
2. **Read CQRS docs** - [CQRS_PATTERN.md](CQRS_PATTERN.md)
3. **Ask the team** - We're here to help!

---

## Summary

You've learned how to:
- ✅ Create a command using `record` syntax
- ✅ Implement a command handler with business logic
- ✅ Use commands in controllers via `IMediator`
- ✅ Test commands with unit tests
- ✅ Follow CQRS best practices

**Congratulations!** You've created your first CQRS command. 🎉

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
