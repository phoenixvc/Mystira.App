# CQRS Pattern (Command Query Responsibility Segregation)

## Status: ✅ IMPLEMENTED

**Implementation Date**: November 2025
**MediatR Version**: 12.4.1
**Related Patterns**: [Specification Pattern](SPECIFICATION_PATTERN.md), [Mediator Pattern](#mediator-pattern)

---

## Overview

CQRS (Command Query Responsibility Segregation) is an architectural pattern that separates **read operations** (Queries) from **write operations** (Commands). This separation provides clear boundaries between state-changing and state-retrieving operations.

**Key Principle**: *"A method should either change state or return data, but not both."* - Bertrand Meyer (Command-Query Separation)

---

## Benefits

✅ **Clear Intent** - Commands modify state, Queries read state
✅ **Scalability** - Optimize read and write paths independently
✅ **Maintainability** - Single Responsibility Principle per handler
✅ **Testability** - Easy to test commands and queries independently
✅ **Flexibility** - Add caching, validation, logging per operation type
✅ **Performance** - Queries can be cached, commands can be queued
✅ **Security** - Different authorization rules for read vs write

---

## Architecture

```
┌─────────────────────────────────────────────────┐
│              API Controller                     │
│  _mediator.Send(command/query)                  │
└──────────────┬──────────────────────────────────┘
               ↓
┌──────────────┴──────────────────────────────────┐
│            MediatR (Mediator)                   │
│  Routes requests to appropriate handlers        │
└──────┬───────────────────────────┬──────────────┘
       ↓                           ↓
┌──────────────────┐     ┌──────────────────────┐
│  Command Handler │     │   Query Handler      │
│  (Write)         │     │   (Read)             │
│  - Validate      │     │   - Retrieve data    │
│  - Execute       │     │   - Apply filters    │
│  - Save changes  │     │   - Format response  │
└─────────┬────────┘     └──────────┬───────────┘
          ↓                         ↓
┌──────────────────┐     ┌──────────────────────┐
│  Write Model     │     │   Read Model         │
│  (Repository)    │     │   (Repository +      │
│  - Add           │     │    Specifications)   │
│  - Update        │     │   - List             │
│  - Delete        │     │   - GetBySpec        │
│  - UnitOfWork    │     │   - Count            │
└──────────────────┘     └──────────────────────┘
```

---

## Implementation

### Project Structure

```
Application/CQRS/
├── ICommand.cs                          # Command marker interface (with result)
├── ICommand.cs                          # Command marker interface (no result)
├── ICommandHandler.cs                   # Command handler interface
├── IQuery.cs                            # Query marker interface
├── IQueryHandler.cs                     # Query handler interface
└── Scenarios/
    ├── Commands/
    │   ├── CreateScenarioCommand.cs     # Command definition
    │   ├── CreateScenarioCommandHandler.cs
    │   ├── DeleteScenarioCommand.cs
    │   └── DeleteScenarioCommandHandler.cs
    └── Queries/
        ├── GetScenarioQuery.cs          # Query definition
        ├── GetScenarioQueryHandler.cs
        ├── GetScenariosQuery.cs
        ├── GetScenariosQueryHandler.cs
        ├── GetScenariosByAgeGroupQuery.cs
        ├── GetScenariosByAgeGroupQueryHandler.cs
        ├── GetPaginatedScenariosQuery.cs
        └── GetPaginatedScenariosQueryHandler.cs
```

### Base Interfaces

#### ICommand<TResponse>

Commands that return a result:

```csharp
using MediatR;

namespace Mystira.App.Application.CQRS;

/// <summary>
/// Marker interface for commands (write operations) that return a result
/// Commands modify state and should be idempotent when possible
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the command</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
```

#### ICommand

Commands with no result (void):

```csharp
/// <summary>
/// Marker interface for commands (write operations) that don't return a result
/// Commands modify state and should be idempotent when possible
/// </summary>
public interface ICommand : IRequest
{
}
```

#### IQuery<TResponse>

Queries (always return data):

```csharp
/// <summary>
/// Marker interface for queries (read operations)
/// Queries should NOT modify state and can be cached
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
```

#### ICommandHandler<TCommand, TResponse>

Command handler interface:

```csharp
/// <summary>
/// Handler for commands (write operations) that return a result
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}
```

#### IQueryHandler<TQuery, TResponse>

Query handler interface:

```csharp
/// <summary>
/// Handler for queries (read operations)
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
```

---

## Commands (Write Operations)

### Characteristics

- **Modify state** - Create, Update, Delete operations
- **Idempotent** - Should produce same result if executed multiple times (when possible)
- **Validated** - Business rule validation before execution
- **Transactional** - Use UnitOfWork for atomic operations
- **Asynchronous** - Support async/await for I/O operations

### Example: CreateScenarioCommand

**Command Definition** (`CreateScenarioCommand.cs`):

```csharp
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Commands;

/// <summary>
/// Command to create a new scenario (write operation)
/// </summary>
public record CreateScenarioCommand(CreateScenarioRequest Request) : ICommand<Scenario>;
```

**Command Handler** (`CreateScenarioCommandHandler.cs`):

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.UseCases.Scenarios;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Commands;

public class CreateScenarioCommandHandler : ICommandHandler<CreateScenarioCommand, Scenario>
{
    private readonly IScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateScenarioCommandHandler> _logger;
    private readonly ValidateScenarioUseCase _validateScenarioUseCase;

    public CreateScenarioCommandHandler(
        IScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateScenarioCommandHandler> logger,
        ValidateScenarioUseCase validateScenarioUseCase)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _validateScenarioUseCase = validateScenarioUseCase;
    }

    public async Task<Scenario> Handle(CreateScenarioCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // 1. Create domain entity
        var scenario = new Scenario
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            AgeGroup = request.AgeGroup,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Validate business rules
        await _validateScenarioUseCase.ExecuteAsync(scenario);

        // 3. Persist changes
        await _repository.AddAsync(scenario);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving scenario: {ScenarioId}", scenario.Id);
            throw;
        }

        _logger.LogInformation("Created new scenario: {ScenarioId} - {Title}", scenario.Id, scenario.Title);
        return scenario;
    }
}
```

### Example: DeleteScenarioCommand

**Command Definition**:

```csharp
public record DeleteScenarioCommand(string ScenarioId) : ICommand;
```

**Command Handler**:

```csharp
public class DeleteScenarioCommandHandler : ICommandHandler<DeleteScenarioCommand>
{
    private readonly IScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteScenarioCommandHandler> _logger;

