# Mystira.App - AI Assistant Instructions

## Project Overview

Mystira is an interactive adventure platform for children featuring educational storytelling with gamification elements. The platform provides facilitators with tools to guide children through D&D-style adventures with age-appropriate content, media, and interactive elements.

## Architecture Rules (STRICT)

### Hexagonal/Clean Architecture - MANDATORY

**Layer Separation (NO VIOLATIONS ALLOWED):**

1. **Domain Layer** (`src/Mystira.App.Domain/`)
   - Pure business logic and domain models
   - Zero external dependencies
   - No references to other layers
   - Contains: Entities, Value Objects, Domain Events, Domain Services

2. **Application Layer** (`src/Mystira.App.Application/`)
   - Use cases and orchestration
   - Defines interfaces (ports) for repositories and external services
   - Contains: Use Cases, Application Services, DTOs, MediatR Handlers (if used)
   - Can reference: Domain layer only

3. **Infrastructure Layer** (`src/Mystira.App.Infrastructure.*/`)
   - External service implementations
   - Database access (Cosmos DB repositories)
   - Third-party integrations (Azure Blob Storage, SendGrid)
   - Implements interfaces from Application layer

4. **API Layer** (`src/Mystira.App.Api/`, `src/Mystira.App.Admin.Api/`)
   - **Controllers ONLY** - NO business logic
   - Handles: Routing, DTO binding, authentication, validation
   - Dispatches to Application layer use cases
   - Maps DTOs to/from use case models

### Breaking These Rules Requires Explicit Justification

If you must violate these rules, you MUST:
1. Explain why the violation is necessary
2. Document the technical debt created
3. Propose a plan to refactor later

## Technology Stack

- **Backend**: .NET 9, C#, ASP.NET Core
- **Frontend**: Blazor WebAssembly PWA
- **Database**: Azure Cosmos DB
- **Storage**: Azure Blob Storage
- **Email**: SendGrid
- **Authentication**: JWT-based (custom implementation)
- **Cloud**: Azure (Static Web Apps, App Services, Cosmos DB, Blob Storage)
- **Architecture**: Hexagonal/Clean Architecture with Domain-Driven Design (DDD)

## Code Generation Rules

### API Controllers (ALWAYS follow this pattern)

```csharp
[ApiController]
[Route("api/[controller]")]
public class EntityController : ControllerBase
{
    private readonly IUseCase _useCase; // Inject use cases, NOT services

    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Create([FromBody] RequestDto request)
    {
        // 1. Map DTO to use case input
        var input = new UseCaseInput { /* map properties */ };
        
        // 2. Call use case (NO BUSINESS LOGIC HERE)
        var result = await _useCase.ExecuteAsync(input);
        
        // 3. Map result to response DTO
        return Ok(new ResponseDto { /* map properties */ });
    }
}
```

