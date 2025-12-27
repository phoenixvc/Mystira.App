# CQRS Migration Guide

**Version:** 1.0
**Last Updated:** 2025-11-24
**Author:** Development Team

---

## Table of Contents

1. [Introduction](#introduction)
2. [Prerequisites](#prerequisites)
3. [Migration Process Overview](#migration-process-overview)
4. [Step-by-Step Migration Guide](#step-by-step-migration-guide)
5. [Patterns and Best Practices](#patterns-and-best-practices)
6. [Common Pitfalls and Solutions](#common-pitfalls-and-solutions)
7. [Decision Framework](#decision-framework)
8. [Migration Checklist](#migration-checklist)
9. [Lessons Learned from Phase 5](#lessons-learned-from-phase-5)
10. [References](#references)

---

## Introduction

This guide documents the process of migrating domain entities from a traditional service layer pattern to CQRS (Command Query Responsibility Segregation) with MediatR. It's based on real-world experience migrating 8 entities across the Mystira.App project during Phase 5.

### What You'll Learn

- How to systematically migrate an entity to CQRS
- Patterns that emerged from migrating 104 files
- When to use CQRS vs. when to keep service layer
- Common mistakes and how to avoid them
- Performance and architectural considerations

### Who This Guide Is For

- Developers adding new entities to the system
- Teams migrating existing codebases to CQRS
- Code reviewers evaluating CQRS implementations
- Architects planning CQRS adoption

---

## Prerequisites

Before migrating an entity to CQRS, ensure you have:

### Required Knowledge

- ✅ Understanding of CQRS pattern (read [ADR-0001](adr/ADR-0001-adopt-cqrs-pattern.md))
- ✅ Familiarity with MediatR library (read [ADR-0004](adr/ADR-0004-use-mediatr-for-cqrs.md))
- ✅ Knowledge of Repository Pattern ([REPOSITORY_PATTERN.md](patterns/REPOSITORY_PATTERN.md))
- ✅ Understanding of Specification Pattern ([ADR-0002](adr/ADR-0002-adopt-specification-pattern.md))
- ✅ Hexagonal Architecture principles ([ADR-0003](adr/ADR-0003-adopt-hexagonal-architecture.md))

### Required Infrastructure

- ✅ MediatR v12.4.1+ installed and configured
- ✅ Repository interface defined in Application layer
- ✅ IUnitOfWork interface available
- ✅ BaseSpecification<T> class implemented
- ✅ ICommand<TResult> and IQuery<TResult> interfaces defined

### Quick Check

```bash
# Verify MediatR is installed
dotnet list package | grep MediatR

# Check for required interfaces
grep -r "ICommand" src/Mystira.App.Application/Interfaces/
grep -r "IQuery" src/Mystira.App.Application/Interfaces/
grep -r "IUnitOfWork" src/Mystira.App.Application/Ports/Data/
```

---

## Migration Process Overview

### High-Level Steps

```
1. Analyze Entity
   ↓
2. Create Commands (Write Operations)
   ↓
3. Create Queries (Read Operations)
   ↓
4. Create Specifications (Reusable Query Logic)
   ↓
5. Update Controller
   ↓
6. Test and Validate
   ↓
7. Remove Old Service Layer Code
   ↓
8. Commit and Document
```

### Typical Timeline

Based on Phase 5 experience:

- **Simple Entity** (read-only, 2-3 queries): ~1-2 hours
- **Medium Entity** (CRUD operations): ~3-4 hours
- **Complex Entity** (GameSession, 10+ operations): ~6-8 hours

### Files Created Per Entity

**Average entity migration creates:**
- 3-4 Command files + handlers (6-8 files)
- 2-6 Query files + handlers (4-12 files)
- 1-8 Specification classes
- **Total: ~11-28 files per entity**

---

## Step-by-Step Migration Guide

### Step 1: Analyze the Entity

**Objective:** Understand the entity's operations and dependencies.

#### 1.1 Identify All Operations

Review the existing service and controller to catalog all operations:

```bash
# Find the service interface
grep -r "interface I.*Service" src/Mystira.App.Application/Services/

# Find the controller
find src/Mystira.App.Api/Controllers -name "*Controller.cs"
```

#### 1.2 Categorize Operations

Create a table like this:

| Operation | Type | Complexity | Notes |
|-----------|------|------------|-------|
| CreateEntity | Command | Simple | Standard CRUD |
| UpdateEntity | Command | Simple | Standard CRUD |
| DeleteEntity | Command | Simple | Standard CRUD |
| GetById | Query | Simple | Single entity lookup |
| GetByAccount | Query | Medium | Uses specification |
| GetStatistics | Query | Complex | May need service layer |

#### 1.3 Check for Edge Cases

Ask these questions:

- ❓ Are there operations that span multiple entities?
- ❓ Are there complex aggregations or calculations?
- ❓ Are there operations that return non-domain objects (files, streams)?
- ❓ Are there operations with complex transaction requirements?

**Example from Phase 5:**

```csharp
// MediaController had two types of operations:
// ✅ Metadata query (GetMediaById) - MIGRATED to CQRS
// ❌ File streaming (GetMediaFile) - KEPT in service layer (returns binary stream)
```

### Step 2: Create Commands (Write Operations)

**Objective:** Implement all operations that modify state.

#### 2.1 Create Command File

**Location:** `src/Mystira.App.Application/CQRS/{EntityName}/Commands/{CommandName}Command.cs`

**Template:**

```csharp
using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.{EntityName}.Commands;

public record {CommandName}Command(
    {Parameters}
) : ICommand<{ReturnType}>;
```

**Example - CreateUserProfileCommand:**

```csharp
using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserProfiles.Commands;

public record CreateUserProfileCommand(CreateUserProfileRequest Request)
    : ICommand<UserProfile>;
```

#### 2.2 Create Command Handler

**Location:** `src/Mystira.App.Application/CQRS/{EntityName}/Commands/{CommandName}CommandHandler.cs`

**Template:**

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.{EntityName}.Commands;

public class {CommandName}CommandHandler
    : ICommandHandler<{CommandName}Command, {ReturnType}>
{
    private readonly I{EntityName}Repository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<{CommandName}CommandHandler> _logger;

    public {CommandName}CommandHandler(
        I{EntityName}Repository repository,
        IUnitOfWork unitOfWork,
        ILogger<{CommandName}CommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<{ReturnType}> Handle(
        {CommandName}Command command,
        CancellationToken cancellationToken)
    {
        // 1. Validate input
        // 2. Create/update domain entity
        // 3. Call repository
        // 4. Commit transaction
        // 5. Log and return
    }
}
```

**Real Example - AwardBadgeCommandHandler:**

```csharp
public async Task<UserBadge> Handle(
    AwardBadgeCommand command,
    CancellationToken cancellationToken)
{
    var request = command.Request;

    // 1. Validate
    if (string.IsNullOrEmpty(request.UserProfileId))
        throw new ArgumentException("UserProfileId is required");
    if (string.IsNullOrEmpty(request.BadgeConfigurationId))
        throw new ArgumentException("BadgeConfigurationId is required");

    // 2. Create entity
    var badge = new UserBadge
    {
        Id = Guid.NewGuid().ToString(),
        UserProfileId = request.UserProfileId,
        BadgeConfigurationId = request.BadgeConfigurationId,
        Axis = request.Axis,
        EarnedAt = DateTime.UtcNow
    };

    // 3. Repository call
    await _repository.AddAsync(badge);

    // 4. Commit transaction
    await _unitOfWork.CommitAsync(cancellationToken);

    // 5. Log and return
    _logger.LogInformation(
        "Awarded badge {BadgeId} to user profile {UserProfileId}",
        badge.Id, request.UserProfileId);

    return badge;
}
```

#### 2.3 Common Command Patterns

**Pattern 1: Create with Validation**

```csharp
// Check for duplicates before creating
var existing = await _repository.GetByEmailAsync(command.Email);
if (existing != null)
    throw new InvalidOperationException($"Account with email {command.Email} already exists");
```

**Pattern 2: Update with Not Found Check**

```csharp
var entity = await _repository.GetByIdAsync(command.Id);
if (entity == null)
    throw new NotFoundException($"{EntityName} not found: {command.Id}");

// Apply updates
entity.UpdateProperty(command.NewValue);

await _repository.UpdateAsync(entity);
await _unitOfWork.CommitAsync(cancellationToken);
```

**Pattern 3: Soft Delete**

```csharp
var entity = await _repository.GetByIdAsync(command.Id);
if (entity == null)
    throw new NotFoundException($"{EntityName} not found: {command.Id}");

entity.IsDeleted = true;
entity.DeletedAt = DateTime.UtcNow;

await _repository.UpdateAsync(entity);
await _unitOfWork.CommitAsync(cancellationToken);
```

### Step 3: Create Queries (Read Operations)

**Objective:** Implement all operations that read data without modifying it.

#### 3.1 Create Query File

**Location:** `src/Mystira.App.Application/CQRS/{EntityName}/Queries/{QueryName}Query.cs`

**Template:**

```csharp
using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.{EntityName}.Queries;

public record {QueryName}Query({Parameters}) : IQuery<{ReturnType}>;
```

**Examples:**

```csharp
// Simple ID lookup
public record GetUserProfileQuery(string ProfileId) : IQuery<UserProfile?>;

// List query
public record GetAllScenariosQuery() : IQuery<List<Scenario>>;

// Filtered query
public record GetSessionsByAccountQuery(string AccountId) : IQuery<List<GameSession>>;
```

#### 3.2 Create Query Handler

**Location:** `src/Mystira.App.Application/CQRS/{EntityName}/Queries/{QueryName}QueryHandler.cs`

**Template:**

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.{EntityName}.Queries;

public class {QueryName}QueryHandler
    : IQueryHandler<{QueryName}Query, {ReturnType}>
{
    private readonly I{EntityName}Repository _repository;
    private readonly ILogger<{QueryName}QueryHandler> _logger;

    public {QueryName}QueryHandler(
        I{EntityName}Repository repository,
        ILogger<{QueryName}QueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<{ReturnType}> Handle(
        {QueryName}Query request,
        CancellationToken cancellationToken)
    {
        // 1. Call repository (with or without specification)
        // 2. Log results
        // 3. Return data
    }
}
```

**Real Example - GetUserBadgesQueryHandler:**

```csharp
public async Task<List<UserBadge>> Handle(
    GetUserBadgesQuery request,
    CancellationToken cancellationToken)
{
    // 1. Use specification for filtering
    var spec = new UserBadgesByProfileSpecification(request.UserProfileId);
    var badges = await _repository.ListAsync(spec);

    // 2. Log
    _logger.LogDebug(
        "Retrieved {Count} badges for user profile {UserProfileId}",
        badges.Count(), request.UserProfileId);

    // 3. Return
    return badges.ToList();
}
```

#### 3.3 Query Patterns

**Pattern 1: Simple Get by ID**

```csharp
public async Task<Entity?> Handle(GetEntityQuery request, CancellationToken cancellationToken)
{
    var entity = await _repository.GetByIdAsync(request.Id);

    if (entity == null)
    {
        _logger.LogWarning("Entity not found: {Id}", request.Id);
    }

    return entity;
}
```

**Pattern 2: List All (Use with Caution)**

```csharp
public async Task<List<Entity>> Handle(GetAllEntitiesQuery request, CancellationToken cancellationToken)
{
    var entities = await _repository.ListAsync();

    _logger.LogDebug("Retrieved {Count} entities", entities.Count());

    return entities.ToList();
}
```

**Pattern 3: Filtered List with Specification**

```csharp
public async Task<List<Entity>> Handle(GetEntitiesByFilterQuery request, CancellationToken cancellationToken)
{
    var spec = new EntitiesByFilterSpecification(request.FilterValue);
    var entities = await _repository.ListAsync(spec);

    _logger.LogDebug(
        "Retrieved {Count} entities with filter {Filter}",
        entities.Count(), request.FilterValue);

    return entities.ToList();
}
```

### Step 4: Create Specifications

**Objective:** Encapsulate reusable query logic.

#### 4.1 Create Specification File

**Location:** `src/Mystira.App.Domain/Specifications/{EntityName}Specifications.cs`

**Template:**

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

public class {SpecificationName}Specification : BaseSpecification<{EntityName}>
{
    public {SpecificationName}Specification({Parameters})
        : base({FilterExpression})
    {
        // Optional: Add ordering
        ApplyOrderBy(e => e.SomeProperty);
        ApplyOrderByDescending(e => e.SomeProperty);

        // Optional: Add includes for related entities
        AddInclude(e => e.RelatedEntity);

        // Optional: Add paging
        ApplyPaging(skip, take);
    }
}
```

**Real Examples from Phase 5:**

```csharp
// Simple filter
public class UserBadgesByProfileSpecification : BaseSpecification<UserBadge>
{
    public UserBadgesByProfileSpecification(string userProfileId)
        : base(b => b.UserProfileId == userProfileId)
    {
        ApplyOrderByDescending(b => b.EarnedAt);
    }
}

// Multiple filter conditions
public class UserBadgesByAxisSpecification : BaseSpecification<UserBadge>
{
    public UserBadgesByAxisSpecification(string userProfileId, string axis)
        : base(b => b.UserProfileId == userProfileId && b.Axis == axis)
    {
        ApplyOrderByDescending(b => b.EarnedAt);
    }
}

// Complex filter with status enum
public class InProgressSessionsSpecification : BaseSpecification<GameSession>
{
    public InProgressSessionsSpecification(string accountId)
        : base(s => s.AccountId == accountId &&
                   (s.Status == SessionStatus.InProgress ||
                    s.Status == SessionStatus.Paused))
    {
        ApplyOrderByDescending(s => s.StartTime);
    }
}
```

#### 4.2 Specification Best Practices

**✅ DO:**
- Use descriptive names: `ActiveSessionsSpecification`, not `SessionSpec1`
- Keep specifications focused on a single query purpose
- Use ordering to make results predictable
- Reuse specifications across multiple handlers

**❌ DON'T:**
- Make specifications too generic (hard to understand)
- Include business logic (keep it pure query filtering)
- Fetch unnecessary related entities (performance hit)

#### 4.3 Common Specification Patterns

**Pattern 1: By Foreign Key**

```csharp
public class EntitiesByAccountSpecification : BaseSpecification<Entity>
{
    public EntitiesByAccountSpecification(string accountId)
        : base(e => e.AccountId == accountId)
    {
        ApplyOrderByDescending(e => e.CreatedAt);
    }
}
```

**Pattern 2: By Status/Flag**

```csharp
public class ActiveEntitiesSpecification : BaseSpecification<Entity>
{
    public ActiveEntitiesSpecification()
        : base(e => !e.IsDeleted && e.IsActive)
    {
        ApplyOrderBy(e => e.Name);
    }
}
```

**Pattern 3: By Date Range**

```csharp
public class RecentEntitiesSpecification : BaseSpecification<Entity>
{
    public RecentEntitiesSpecification(int daysAgo = 7)
        : base(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-daysAgo))
    {
        ApplyOrderByDescending(e => e.CreatedAt);
    }
}
```

**Pattern 4: Composite Filter**

```csharp
public class SessionsByAccountAndScenarioSpecification : BaseSpecification<GameSession>
{
    public SessionsByAccountAndScenarioSpecification(string accountId, string scenarioId)
        : base(s => s.AccountId == accountId && s.ScenarioId == scenarioId)
    {
        ApplyOrderByDescending(s => s.StartTime);
        AddInclude(s => s.Scenario); // Include related data
    }
}
```

### Step 5: Update Controller

**Objective:** Replace service layer calls with MediatR.

#### 5.1 Update Constructor

**Before (Service Layer):**

```csharp
public GameSessionsController(
    IGameSessionApiService sessionService,
    ILogger<GameSessionsController> logger)
{
    _sessionService = sessionService;
    _logger = logger;
}
```

**After (CQRS with MediatR):**

```csharp
public GameSessionsController(
    IMediator mediator,
    ILogger<GameSessionsController> logger)
{
    _mediator = mediator;
    _logger = logger;
}
```

#### 5.2 Update Endpoints

**Pattern 1: POST (Create)**

```csharp
// Before
[HttpPost]
public async Task<ActionResult<Entity>> Create([FromBody] CreateEntityRequest request)
{
    var entity = await _entityService.CreateAsync(request);
    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
}

// After
[HttpPost]
public async Task<ActionResult<Entity>> Create([FromBody] CreateEntityRequest request)
{
    var command = new CreateEntityCommand(request);
    var entity = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
}
```

**Pattern 2: GET (Read)**

```csharp
// Before
[HttpGet("{id}")]
public async Task<ActionResult<Entity>> GetById(string id)
{
    var entity = await _entityService.GetByIdAsync(id);
    if (entity == null)
        return NotFound();
    return Ok(entity);
}

// After
[HttpGet("{id}")]
public async Task<ActionResult<Entity>> GetById(string id)
{
    var query = new GetEntityQuery(id);
    var entity = await _mediator.Send(query);
    if (entity == null)
        return NotFound();
    return Ok(entity);
}
```

**Pattern 3: PUT (Update)**

```csharp
// Before
[HttpPut("{id}")]
public async Task<ActionResult<Entity>> Update(string id, [FromBody] UpdateEntityRequest request)
{
    var entity = await _entityService.UpdateAsync(id, request);
    return Ok(entity);
}

// After
[HttpPut("{id}")]
public async Task<ActionResult<Entity>> Update(string id, [FromBody] UpdateEntityRequest request)
{
    var command = new UpdateEntityCommand(id, request);
    var entity = await _mediator.Send(command);
    return Ok(entity);
}
```

**Pattern 4: DELETE**

```csharp
// Before
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(string id)
{
    await _entityService.DeleteAsync(id);
    return NoContent();
}

// After
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(string id)
{
    var command = new DeleteEntityCommand(id);
    await _mediator.Send(command);
    return NoContent();
}
```

#### 5.3 Preserve HTTP Concerns in Controller

**Important:** Keep authorization, validation, and HTTP-specific logic in the controller.

**Example from GameSessionsController:**

```csharp
[HttpGet("account/{accountId}/sessions")]
[Authorize]
public async Task<ActionResult<List<GameSession>>> GetByAccount(string accountId)
{
    // HTTP Concern: Extract claims from JWT
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var externalUserId = User.FindFirst("sub")?.Value ?? userIdClaim;

    // HTTP Concern: Authorization check
    if (string.IsNullOrEmpty(externalUserId))
    {
        return Unauthorized(new ErrorResponse
        {
            Message = "User ID not found in token",
            TraceId = HttpContext.TraceIdentifier
        });
    }

    // Business Logic: Delegate to CQRS
    var query = new GetSessionsByAccountQuery(accountId);
    var sessions = await _mediator.Send(query);

    return Ok(sessions);
}
```

### Step 6: Test and Validate

**Objective:** Ensure the migration works correctly.

#### 6.1 Build and Run

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/Mystira.App.Api

# Check for errors in logs
tail -f logs/api.log
```

#### 6.2 Test Each Endpoint

Use your API client (Postman, curl, etc.):

```bash
# Test GET query
curl -X GET "http://localhost:5000/api/entities/123"

# Test POST command
curl -X POST "http://localhost:5000/api/entities" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test", "value": 42}'

# Test PUT command
curl -X PUT "http://localhost:5000/api/entities/123" \
  -H "Content-Type: application/json" \
  -d '{"name": "Updated", "value": 99}'

# Test DELETE command
curl -X DELETE "http://localhost:5000/api/entities/123"
```

#### 6.3 Check MediatR Registration

Verify handlers are auto-discovered:

```bash
# Check MediatR registration in Program.cs or Startup.cs
grep -A 5 "AddMediatR" src/Mystira.App.Api/Program.cs
```

Should look like:

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ICommand<>).Assembly);
});
```

### Step 7: Remove Old Service Layer Code

**Objective:** Clean up deprecated code.

#### 7.1 Identify Service Files

```bash
# Find the service interface
find src/Mystira.App.Application/Services -name "I*Service.cs"

# Find the service implementation
find src/Mystira.App.Infrastructure/Services -name "*Service.cs"
```

#### 7.2 Check for Other Usages

**Before deleting**, ensure no other code uses the service:

```bash
# Search for references
grep -r "IEntityService" src/
```

#### 7.3 Remove Service Files

**Only remove if:**
- ✅ All controller endpoints migrated to MediatR
- ✅ No other services depend on it
- ✅ Tests have been updated

```bash
# Remove service interface
rm src/Mystira.App.Application/Services/IEntityService.cs

# Remove service implementation
rm src/Mystira.App.Infrastructure/Services/EntityService.cs
```

#### 7.4 Update Dependency Injection

Remove service registration from `Program.cs`:

```csharp
// BEFORE
builder.Services.AddScoped<IEntityService, EntityService>();

// AFTER - removed
```

### Step 8: Commit and Document

**Objective:** Record the migration in git history.

#### 8.1 Stage Files

```bash
git add src/Mystira.App.Application/CQRS/{EntityName}/
git add src/Mystira.App.Domain/Specifications/
git add src/Mystira.App.Api/Controllers/{EntityName}Controller.cs

# If you removed service files
git add src/Mystira.App.Application/Services/
git add src/Mystira.App.Infrastructure/Services/
```

#### 8.2 Commit with Descriptive Message

```bash
git commit -m "$(cat <<'EOF'
refactor: Migrate {EntityName} to CQRS pattern

- Add {N} commands with handlers (Create, Update, Delete, etc.)
- Add {N} queries with handlers (GetById, GetByAccount, etc.)
- Add {N} specifications for reusable query logic
- Update {EntityName}Controller to use IMediator
- Remove deprecated I{EntityName}Service and implementation
- Maintain hexagonal architecture compliance (zero App → Infra dependencies)

Part of Phase 5 CQRS migration. See ADR-0006 for details.
EOF
)"
```

#### 8.3 Push to Remote

```bash
git push -u origin claude/add-project-readmes-0164TfHyDcEfm3nKpnPk6osQ
```

---

## Patterns and Best Practices

### Command Handler Patterns

#### 1. Validation First

```csharp
public async Task<Entity> Handle(CreateEntityCommand command, CancellationToken cancellationToken)
{
    // ✅ Validate at the beginning
    if (string.IsNullOrEmpty(command.Name))
        throw new ArgumentException("Name is required");

    if (command.Value < 0)
        throw new ArgumentException("Value must be non-negative");

    // ... rest of handler
}
```

#### 2. Check for Duplicates Before Creating

```csharp
// Check if entity already exists
var existing = await _repository.GetByEmailAsync(command.Email);
if (existing != null)
    throw new InvalidOperationException($"Entity with email '{command.Email}' already exists");
```



```csharp
// ✅ CORRECT
await _repository.AddAsync(entity);
await _unitOfWork.CommitAsync(cancellationToken);

// ❌ WRONG - no transaction management
await _repository.AddAsync(entity);
```

#### 4. Set Timestamps in Handler, Not Domain Model

```csharp
// ✅ CORRECT - Handler controls timestamps
var entity = new Entity
{
    Id = Guid.NewGuid().ToString(),
    Name = command.Name,
    CreatedAt = DateTime.UtcNow,  // Set explicitly
    UpdatedAt = DateTime.UtcNow
};

// ❌ WRONG - Don't rely on database defaults for timestamps
var entity = new Entity
{
    Id = Guid.NewGuid().ToString(),
    Name = command.Name
    // Missing timestamps
};
```

#### 5. Log Important Actions

```csharp
_logger.LogInformation(
    "Created entity {EntityId} with name {Name}",
    entity.Id, entity.Name);
```

### Query Handler Patterns

#### 1. Never Use UnitOfWork in Queries

```csharp
// ✅ CORRECT - Query handler without UnitOfWork
public class GetEntityQueryHandler : IQueryHandler<GetEntityQuery, Entity?>
{
    private readonly IEntityRepository _repository;
    private readonly ILogger<GetEntityQueryHandler> _logger;
    // No IUnitOfWork!

    public GetEntityQueryHandler(
        IEntityRepository repository,
        ILogger<GetEntityQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}

// ❌ WRONG - Query should not have UnitOfWork
public class GetEntityQueryHandler : IQueryHandler<GetEntityQuery, Entity?>
{
    private readonly IUnitOfWork _unitOfWork; // DON'T DO THIS
}
```

#### 2. Use Specifications for Filtering

```csharp
// ✅ CORRECT - Use specification
var spec = new EntitiesByAccountSpecification(request.AccountId);
var entities = await _repository.ListAsync(spec);

// ❌ AVOID - Filtering in handler
var allEntities = await _repository.ListAsync();
var filtered = allEntities.Where(e => e.AccountId == request.AccountId).ToList();
```

#### 3. Return Null for Not Found (Don't Throw)

```csharp
// ✅ CORRECT - Return null, let controller decide response
public async Task<Entity?> Handle(GetEntityQuery request, CancellationToken cancellationToken)
{
    var entity = await _repository.GetByIdAsync(request.Id);

    if (entity == null)
    {
        _logger.LogWarning("Entity not found: {Id}", request.Id);
    }

    return entity; // null is OK
}

// ❌ AVOID - Throwing in query handler
if (entity == null)
    throw new NotFoundException($"Entity not found: {request.Id}");
```

#### 4. Log Query Execution (Debug Level)

```csharp
_logger.LogDebug(
    "Retrieved {Count} entities for account {AccountId}",
    entities.Count(), request.AccountId);
```

### Specification Patterns

#### 1. Use Descriptive Names

```csharp
// ✅ GOOD
ActiveScenariosSpecification
PublishedScenariosSpecification
RecentScenariosSpecification

// ❌ BAD
ScenarioSpec1
ScenarioSpecification
MySpecification
```

#### 2. Apply Consistent Ordering

```csharp
// ✅ GOOD - Predictable ordering
public class RecentScenariosSpecification : BaseSpecification<Scenario>
{
    public RecentScenariosSpecification()
        : base(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-30))
    {
        ApplyOrderByDescending(s => s.CreatedAt); // Most recent first
    }
}
```

#### 3. Keep Specifications Pure (No Side Effects)

```csharp
// ✅ GOOD - Pure filtering logic
public class ActiveScenariosSpecification : BaseSpecification<Scenario>
{
    public ActiveScenariosSpecification()
        : base(s => !s.IsDeleted && s.IsPublished)
    {
        ApplyOrderBy(s => s.Title);
    }
}

// ❌ BAD - Don't modify state in specifications
public class BadSpecification : BaseSpecification<Scenario>
{
    public BadSpecification()
        : base(s => s.IsPublished)
    {
        // DON'T DO THIS
        s.LastAccessedAt = DateTime.UtcNow; // Side effect!
    }
}
```

### Controller Patterns

#### 1. Keep Controllers Thin

```csharp
// ✅ GOOD - Controller only handles HTTP concerns
[HttpPost]
public async Task<ActionResult<Entity>> Create([FromBody] CreateEntityRequest request)
{
    var command = new CreateEntityCommand(request);
    var entity = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
}

// ❌ BAD - Business logic in controller
[HttpPost]
public async Task<ActionResult<Entity>> Create([FromBody] CreateEntityRequest request)
{
    // Validation, creation, repository calls - should be in handler!
    if (string.IsNullOrEmpty(request.Name))
        return BadRequest("Name is required");

    var entity = new Entity { Name = request.Name };
    await _repository.AddAsync(entity);
    await _unitOfWork.CommitAsync();

    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
}
```

#### 2. Handle Not Found Correctly

```csharp
// ✅ GOOD
[HttpGet("{id}")]
public async Task<ActionResult<Entity>> GetById(string id)
{
    var query = new GetEntityQuery(id);
    var entity = await _mediator.Send(query);

    if (entity == null)
    {
        return NotFound(new ErrorResponse
        {
            Message = $"Entity not found: {id}",
            TraceId = HttpContext.TraceIdentifier
        });
    }

    return Ok(entity);
}
```

#### 3. Use Proper HTTP Status Codes

```csharp
// ✅ GOOD - Appropriate status codes
return CreatedAtAction(...);      // 201 Created
return Ok(entity);                 // 200 OK
return NoContent();                // 204 No Content
return NotFound();                 // 404 Not Found
return BadRequest(error);          // 400 Bad Request
return Unauthorized(error);        // 401 Unauthorized
```

---

## Common Pitfalls and Solutions

### Pitfall 1: Using UnitOfWork in Query Handlers

**Problem:**

```csharp
// ❌ WRONG
public class GetEntityQueryHandler : IQueryHandler<GetEntityQuery, Entity?>
{
    private readonly IUnitOfWork _unitOfWork; // Query shouldn't have this!

    public GetEntityQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
}
```

**Solution:**

```csharp
// ✅ CORRECT
public class GetEntityQueryHandler : IQueryHandler<GetEntityQuery, Entity?>
{
    private readonly IEntityRepository _repository;

    public GetEntityQueryHandler(IEntityRepository repository)
    {
        _repository = repository;
    }
}
```

**Why:** Queries are read-only and don't need transaction management. Including `IUnitOfWork` in queries wastes resources and violates CQRS principles.

### Pitfall 2: Not Validating Command Input

**Problem:**

```csharp
// ❌ WRONG - No validation
public async Task<Entity> Handle(CreateEntityCommand command, CancellationToken cancellationToken)
{
    var entity = new Entity
    {
        Id = Guid.NewGuid().ToString(),
        Name = command.Name // What if Name is null or empty?
    };

    await _repository.AddAsync(entity);
    await _unitOfWork.CommitAsync(cancellationToken);

    return entity;
}
```

**Solution:**

```csharp
// ✅ CORRECT - Validate first
public async Task<Entity> Handle(CreateEntityCommand command, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(command.Name))
        throw new ArgumentException("Name is required and cannot be empty");

    if (command.Name.Length > 100)
        throw new ArgumentException("Name cannot exceed 100 characters");

    var entity = new Entity
    {
        Id = Guid.NewGuid().ToString(),
        Name = command.Name.Trim()
    };

    await _repository.AddAsync(entity);
    await _unitOfWork.CommitAsync(cancellationToken);

    return entity;
}
```

### Pitfall 3: Forgetting to Commit Transactions

**Problem:**

```csharp
// ❌ WRONG - No commit!
public async Task<Entity> Handle(CreateEntityCommand command, CancellationToken cancellationToken)
{
    var entity = new Entity { Name = command.Name };
    await _repository.AddAsync(entity);
    // Missing: await _unitOfWork.CommitAsync(cancellationToken);
    return entity;
}
```

**Solution:**

```csharp
// ✅ CORRECT - Always commit
public async Task<Entity> Handle(CreateEntityCommand command, CancellationToken cancellationToken)
{
    var entity = new Entity { Name = command.Name };
    await _repository.AddAsync(entity);
    await _unitOfWork.CommitAsync(cancellationToken); // Don't forget!
    return entity;
}
```

### Pitfall 4: Returning Wrong Types from Controllers

**Problem:**

```csharp
// ❌ WRONG - Returns entity directly (no ActionResult)
[HttpGet("{id}")]
public async Task<Entity> GetById(string id)
{
    var query = new GetEntityQuery(id);
    return await _mediator.Send(query); // What if null? 200 OK with null body!
}
```

**Solution:**

```csharp
// ✅ CORRECT - Return ActionResult<T>
[HttpGet("{id}")]
public async Task<ActionResult<Entity>> GetById(string id)
{
    var query = new GetEntityQuery(id);
    var entity = await _mediator.Send(query);

    if (entity == null)
        return NotFound(); // Proper 404 response

    return Ok(entity); // 200 OK with entity
}
```

### Pitfall 5: Creating Specifications That Are Too Generic

**Problem:**

```csharp
// ❌ WRONG - Too generic, hard to understand
public class GenericFilterSpecification : BaseSpecification<Entity>
{
    public GenericFilterSpecification(
        string? name,
        DateTime? startDate,
        DateTime? endDate,
        bool? isActive,
        string? category)
        : base(e =>
            (name == null || e.Name.Contains(name)) &&
            (startDate == null || e.CreatedAt >= startDate) &&
            (endDate == null || e.CreatedAt <= endDate) &&
            (isActive == null || e.IsActive == isActive) &&
            (category == null || e.Category == category))
    {
    }
}
```

**Solution:**

```csharp
// ✅ CORRECT - Specific, focused specifications
public class EntitiesByNameSpecification : BaseSpecification<Entity>
{
    public EntitiesByNameSpecification(string nameContains)
        : base(e => e.Name.Contains(nameContains))
    {
        ApplyOrderBy(e => e.Name);
    }
}

public class ActiveEntitiesSpecification : BaseSpecification<Entity>
{
    public ActiveEntitiesSpecification()
        : base(e => e.IsActive)
    {
        ApplyOrderBy(e => e.Name);
    }
}

public class EntitiesByDateRangeSpecification : BaseSpecification<Entity>
{
    public EntitiesByDateRangeSpecification(DateTime startDate, DateTime endDate)
        : base(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
    {
        ApplyOrderByDescending(e => e.CreatedAt);
    }
}
```

### Pitfall 6: Not Handling FantasyTheme or Complex Type Conversions

**Problem:**

```csharp
// ❌ WRONG - Type mismatch
var profile = new UserProfile
{
    PreferredFantasyThemes = request.PreferredFantasyThemes // List<string> != List<FantasyTheme>
};
```

**Solution:**

```csharp
// ✅ CORRECT - Convert types
var profile = new UserProfile
{
    PreferredFantasyThemes = request.PreferredFantasyThemes?
        .Select(t => new FantasyTheme(t))
        .ToList() ?? new List<FantasyTheme>()
};
```

### Pitfall 7: Not Using Specifications (Filtering in Memory)

**Problem:**

```csharp
// ❌ WRONG - Loads all entities into memory, then filters
public async Task<List<Entity>> Handle(GetActiveEntitiesQuery request, CancellationToken cancellationToken)
{
    var allEntities = await _repository.ListAsync();
    var activeEntities = allEntities.Where(e => e.IsActive).ToList(); // In-memory filter!
    return activeEntities;
}
```

**Solution:**

```csharp
// ✅ CORRECT - Filter at database level
public async Task<List<Entity>> Handle(GetActiveEntitiesQuery request, CancellationToken cancellationToken)
{
    var spec = new ActiveEntitiesSpecification();
    var activeEntities = await _repository.ListAsync(spec); // Database filter!
    return activeEntities.ToList();
}
```

**Why:** Filtering in memory is inefficient for large datasets. Specifications push filtering to the database level.

### Pitfall 8: Mixing Service Layer and CQRS

**Problem:**

```csharp
// ❌ WRONG - Controller uses both MediatR and service layer
public class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEntityService _entityService; // Mixed approach!

    [HttpPost]
    public async Task<ActionResult<Entity>> Create([FromBody] CreateEntityRequest request)
    {
        var command = new CreateEntityCommand(request);
        var entity = await _mediator.Send(command); // CQRS
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<EntityStats>> GetStats()
    {
        var stats = await _entityService.GetStatsAsync(); // Service layer!
        return Ok(stats);
    }
}
```

**Solution:**

Choose one approach per entity:

```csharp
// ✅ OPTION 1 - Fully migrate to CQRS
public class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<ActionResult<Entity>> Create([FromBody] CreateEntityRequest request)
    {
        var command = new CreateEntityCommand(request);
        var entity = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<EntityStats>> GetStats()
    {
        var query = new GetEntityStatsQuery();
        var stats = await _mediator.Send(query); // Also CQRS
        return Ok(stats);
    }
}

// ✅ OPTION 2 - Keep complex operation in service layer (document why)
public class EntitiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEntityService _entityService; // Only for complex stats

    [HttpPost]
    public async Task<ActionResult<Entity>> Create([FromBody] CreateEntityRequest request)
    {
        var command = new CreateEntityCommand(request);
        var entity = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    // Complex aggregation with cross-entity calculations
    // Kept in service layer per ADR-0006 guidance
    [HttpGet("stats")]
    public async Task<ActionResult<EntityStats>> GetStats()
    {
        var stats = await _entityService.GetComplexStatsAsync();
        return Ok(stats);
    }
}
```

---

## Decision Framework

### When to Use CQRS

Use CQRS for operations that:

✅ **Operate on a single domain entity**
- CreateScenario, UpdateScenario, GetScenarioById

✅ **Have clear read or write semantics**
- Commands modify state, Queries read state

✅ **Benefit from separation of concerns**
- Different scaling for reads vs. writes

✅ **Use repository pattern**
- Operations that call repository methods

✅ **Are simple to medium complexity**
- Standard CRUD, filtered lists, simple validations

### When to Keep Service Layer

Keep service layer for operations that:

❌ **Span multiple entities/aggregates**
- LinkProfilesToAccount (affects Account AND UserProfiles)
- ValidateAccount (checks Account, Subscription, Profiles)

❌ **Return non-domain objects**
- GetMediaFile (returns binary stream, not MediaAsset entity)
- GeneratePDF (returns file, not domain object)

❌ **Have complex orchestration**
- Multi-step workflows with conditional branching
- Operations requiring multiple transactions

❌ **Perform complex calculations/aggregations**
- GetBadgeStatistics (aggregates across UserBadge, BadgeConfiguration, UserProfile)
- GetReportData (joins multiple entities with calculations)

❌ **Are inherently cross-cutting**
- Audit logging that touches multiple entities
- Batch operations across different entity types

### Decision Matrix

| Operation Type | Example | Use CQRS? | Reason |
|----------------|---------|-----------|---------|
| Create single entity | CreateUserProfile | ✅ Yes | Standard command |
| Update single entity | UpdateScenario | ✅ Yes | Standard command |
| Delete single entity | DeleteGameSession | ✅ Yes | Standard command |
| Get by ID | GetAccountById | ✅ Yes | Simple query |
| Get by filter | GetSessionsByAccount | ✅ Yes | Query with specification |
| List all (small set) | GetAllBadgeConfigurations | ✅ Yes | Query (if < 1000 records) |
| Complex aggregation | GetBadgeStatistics | ❌ No | Multi-entity calculation |
| Cross-entity operation | LinkProfilesToAccount | ❌ No | Affects multiple aggregates |
| File/stream operation | GetMediaFile (binary) | ❌ No | Non-domain return type |
| Workflow orchestration | ProcessPaymentAndUpgrade | ❌ No | Multi-step transaction |

### Real Examples from Phase 5

| Entity | Operation | Decision | Reason |
|--------|-----------|----------|---------|
| MediaAsset | GetMediaById | ✅ CQRS | Returns metadata (domain object) |
| MediaAsset | GetMediaFile | ❌ Service | Returns binary stream |
| Account | CreateAccount | ✅ CQRS | Single entity create |
| Account | LinkProfilesToAccount | ❌ Service | Affects Account + UserProfiles |
| UserBadge | AwardBadge | ✅ CQRS | Single entity create |
| UserBadge | GetBadgeStatistics | ❌ Service | Complex multi-entity aggregation |
| GameSession | StartGameSession | ✅ CQRS | Single entity create with validation |
| GameSession | GetSessionStats | ❌ Service | Aggregates session data with calculations |

---

## Migration Checklist

Use this checklist for each entity migration:

### Phase 1: Planning

- [ ] Identify all operations (list endpoints in controller)
- [ ] Categorize as Command or Query
- [ ] Identify edge cases (cross-entity, complex, etc.)
- [ ] Decide what to migrate vs. keep in service layer
- [ ] Estimate file count and complexity

### Phase 2: Commands

- [ ] Create `Commands` folder under `CQRS/{EntityName}/`
- [ ] Create Command classes (e.g., `CreateEntityCommand.cs`)
- [ ] Create CommandHandler classes (e.g., `CreateEntityCommandHandler.cs`)
- [ ] Add validation in handlers
- [ ] Include `IUnitOfWork` in all command handlers
- [ ] Commit after each command creation

### Phase 3: Queries

- [ ] Create `Queries` folder under `CQRS/{EntityName}/`
- [ ] Create Query classes (e.g., `GetEntityQuery.cs`)
- [ ] Create QueryHandler classes (e.g., `GetEntityQueryHandler.cs`)
- [ ] **Do NOT include `IUnitOfWork`** in query handlers
- [ ] Use specifications for filtering

### Phase 4: Specifications

- [ ] Create or update `{EntityName}Specifications.cs` in Domain layer
- [ ] Create specification for each filtered query
- [ ] Add ordering to specifications
- [ ] Test specifications return expected results

### Phase 5: Controller Update

- [ ] Replace service dependency with `IMediator` in constructor
- [ ] Update each endpoint to use `_mediator.Send()`
- [ ] Keep HTTP concerns (auth, status codes) in controller
- [ ] Handle null returns with `NotFound()`
- [ ] Test each endpoint

### Phase 6: Cleanup

- [ ] Remove service interface (`IEntityService`)
- [ ] Remove service implementation (`EntityService`)
- [ ] Remove DI registration for service
- [ ] Search codebase for remaining references
- [ ] Update tests to mock `IMediator` instead of service

### Phase 7: Documentation

- [ ] Commit with descriptive message
- [ ] Push to remote branch
- [ ] Update ADR if necessary
- [ ] Update architecture docs

### Phase 8: Validation

- [ ] Build solution (`dotnet build`)
- [ ] Run API (`dotnet run`)
- [ ] Test all endpoints (Postman, curl, etc.)
- [ ] Check logs for errors
- [ ] Verify data persistence

---

## Lessons Learned from Phase 5

### What Went Well

#### 1. Incremental Migration

**Lesson:** Migrating one entity at a time allowed for:
- Immediate validation of each migration
- Clear commit history
- Easy rollback if needed
- Team learning from early migrations

**Recommendation:** Don't try to migrate all entities at once. Do 1-2 entities, review, then continue.

#### 2. Consistent Folder Structure

**Lesson:** Having a predictable structure made navigation easy:

```
Application/CQRS/{EntityName}/
  Commands/
    {CommandName}Command.cs
    {CommandName}CommandHandler.cs
  Queries/
    {QueryName}Query.cs
    {QueryName}QueryHandler.cs
```

**Recommendation:** Establish folder conventions before starting migration.

#### 3. Specification Pattern for Queries

**Lesson:** Specifications made query logic reusable and testable:
- `UserBadgesByProfileSpecification` used in multiple handlers
- Easy to add new filter combinations
- Database-level filtering (not in-memory)

**Recommendation:** Invest time in creating good specifications early.

#### 4. MediatR Auto-Discovery

**Lesson:** MediatR automatically discovers handlers, no manual registration needed:

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ICommand<>).Assembly);
});
```

**Recommendation:** Trust auto-discovery. Don't manually register handlers.

### Challenges Encountered

#### 1. Type Conversions (FantasyTheme)

**Challenge:** Request DTOs had `List<string>` but domain models needed `List<FantasyTheme>`.

**Solution:**

```csharp
PreferredFantasyThemes = request.PreferredFantasyThemes?
    .Select(t => new FantasyTheme(t))
    .ToList() ?? new List<FantasyTheme>()
```

**Lesson:** Always check for type mismatches between DTOs and domain models.

#### 2. Deciding Service Layer vs. CQRS

**Challenge:** Operations like `LinkProfilesToAccount` and `GetBadgeStatistics` didn't fit CQRS well.

**Solution:** Left complex operations in service layer, documented in ADR-0006.

**Lesson:** CQRS is not a silver bullet. Use service layer for complex orchestration.

#### 3. File Count Explosion

**Challenge:** Phase 5 created 104 new files, which can feel overwhelming.

**Solution:**
- Consistent naming helped IDE search
- Folder grouping by entity kept related files together
- Clear naming (`CreateUserProfileCommand` is self-documenting)

**Lesson:** File count is high, but consistency reduces cognitive load.

#### 4. Remembering to Commit Transactions

**Challenge:** Easy to forget `await _unitOfWork.CommitAsync(cancellationToken)` in command handlers.

**Solution:** Made it part of the code review checklist.

**Lesson:** Use templates and checklists to avoid missing critical steps.

### Metrics from Phase 5

| Metric | Value |
|--------|-------|
| Entities migrated | 8 |
| Commands created | 16 |
| Queries created | 20 |
| Specifications created | 32 |
| Controllers updated | 8 |
| Files created | 104 |
| Total LOC added | ~1,917 |
| Time per entity (avg) | 3-6 hours |
| Commits | 4 major commits |

### Performance Observations

**No significant performance degradation:**
- MediatR adds minimal overhead (<1ms per request)
- Specification pattern pushed filtering to database (better than in-memory)
- Separation of concerns allows future optimization (caching, read replicas)

**Potential optimizations:**
- Add caching to frequently-accessed queries
- Implement query result snapshots for expensive aggregations
- Use read replicas for query handlers

---

## References

### Architecture Decision Records

- [ADR-0001: Adopt CQRS Pattern](adr/ADR-0001-adopt-cqrs-pattern.md)
- [ADR-0002: Adopt Specification Pattern](adr/ADR-0002-adopt-specification-pattern.md)
- [ADR-0003: Adopt Hexagonal Architecture](adr/ADR-0003-adopt-hexagonal-architecture.md)
- [ADR-0004: Use MediatR for CQRS](adr/ADR-0004-use-mediatr-for-cqrs.md)
- [ADR-0006: Phase 5 - Complete CQRS Migration](adr/ADR-0006-phase-5-cqrs-migration.md)

### Quick-Start Guides

- [QUICKSTART_COMMAND.md](QUICKSTART_COMMAND.md) - Creating commands and handlers
- [QUICKSTART_QUERY.md](QUICKSTART_QUERY.md) - Creating queries and handlers
- [QUICKSTART_SPECIFICATION.md](QUICKSTART_SPECIFICATION.md) - Creating specifications

### Pattern Documentation

- [CQRS_PATTERN.md](patterns/CQRS_PATTERN.md) - CQRS pattern overview
- [SPECIFICATION_PATTERN.md](patterns/SPECIFICATION_PATTERN.md) - Specification pattern details
- [REPOSITORY_PATTERN.md](patterns/REPOSITORY_PATTERN.md) - Repository pattern usage
- [UNIT_OF_WORK_PATTERN.md](patterns/UNIT_OF_WORK_PATTERN.md) - Transaction management

### External Resources

- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)
- [CQRS Pattern - Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [Specification Pattern](https://en.wikipedia.org/wiki/Specification_pattern)
- [Hexagonal Architecture - Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)

---

## Appendix: Complete Example Migration

### Example: UserBadge Entity

This is a real example from Phase 5 showing a complete migration.

#### Step 1: Analyze Operations

**Original Service Interface:**

```csharp
public interface IUserBadgeService
{
    Task<UserBadge> AwardBadgeAsync(AwardBadgeRequest request);
    Task<List<UserBadge>> GetUserBadgesAsync(string userProfileId);
    Task<BadgeStats> GetBadgeStatisticsAsync(string userProfileId); // Complex!
}
```

**Decision:**
- ✅ `AwardBadgeAsync` → `AwardBadgeCommand` (simple create)
- ✅ `GetUserBadgesAsync` → `GetUserBadgesQuery` (simple list)
- ❌ `GetBadgeStatisticsAsync` → Keep in service (complex aggregation)

#### Step 2: Create Command

**File:** `src/Mystira.App.Application/CQRS/UserBadges/Commands/AwardBadgeCommand.cs`

```csharp
using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Commands;

public record AwardBadgeCommand(AwardBadgeRequest Request) : ICommand<UserBadge>;
```

**File:** `src/Mystira.App.Application/CQRS/UserBadges/Commands/AwardBadgeCommandHandler.cs`

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Commands;

public class AwardBadgeCommandHandler : ICommandHandler<AwardBadgeCommand, UserBadge>
{
    private readonly IUserBadgeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AwardBadgeCommandHandler> _logger;

    public AwardBadgeCommandHandler(
        IUserBadgeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AwardBadgeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserBadge> Handle(AwardBadgeCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrEmpty(request.UserProfileId))
            throw new ArgumentException("UserProfileId is required");
        if (string.IsNullOrEmpty(request.BadgeConfigurationId))
            throw new ArgumentException("BadgeConfigurationId is required");

        var badge = new UserBadge
        {
            Id = Guid.NewGuid().ToString(),
            UserProfileId = request.UserProfileId,
            BadgeConfigurationId = request.BadgeConfigurationId,
            Axis = request.Axis,
            EarnedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(badge);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Awarded badge {BadgeId} to user profile {UserProfileId}",
            badge.Id, request.UserProfileId);

        return badge;
    }
}
```

#### Step 3: Create Query

**File:** `src/Mystira.App.Application/CQRS/UserBadges/Queries/GetUserBadgesQuery.cs`

```csharp
using Mystira.App.Application.Interfaces;
using Mystira.App.Domain.Models;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

public record GetUserBadgesQuery(string UserProfileId) : IQuery<List<UserBadge>>;
```

**File:** `src/Mystira.App.Application/CQRS/UserBadges/Queries/GetUserBadgesQueryHandler.cs`

```csharp
using Microsoft.Extensions.Logging;
using Mystira.App.Application.Interfaces;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Mystira.App.Domain.Specifications;

namespace Mystira.App.Application.CQRS.UserBadges.Queries;

public class GetUserBadgesQueryHandler : IQueryHandler<GetUserBadgesQuery, List<UserBadge>>
{
    private readonly IUserBadgeRepository _repository;
    private readonly ILogger<GetUserBadgesQueryHandler> _logger;

    public GetUserBadgesQueryHandler(
        IUserBadgeRepository repository,
        ILogger<GetUserBadgesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<UserBadge>> Handle(GetUserBadgesQuery request, CancellationToken cancellationToken)
    {
        var spec = new UserBadgesByProfileSpecification(request.UserProfileId);
        var badges = await _repository.ListAsync(spec);

        _logger.LogDebug("Retrieved {Count} badges for user profile {UserProfileId}",
            badges.Count(), request.UserProfileId);

        return badges.ToList();
    }
}
```

#### Step 4: Create Specifications

**File:** `src/Mystira.App.Domain/Specifications/UserBadgeSpecifications.cs`

```csharp
using Mystira.App.Domain.Models;

namespace Mystira.App.Domain.Specifications;

public class UserBadgesByProfileSpecification : BaseSpecification<UserBadge>
{
    public UserBadgesByProfileSpecification(string userProfileId)
        : base(b => b.UserProfileId == userProfileId)
    {
        ApplyOrderByDescending(b => b.EarnedAt);
    }
}

public class UserBadgesByAxisSpecification : BaseSpecification<UserBadge>
{
    public UserBadgesByAxisSpecification(string userProfileId, string axis)
        : base(b => b.UserProfileId == userProfileId && b.Axis == axis)
    {
        ApplyOrderByDescending(b => b.EarnedAt);
    }
}
```

#### Step 5: Update Controller

**Before:**

```csharp
public class UserBadgesController : ControllerBase
{
    private readonly IUserBadgeService _badgeService;

    public UserBadgesController(IUserBadgeService badgeService)
    {
        _badgeService = badgeService;
    }

    [HttpPost("award")]
    public async Task<ActionResult<UserBadge>> AwardBadge([FromBody] AwardBadgeRequest request)
    {
        var badge = await _badgeService.AwardBadgeAsync(request);
        return CreatedAtAction(nameof(GetUserBadges), new { userProfileId = badge.UserProfileId }, badge);
    }

    [HttpGet("{userProfileId}")]
    public async Task<ActionResult<List<UserBadge>>> GetUserBadges(string userProfileId)
    {
        var badges = await _badgeService.GetUserBadgesAsync(userProfileId);
        return Ok(badges);
    }
}
```

**After:**

```csharp
public class UserBadgesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserBadgeService _badgeService; // Keep for statistics

    public UserBadgesController(
        IMediator mediator,
        IUserBadgeService badgeService)
    {
        _mediator = mediator;
        _badgeService = badgeService; // Only for complex stats
    }

    [HttpPost("award")]
    public async Task<ActionResult<UserBadge>> AwardBadge([FromBody] AwardBadgeRequest request)
    {
        var command = new AwardBadgeCommand(request);
        var badge = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUserBadges), new { userProfileId = badge.UserProfileId }, badge);
    }

    [HttpGet("{userProfileId}")]
    public async Task<ActionResult<List<UserBadge>>> GetUserBadges(string userProfileId)
    {
        var query = new GetUserBadgesQuery(userProfileId);
        var badges = await _mediator.Send(query);
        return Ok(badges);
    }

    // Complex operation kept in service layer
    [HttpGet("{userProfileId}/statistics")]
    public async Task<ActionResult<BadgeStats>> GetBadgeStatistics(string userProfileId)
    {
        var stats = await _badgeService.GetBadgeStatisticsAsync(userProfileId);
        return Ok(stats);
    }
}
```

#### Step 6: Commit

```bash
git add src/Mystira.App.Application/CQRS/UserBadges/
git add src/Mystira.App.Domain/Specifications/UserBadgeSpecifications.cs
git add src/Mystira.App.Api/Controllers/UserBadgesController.cs

git commit -m "$(cat <<'EOF'
refactor: Migrate UserBadge to CQRS pattern

- Add AwardBadgeCommand and handler
- Add GetUserBadgesQuery and handler
- Add UserBadgesByProfileSpecification and UserBadgesByAxisSpecification
- Update UserBadgesController to use IMediator for award and get operations
- Keep GetBadgeStatisticsAsync in service layer (complex multi-entity aggregation)

Part of Phase 5 CQRS migration. See ADR-0006.
EOF
)"

git push -u origin claude/add-project-readmes-0164TfHyDcEfm3nKpnPk6osQ
```

---

**End of Guide**

This migration guide is a living document. Update it as new patterns emerge or lessons are learned.

For questions or suggestions, contact the development team.

---

Copyright (c) 2025 Mystira. All rights reserved.