    public async Task Handle(DeleteScenarioCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ScenarioId))
            throw new ArgumentException("Scenario ID cannot be null or empty");

        var scenario = await _repository.GetByIdAsync(command.ScenarioId);
        if (scenario == null)
            throw new InvalidOperationException($"Scenario not found: {command.ScenarioId}");

        await _repository.DeleteAsync(command.ScenarioId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted scenario: {ScenarioId}", command.ScenarioId);
    }
}
```

---

## Queries (Read Operations)

### Characteristics

- **Read-only** - Do NOT modify state
- **Cacheable** - Can be cached for performance
- **Idempotent** - Always produce same result for same input
- **Fast** - Optimized for retrieval
- **Composable** - Can use Specification Pattern for complex queries

### Example: GetScenarioQuery

**Query Definition** (`GetScenarioQuery.cs`):

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

/// <summary>
/// Query to retrieve a single scenario by ID (read operation)
/// </summary>
public record GetScenarioQuery(string ScenarioId) : IQuery<Scenario?>;
```

**Query Handler** (`GetScenarioQueryHandler.cs`):

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.Scenarios.Queries;

public class GetScenarioQueryHandler : IQueryHandler<GetScenarioQuery, Scenario?>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenarioQueryHandler> _logger;

    public GetScenarioQueryHandler(
        IScenarioRepository repository,
        ILogger<GetScenarioQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Scenario?> Handle(GetScenarioQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioId))
            throw new ArgumentException("Scenario ID cannot be null or empty");

        var scenario = await _repository.GetByIdAsync(request.ScenarioId);

        if (scenario == null)
            _logger.LogWarning("Scenario not found: {ScenarioId}", request.ScenarioId);
        else
            _logger.LogDebug("Retrieved scenario: {ScenarioId}", request.ScenarioId);

        return scenario;
    }
}
```

### Example: Query with Specification

**Query Definition**:

```csharp
public record GetScenariosByAgeGroupQuery(string AgeGroup) : IQuery<IEnumerable<Scenario>>;
```

**Query Handler** (uses Specification Pattern):

```csharp
public class GetScenariosByAgeGroupQueryHandler : IQueryHandler<GetScenariosByAgeGroupQuery, IEnumerable<Scenario>>
{
    private readonly IScenarioRepository _repository;
    private readonly ILogger<GetScenariosByAgeGroupQueryHandler> _logger;

    public async Task<IEnumerable<Scenario>> Handle(GetScenariosByAgeGroupQuery request, CancellationToken cancellationToken)
    {
        // Create specification (Domain layer)
        var spec = new ScenariosByAgeGroupSpecification(request.AgeGroup);

        // Use specification to query repository
        var scenarios = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} scenarios for age group: {AgeGroup}",
            scenarios.Count(), request.AgeGroup);

        return scenarios;
    }
}
```

---

## Controller Integration

### Using Commands and Queries from Controllers

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Mystira.App.Application.CQRS.Scenarios.Commands;
using Mystira.App.Application.CQRS.Scenarios.Queries;
using Mystira.App.Contracts.Requests.Scenarios;

[ApiController]
[Route("api/scenarios")]
public class ScenariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScenariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Command (Write) - Create new scenario
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
    {
        var command = new CreateScenarioCommand(request);
        var scenario = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = scenario.Id }, scenario);
    }

    // Query (Read) - Get single scenario
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new GetScenarioQuery(id);
        var scenario = await _mediator.Send(query);
        if (scenario == null) return NotFound();
        return Ok(scenario);
    }

    // Query (Read) - Get scenarios by age group
    [HttpGet("age-group/{ageGroup}")]
    public async Task<IActionResult> GetByAgeGroup(string ageGroup)
    {
        var query = new GetScenariosByAgeGroupQuery(ageGroup);
        var scenarios = await _mediator.Send(query);
        return Ok(scenarios);
    }

    // Query (Read) - Get paginated scenarios
    [HttpGet]
    public async Task<IActionResult> GetScenarios([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var query = new GetPaginatedScenariosQuery(page, size);
        var scenarios = await _mediator.Send(query);
        return Ok(scenarios);
    }

    // Command (Write) - Delete scenario
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var command = new DeleteScenarioCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}
```