**API vs AdminAPI Routing:**
- **`/api/*`**: User-facing operations (own resources)
- **`/adminapi/*`**: System-level operations (any user's resources, require admin role)

### Use Cases (Application Layer)

```csharp
// Define use case interface
public interface ICreateEntityUseCase
{
    Task<UseCaseResult<EntityDto>> ExecuteAsync(CreateEntityInput input);
}

// Implement use case
public class CreateEntityUseCase : ICreateEntityUseCase
{
    private readonly IEntityRepository _repository;
    private readonly IDomainService _domainService;

    public CreateEntityUseCase(IEntityRepository repository, IDomainService domainService)
    {
        _repository = repository;
        _domainService = domainService;
    }

    public async Task<UseCaseResult<EntityDto>> ExecuteAsync(CreateEntityInput input)
    {
        // 1. Validate input
        if (string.IsNullOrEmpty(input.Name))
            return UseCaseResult<EntityDto>.Failure("Name is required");

        // 2. Execute business logic
        var entity = new Entity(input.Name, input.Description);
        
        // 3. Persist changes
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        // 4. Return result
        return UseCaseResult<EntityDto>.Success(MapToDto(entity));
    }
}
```

### Domain Models

```csharp
// Domain entities should be rich with behavior
public class GameSession
{
    // Private setters - only domain methods can modify state
    public string Id { get; private set; }
    public string AccountId { get; private set; }
    public SessionStatus Status { get; private set; }
    
    // Factory method
    public static GameSession Create(string accountId, string scenarioId)
    {
        // Business rules enforced here
        if (string.IsNullOrEmpty(accountId))
            throw new ArgumentException("Account ID is required");
            
        return new GameSession
        {
            Id = Guid.NewGuid().ToString(),
            AccountId = accountId,
            Status = SessionStatus.Active,
            StartedAt = DateTime.UtcNow
        };
    }
    
    // Domain methods
    public void Complete()
    {
        if (Status != SessionStatus.Active)
            throw new InvalidOperationException("Can only complete active sessions");
            
        Status = SessionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}
```

## Security Rules

### Authentication & Authorization

```csharp
// Always use [Authorize] for protected endpoints
[Authorize]
[HttpGet("profile")]
public async Task<ActionResult<ProfileDto>> GetProfile()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ...
}

// Admin endpoints require admin role
[Authorize(Roles = "Admin")]
[HttpDelete("adminapi/scenarios/{id}")]
public async Task<IActionResult> DeleteScenario(string id)
{
    // ...
}
```

### Input Validation

```csharp
// Always validate at API layer
public class CreateScenarioRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }
    
    [Required]
    public string Description { get; set; }
    
    [Range(3, 18)]
    public int MinAge { get; set; }
}
```

### Secrets Management

- **NEVER** hardcode secrets, API keys, or connection strings
- Always use configuration: `configuration["Azure:BlobStorage:ConnectionString"]`
- Use Azure Key Vault for production secrets
- Use User Secrets for local development

## Blazor PWA Rules

### Component Structure

```razor
@page "/adventures"
@inject IApiClient ApiClient
@inject NavigationManager Navigation

<PageTitle>Adventures</PageTitle>

<div class="container">
    @if (isLoading)
    {
        <SkeletonLoader Count="6" />
    }
    else
    {
        @foreach (var adventure in adventures)
        {
            <AdventureCard Adventure="@adventure" OnClick="@SelectAdventure" />
        }
    }
</div>

@code {
    private List<AdventureDto> adventures = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        adventures = await ApiClient.GetAdventuresAsync();
        isLoading = false;
    }

    private void SelectAdventure(AdventureDto adventure)
    {
        Navigation.NavigateTo($"/adventure/{adventure.Id}");
    }
}
```

### CSS Styling

**Use Blazor Scoped CSS (NOT CSS Modules):**

```
Components/
├── AdventureCard.razor
└── AdventureCard.razor.css  ← Automatically scoped at build time
```

```css
/* AdventureCard.razor.css - scoped to component */
.card {
    border: 1px solid #ddd;
    border-radius: 8px;
    padding: 16px;
}

.card:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}
```

**Global CSS** (`wwwroot/css/app.css`) for:
- Design system variables
- Utility classes
- Bootstrap overrides

## Testing Patterns

### Unit Tests (xUnit)

```csharp
public class CreateGameSessionUseCaseTests
{
    private readonly Mock<IGameSessionRepository> _mockRepository;
    private readonly CreateGameSessionUseCase _useCase;

    public CreateGameSessionUseCaseTests()
    {
        _mockRepository = new Mock<IGameSessionRepository>();
        _useCase = new CreateGameSessionUseCase(_mockRepository.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var input = new CreateGameSessionInput
        {
            AccountId = "account-123",
            ScenarioId = "scenario-456"
        };

        // Act
        var result = await _useCase.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<GameSession>()), Times.Once);
    }
}
```

### Integration Tests

```csharp
public class GameSessionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GameSessionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGameSession_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateGameSessionRequest
        {
            AccountId = "test-account",
            ScenarioId = "test-scenario"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/gamesessions", request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

## Performance Guidelines

### Database Queries (Cosmos DB)

```csharp
// ✅ GOOD - Use async, projection, and pagination
public async Task<List<ScenarioDto>> GetScenariosAsync(int page, int pageSize)
{
    return await _container
        .GetItemLinqQueryable<Scenario>()
        .Where(s => s.IsPublished)
        .OrderByDescending(s => s.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(s => new ScenarioDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description
        })
        .ToListAsync();
}

// ❌ BAD - Synchronous, no projection, loads everything
public List<Scenario> GetAllScenarios()
{
    return _container
        .GetItemLinqQueryable<Scenario>()
        .ToList(); // Loads all fields, all items
}
```

### Blazor Components

```csharp
// Use ShouldRender to prevent unnecessary re-renders
protected override bool ShouldRender()
{
    return hasChanges; // Only render when data actually changed
}

// Use @key for list items
@foreach (var item in items)
{
    <div @key="item.Id">
        <ItemComponent Item="@item" />
    </div>
}
```

## COPPA Compliance (Critical for Children's Platform)

### Data Collection Rules

```csharp
// Always verify parental consent before collecting child data
public async Task<bool> CreateChildProfileAsync(CreateProfileRequest request)
{
    // 1. Verify parental consent exists and is valid
    var consent = await _consentRepository.GetByEmailAsync(request.ParentEmail);
    if (consent == null || !consent.IsVerified)
    {
        throw new UnauthorizedException("Parental consent required");
    }

    // 2. Only collect necessary information
    var profile = new ChildProfile
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.ChildName, // First name only
        Age = request.Age,
        ParentEmail = request.ParentEmail, // Stored encrypted
        // NO unnecessary personal information
    };

    await _repository.AddAsync(profile);
    return true;
}
```

### Data Anonymization

```csharp
// Anonymize user IDs in logs and analytics
public void LogEvent(string eventName, string userId)
{
    var anonymizedId = HashUserId(userId); // One-way hash
    _logger.LogInformation("Event: {EventName}, User: {AnonymizedId}", 
        eventName, anonymizedId);
}
```

## Error Handling

### API Error Responses

```csharp
public async Task<ActionResult<GameSessionDto>> GetGameSession(string id)
{
    try
    {
        var result = await _useCase.ExecuteAsync(new GetGameSessionInput { Id = id });
        
        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        
        return Ok(result.Data);
    }
    catch (UnauthorizedException ex)
    {
        return Unauthorized(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting game session {Id}", id);
        return StatusCode(500, new { message = "An error occurred" });
    }
}
```

### Blazor Error Handling

```razor
@code {
    private string errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            data = await ApiClient.GetDataAsync();
        }
        catch (UnauthorizedException)
        {
            Navigation.NavigateTo("/login");
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to load data. Please try again.";
            Logger.LogError(ex, "Error loading data");
        }
    }
}

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger">@errorMessage</div>
}
```

## Naming Conventions

- **C# Classes**: PascalCase (`GameSession`, `CreateGameSessionUseCase`)
- **C# Methods**: PascalCase (`ExecuteAsync`, `GetProfileById`)
- **C# Private Fields**: _camelCase (`_repository`, `_logger`)
- **C# Parameters**: camelCase (`accountId`, `scenarioId`)
- **Blazor Components**: PascalCase (`AdventureCard.razor`, `HeroSection.razor`)
- **CSS Classes**: kebab-case (`adventure-card`, `hero-section`)
- **Database IDs**: kebab-case with prefix (`game-session-123`, `scenario-abc-456`)

## Common Patterns to Follow

### Repository Pattern

```csharp
public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(string id);
    Task<List<GameSession>> GetByAccountIdAsync(string accountId);
    Task AddAsync(GameSession session);
    Task UpdateAsync(GameSession session);
    Task DeleteAsync(string id);
}
```

### Use Case Result Pattern

```csharp
public class UseCaseResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static UseCaseResult<T> Success(T data)
        => new() { IsSuccess = true, Data = data };

    public static UseCaseResult<T> Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
```

## Documentation References

- [Architectural Rules](docs/architecture/ARCHITECTURAL_RULES.md)
- [Best Practices](docs/best-practices.md)
- [CSS Styling Approach](docs/features/CSS_STYLING_APPROACH.md)
- [Potential Enhancements Roadmap](docs/POTENTIAL_ENHANCEMENTS_ROADMAP.md)
- [Setup Instructions](docs/setup/)

## When in Doubt

1. **Check existing code** for similar patterns
2. **Consult architectural rules** - they are STRICT
3. **Ask for clarification** rather than assuming
4. **Document decisions** in code comments or ADRs
5. **Prioritize child safety** - COPPA compliance is non-negotiable

## Key Reminders

- ✅ Controllers dispatch to use cases (no business logic)
- ✅ Business logic lives in Application/Domain layers
- ✅ Always use async/await for I/O operations
- ✅ Validate input at API boundaries
- ✅ Use scoped CSS for Blazor components
- ✅ Never hardcode secrets
- ✅ Ensure COPPA compliance for all child data
- ✅ Log errors with context (but anonymize user data)
- ✅ Write tests for business logic
- ✅ Follow existing code patterns and conventions
