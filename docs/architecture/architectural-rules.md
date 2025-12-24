# Architectural Rules - Hexagonal / Clean Architecture

This document defines the strict architectural rules that all code in the Mystira.App codebase must follow. These rules enforce Hexagonal Architecture (Ports and Adapters) and Clean Architecture principles.

## Table of Contents

1. [Layering Rules](#layering-rules)
2. [API vs AdminAPI Routing Rules](#api-vs-adminapi-routing-rules)
3. [DTO Project Rules](#dto-project-rules)
4. [Services Placement](#services-placement)
5. [Interaction Flow](#interaction-flow)
6. [Decision Guidelines](#decision-guidelines)
7. [Enforcement](#enforcement)

## Layering Rules

### API Layer (`/api`)

**Controllers only.**

- **Handles**: Routing, DTO binding, auth attributes, validation
- **Absolutely no business logic**
- **Maps**: DTOs → Use Case Input Models
- **Location**: `src/Mystira.App.Api/Controllers/`

**Example:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class GameSessionsController : ControllerBase
{
    private readonly ICreateGameSessionUseCase _createUseCase;

    [HttpPost]
    public async Task<ActionResult<GameSessionResponse>> Create(
        [FromBody] CreateGameSessionRequest request)
    {
        // Map DTO to use case input
        var input = new CreateGameSessionInput
        {
            AccountId = request.AccountId,
            ScenarioId = request.ScenarioId
        };

        // Call use case (no business logic here)
        var result = await _createUseCase.ExecuteAsync(input);
        
        // Map result to DTO
        return Ok(new GameSessionResponse { /* ... */ });
    }
}
```

### AdminAPI Layer (`/adminapi`)

**Same rules as API layer.**

- **Used only for**: Operations affecting system-level or other users' resources
- **Do not expose**: Admin endpoints to public clients
- **Location**: `src/Mystira.App.Admin.Api/Controllers/`

**Example:**

```csharp
[ApiController]
[Route("adminapi/[controller]")]
[Authorize(Roles = "Admin")]
public class ScenariosAdminController : ControllerBase
{
    private readonly IDeleteScenarioUseCase _deleteUseCase;

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        // System-level operation - belongs in AdminAPI
        await _deleteUseCase.ExecuteAsync(id);
        return NoContent();
    }
}
```

### Application Layer

**The ONLY location for:**

- Use Cases (one class per business action)
- Application Services
- Ports (interfaces for repositories & external systems)
- MediatR handlers / pipeline behaviours (if used)
- Orchestration, workflows, and business rules

**Location**: `src/Mystira.App.Application/`

**Structure:**

``` text
Mystira.App.Application/
├── UseCases/
│   ├── GameSessions/
│   │   ├── CreateGameSessionUseCase.cs
│   │   ├── GetGameSessionUseCase.cs
│   │   └── ...
│   └── Accounts/
│       └── ...
├── Ports/
│   ├── IGameSessionRepository.cs
│   └── IUnitOfWork.cs
└── Services/
    └── (Application Services only)
```

**Example:**

```csharp
namespace Mystira.App.Application.UseCases.GameSessions;

public class CreateGameSessionUseCase : ICreateGameSessionUseCase
{
    private readonly IGameSessionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<GameSession> ExecuteAsync(CreateGameSessionInput input)
    {
        // Business logic and orchestration here
        var session = new GameSession
        {
            AccountId = input.AccountId,
            ScenarioId = input.ScenarioId,
            Status = GameSessionStatus.Active
        };

        await _repository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();
        
        return session;
    }
}
```

### Domain Layer

#### Entities, Value Objects, Domain Events, Pure business invariants

- **No infrastructure**
- **No DTOs**
- **No framework dependencies**
- **Location**: `src/Mystira.App.Domain/`

**Structure:**

``` text
Mystira.App.Domain/
├── Models/
│   ├── GameSession.cs
│   ├── Account.cs
│   └── ...
├── ValueObjects/
│   └── ...
└── Events/
    └── ...
```

**Example:**

```csharp
namespace Mystira.App.Domain.Models;

public class GameSession
{
    public string Id { get; set; }
    public string AccountId { get; set; }
    public GameSessionStatus Status { get; set; }

    // Domain logic - business invariants
    public void End()
    {
        if (Status == GameSessionStatus.Ended)
            throw new InvalidOperationException("Session already ended");
        
        Status = GameSessionStatus.Ended;
        EndedAt = DateTime.UtcNow;
    }
}
```

### Infrastructure Layer

#### Repository implementations, External API adapters, File storage adapters, EF Core mappings**

- **No business rules**
- **Location**: `src/Mystira.App.Infrastructure.*/`

**Structure:**

``` text
Mystira.App.Infrastructure.Data/
├── Repositories/
│   ├── GameSessionRepository.cs
│   └── ...
└── UnitOfWork/
    └── UnitOfWork.cs

Mystira.App.Infrastructure.Azure/
└── Services/
    └── AzureBlobStorageService.cs
```

**Example:**

```csharp
namespace Mystira.App.Infrastructure.Data.Repositories;

public class GameSessionRepository : Repository<GameSession>, IGameSessionRepository
{
    public GameSessionRepository(DbContext context) : base(context)
    {
    }

    // Pure data access - no business logic
    public async Task<GameSession?> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Where(gs => gs.AccountId == accountId)
            .FirstOrDefaultAsync();
    }
}
```

## API vs AdminAPI Routing Rules

Use simple deterministic rules:

### Rule: "User acting on their own resources" → `/api`

**Examples:**

- Edit own avatar → `/api/avatars`
- Upload media → `/api/media`
- Create own game session → `/api/gamesessions`
- View own profile → `/api/userprofiles`
- Authenticate → `/api/auth`

### Rule: "Acting on system-level or other users' data" → `/adminapi`

**Examples:**

- Publish bundle → `/adminapi/bundles`
- Delete other user content → `/adminapi/userprofiles/{id}`
- Moderate items → `/adminapi/scenarios/{id}/moderate`
- System configuration → `/adminapi/config`

### Important Notes

- **Roles do not determine routing** - The operation's ownership determines routing
- **Authentication endpoints** live under `/api/auth` (users authenticate themselves)
- **Admin authentication** may use `/adminapi/auth` if separate admin auth is needed

### Decision Tree

``` text
Is the user acting on their own resources?
├─ YES → /api
└─ NO → Is it system-level or other users' data?
    ├─ YES → /adminapi
    └─ NO → Review requirement (may be misclassified)
```

## DTO Project Rules

DTOs belong in a separate contracts project.

### DTO Project May Contain

- Request DTOs
- Response DTOs
- Message contracts

**Location**: `src/Mystira.Contracts.App/`

**Structure:**

``` text
Mystira.Contracts.App/
├── Requests/
│   ├── GameSessions/
│   │   ├── CreateGameSessionRequest.cs
│   │   └── ...
│   └── Accounts/
│       └── ...
├── Responses/
│   ├── GameSessions/
│   │   ├── GameSessionResponse.cs
│   │   └── ...
│   └── ...
└── Messages/
    └── ...
```

### DTO Project Must NOT Reference

- API or AdminAPI
- Infrastructure
- Domain (except for simple value types if needed)

### DTO Rules

- **No business logic** - Only simple properties
- **Mapping must occur** in the API/AdminAPI layer
- **Use AutoMapper** or manual mapping in controllers

**Example:**

```csharp
namespace Mystira.Contracts.App.Requests.GameSessions;

public class CreateGameSessionRequest
{
    [Required]
    public string AccountId { get; set; } = string.Empty;
    
    [Required]
    public string ScenarioId { get; set; } = string.Empty;
    
    // No business logic - just data
}
```

## Services Placement

### Services Exist Only in the Application Layer

- **API/AdminAPI never contain services**
- **Domain services allowed** only when logic doesn't belong in an entity
- **Infrastructure services** are adapters (repositories, external APIs)

### Service Types

1. **Application Services** (`Mystira.App.Application/Services/`)
   - Orchestrate use cases
   - Coordinate multiple repositories
   - Handle cross-cutting concerns

2. **Domain Services** (`Mystira.App.Domain/Services/`)
   - Pure domain logic that doesn't belong in a single entity
   - Rare - prefer entity methods when possible

3. **Infrastructure Services** (`Mystira.App.Infrastructure.*/Services/`)
   - Adapters for external systems
   - File storage, external APIs, etc.

### Anti-Pattern: Service in API Layer

❌ **WRONG:**

```csharp
// src/Mystira.App.Api/Services/GameSessionService.cs
public class GameSessionService  // ❌ Service in API layer
{
    public async Task<GameSession> CreateAsync(...)
    {
        // Business logic here - WRONG!
    }
}
```

✅ **CORRECT:**

```csharp
// src/Mystira.App.Application/UseCases/GameSessions/CreateGameSessionUseCase.cs
public class CreateGameSessionUseCase  // ✅ Use case in Application layer
{
    public async Task<GameSession> ExecuteAsync(...)
    {
        // Business logic here - CORRECT!
    }
}
```

## Interaction Flow

**Enforce this strict flow:**

``` text
API/AdminAPI Controller
        ↓ maps DTO → input model
Application Use Case / Service
        ↓ calls domain
Domain Entities / Value Objects
        ↓ execute logic
Infrastructure Adapters (repos, storage, external APIs)
```

### Rules

1. **No layer may skip the layer below it**
   - Controller → Use Case → Domain → Repository ✅
   - Controller → Repository ❌

2. **No inward dependency violations**
   - Domain cannot depend on Application ❌
   - Application cannot depend on Infrastructure ❌
   - Use dependency inversion (interfaces)

3. **Dependency Direction**

   ``` text
   API → Application → Domain ← Infrastructure
   ```

### Example Flow

```csharp
// 1. Controller (API Layer)
[HttpPost]
public async Task<ActionResult> Create([FromBody] CreateGameSessionRequest dto)
{
    // Map DTO to input model
    var input = new CreateGameSessionInput
    {
        AccountId = dto.AccountId,
        ScenarioId = dto.ScenarioId
    };
    
    // Call use case
    var result = await _createUseCase.ExecuteAsync(input);
    
    // Map result to DTO
    return Ok(new GameSessionResponse { /* ... */ });
}

// 2. Use Case (Application Layer)
public async Task<GameSession> ExecuteAsync(CreateGameSessionInput input)
{
    // Orchestrate business logic
    var session = new GameSession
    {
        AccountId = input.AccountId,
        ScenarioId = input.ScenarioId
    };
    
    // Call domain method
    session.Start(); // Domain logic
    
    // Use repository (infrastructure)
    await _repository.AddAsync(session);
    await _unitOfWork.SaveChangesAsync();
    
    return session;
}

// 3. Domain Entity (Domain Layer)
public void Start()
{
    // Business invariant
    if (Status != GameSessionStatus.Pending)
        throw new InvalidOperationException("Cannot start non-pending session");
    
    Status = GameSessionStatus.Active;
    StartedAt = DateTime.UtcNow;
}

// 4. Repository (Infrastructure Layer)
public async Task AddAsync(GameSession session)
{
    // Pure data access
    await _dbSet.AddAsync(session);
}
```

## Decision Guidelines

### When in Doubt

Apply these rules:

#### Ownership Rule

- **My data?** → `/api`
- **System/other user data** → `/adminapi`

#### Logic Rule

- **Anything meaningful** → Application Layer
- **Pure rules/invariants** → Domain Layer
- **I/O** → Infrastructure Layer
- **Glue** → API/AdminAPI Layer

#### Layer Rule

- **Business logic?** → Application or Domain
- **Data access?** → Infrastructure
- **HTTP concerns?** → API/AdminAPI
- **DTOs?** → Contracts

### Common Scenarios

#### Scenario 1: User uploads media

- **Routing**: `/api/media` (user's own media)
- **Controller**: `MediaController` in API
- **Use Case**: `UploadMediaUseCase` in Application
- **Repository**: `MediaAssetRepository` in Infrastructure

#### Scenario 2: Admin deletes user content

- **Routing**: `/adminapi/media/{id}` (other user's content)
- **Controller**: `MediaAdminController` in AdminAPI
- **Use Case**: `DeleteMediaUseCase` in Application
- **Repository**: `MediaAssetRepository` in Infrastructure

#### Scenario 3: System processes background job

- **Routing**: N/A (background job)
- **Service**: Application Service or Use Case
- **Repository**: Infrastructure
- **No API layer** (internal operation)

## Enforcement

### Code Review Checklist

- [ ] No business logic in controllers
- [ ] No services in API/AdminAPI layers
- [ ] DTOs only in Contracts project
- [ ] Use cases in Application layer
- [ ] Domain entities contain business invariants
- [ ] Infrastructure contains only adapters
- [ ] Correct routing (`/api` vs `/adminapi`)
- [ ] Proper dependency direction

### Automated Checks

- **Build-time**: Project references enforce dependency rules
- **Linting**: EditorConfig and analyzers enforce style
- **CI/CD**: Format checks and build validation

### Violations

If code violates these rules:

1. **Refactor immediately** - Don't accumulate technical debt
2. **Document exception** - If exception is truly needed, document why
3. **Update rules** - If rule is wrong, update this document

## References

- [Hexagonal Architecture](patterns/HEXAGONAL_ARCHITECTURE.md)
- [Repository Pattern](patterns/REPOSITORY_PATTERN.md)
- [Unit of Work Pattern](patterns/UNIT_OF_WORK_PATTERN.md)
- [API Endpoint Classification](API_ENDPOINT_CLASSIFICATION.md)