---

## MediatR Configuration

### Dependency Injection Setup

**Program.cs** (or **Startup.cs**):

```csharp
// Register MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateScenarioCommand).Assembly);
});

// Register repositories and other services
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## Best Practices

### 1. Naming Conventions

✅ **Commands**: Verb-based, imperative mood
- `CreateScenarioCommand`
- `UpdateScenarioCommand`
- `DeleteScenarioCommand`
- `PublishScenarioCommand`

✅ **Queries**: Noun-based, descriptive
- `GetScenarioQuery`
- `GetScenariosQuery`
- `GetScenariosByAgeGroupQuery`
- `GetPaginatedScenariosQuery`

### 2. Command Design

✅ **Single Responsibility** - One command does one thing
✅ **Immutable** - Use `record` types
✅ **Validation** - Validate in handler, not in command
✅ **Idempotency** - Design for safe retries
✅ **Async** - Always use async/await

### 3. Query Design

✅ **Read-Only** - Never modify state in queries
✅ **Cacheable** - Design queries to be cacheable
✅ **Specification Pattern** - Use for complex filters
✅ **Projection** - Return only needed data
✅ **Performance** - Optimize for retrieval speed

### 4. Handler Design

✅ **Dependency Injection** - Inject repositories, services
✅ **Logging** - Log important operations
✅ **Error Handling** - Proper exception handling
✅ **Transactions** - Use UnitOfWork for atomicity
✅ **Validation** - Business rule validation

---

## Advanced Topics

### Pipeline Behaviors

MediatR supports pipeline behaviors for cross-cutting concerns:

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

### Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

---

## Testing

### Command Handler Tests

```csharp
public class CreateScenarioCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CreatesScenario()
    {
        // Arrange
        var mockRepo = new Mock<IScenarioRepository>();
        var mockUoW = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<CreateScenarioCommandHandler>>();
        var mockValidator = new Mock<ValidateScenarioUseCase>();

        var handler = new CreateScenarioCommandHandler(
            mockRepo.Object,
            mockUoW.Object,
            mockLogger.Object,
            mockValidator.Object);

        var command = new CreateScenarioCommand(new CreateScenarioRequest
        {
            Title = "Test Scenario",
            Description = "Test Description",
            AgeGroup = "Ages7to9"
        });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Scenario", result.Title);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Scenario>()), Times.Once);
        mockUoW.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Query Handler Tests

```csharp
public class GetScenarioQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingId_ReturnsScenario()
    {
        // Arrange
        var mockRepo = new Mock<IScenarioRepository>();
        var mockLogger = new Mock<ILogger<GetScenarioQueryHandler>>();

        var expectedScenario = new Scenario { Id = "123", Title = "Test" };
        mockRepo.Setup(r => r.GetByIdAsync("123")).ReturnsAsync(expectedScenario);

        var handler = new GetScenarioQueryHandler(mockRepo.Object, mockLogger.Object);
        var query = new GetScenarioQuery("123");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123", result.Id);
        Assert.Equal("Test", result.Title);
    }
}
```

---

## Migration from Use Cases

### Before (Use Case Pattern)

```csharp
public class CreateScenarioUseCase
{
    public async Task<Scenario> ExecuteAsync(CreateScenarioRequest request)
    {
        // Business logic here
    }
}

// Controller
public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
{
    var scenario = await _createScenarioUseCase.ExecuteAsync(request);
    return Ok(scenario);
}
```

### After (CQRS Pattern)

```csharp
public class CreateScenarioCommandHandler : ICommandHandler<CreateScenarioCommand, Scenario>
{
    public async Task<Scenario> Handle(CreateScenarioCommand command, CancellationToken cancellationToken)
    {
        // Business logic here
    }
}

// Controller
public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
{
    var command = new CreateScenarioCommand(request);
    var scenario = await _mediator.Send(command);
    return Ok(scenario);
}
```

---

## Related Patterns

- **[Specification Pattern](SPECIFICATION_PATTERN.md)** - Reusable query logic
- **[Repository Pattern](REPOSITORY_PATTERN.md)** - Data access abstraction
- **[Unit of Work Pattern](UNIT_OF_WORK_PATTERN.md)** - Transaction management
- **Mediator Pattern** - Request routing (MediatR)

---

## References

- [CQRS Pattern - Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [Command Query Separation - Martin Fowler](https://martinfowler.com/bliki/CommandQuerySeparation.html)
- [MediatR Library - Jimmy Bogard](https://github.com/jbogard/MediatR)
- [CQRS Journey - Microsoft](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10))

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
