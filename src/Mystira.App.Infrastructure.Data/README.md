# Mystira.App.Infrastructure.Data

Data persistence infrastructure implementing the repository pattern and unit of work. This project serves as a **secondary adapter** in the hexagonal architecture, providing concrete implementations of data access ports defined by the Application layer.

## ✅ Hexagonal Architecture - FULLY COMPLIANT

**Layer**: **Infrastructure - Data Adapter (Secondary/Driven)**

The Infrastructure.Data layer is a **secondary adapter** (driven adapter) that:
- **Implements** repository port interfaces defined in `Application.Ports.Data`
- **Translates** domain entities to/from database representations
- **Manages** data persistence using Entity Framework Core
- **Abstracts** database technology from the core business logic
- **Coordinates** transactions via Unit of Work pattern
- **ZERO reverse dependencies** - Application never references Infrastructure

**Dependency Flow** (Correct ✅):
```
Domain Layer (Core)
    ↓ references
Application Layer
    ↓ defines
Application.Ports.Data (Interfaces)
    ↑ implemented by
Infrastructure.Data (THIS - Implementations)
    ↓ uses
Entity Framework Core / Cosmos DB
```

**Key Principles**:
- ✅ **Port Implementation** - Implements repository interfaces from `Application.Ports.Data`
- ✅ **Persistence Ignorance** - Domain models don't know about EF Core
- ✅ **Technology Adapter** - Adapts EF Core to application needs
- ✅ **Dependency Inversion** - Application defines ports, Infrastructure implements them
- ✅ **Clean Architecture** - No circular dependencies, proper layering

## Project Structure

```
Mystira.App.Infrastructure.Data/
├── Repositories/
│   ├── AccountRepository.cs                  # Implements IAccountRepository
│   ├── ScenarioRepository.cs                 # Implements IScenarioRepository
│   ├── GameSessionRepository.cs              # Implements IGameSessionRepository
│   ├── MediaAssetRepository.cs               # Implements IMediaAssetRepository
│   ├── BadgeConfigurationRepository.cs       # Implements IBadgeConfigurationRepository
│   ├── CharacterMapRepository.cs             # Implements ICharacterMapRepository
│   ├── UserBadgeRepository.cs                # Implements IUserBadgeRepository
│   ├── UserProfileRepository.cs              # Implements IUserProfileRepository
│   ├── AvatarConfigurationFileRepository.cs  # Implements IAvatarConfigurationFileRepository
│   ├── CharacterMapFileRepository.cs         # Implements ICharacterMapFileRepository
│   ├── CharacterMediaMetadataFileRepository.cs
│   ├── MediaMetadataFileRepository.cs
│   ├── ContentBundleRepository.cs            # Implements IContentBundleRepository
│   └── PendingSignupRepository.cs            # Implements IPendingSignupRepository
├── UnitOfWork/
│   └── UnitOfWork.cs                         # Implements IUnitOfWork
├── MystiraAppDbContext.cs                    # EF Core DbContext
├── PartitionKeyInterceptor.cs                # Cosmos DB optimization
└── Mystira.App.Infrastructure.Data.csproj
```

**Port Interfaces** (defined in Application layer):
- All `I*Repository` interfaces live in `Application/Ports/Data/`
- `IUnitOfWork` lives in `Application/Ports/Data/`
- Infrastructure.Data references Application to implement these ports

## Core Concepts

### Repository Pattern

The repository pattern abstracts data access, allowing the application to work with domain entities without knowing about database details.

#### Port Interface (defined in Application.Ports.Data)
```csharp
// Location: Application/Ports/Data/IRepository.cs
namespace Mystira.App.Application.Ports.Data;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
    IQueryable<T> GetQueryable();
}
```

#### Implementation (in Infrastructure.Data)
```csharp
// Location: Infrastructure.Data/Repositories/ScenarioRepository.cs
using Mystira.App.Application.Ports.Data;  // Port interface ✅

namespace Mystira.App.Infrastructure.Data.Repositories;

public class ScenarioRepository : IScenarioRepository
{
    private readonly MystiraAppDbContext _context;

    public ScenarioRepository(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<Scenario?> GetByIdAsync(string id)
    {
        return await _context.Scenarios
            .Include(s => s.Scenes)
            .Include(s => s.CharacterArchetypes)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Scenario>> GetByAgeGroupAsync(string ageGroup)
    {
        return await _context.Scenarios
            .Where(s => s.AgeGroup == ageGroup)
            .ToListAsync();
    }

    public async Task AddAsync(Scenario scenario)
    {
        await _context.Scenarios.AddAsync(scenario);
    }
}
```

### Unit of Work Pattern

Coordinates multiple repository operations into a single transaction.

#### Port Interface (Application.Ports.Data)
```csharp
// Location: Application/Ports/Data/IUnitOfWork.cs
namespace Mystira.App.Application.Ports.Data;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

#### Implementation (Infrastructure.Data)
```csharp
// Location: Infrastructure.Data/UnitOfWork/UnitOfWork.cs
using Mystira.App.Application.Ports.Data;  // Port interface ✅

namespace Mystira.App.Infrastructure.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly MystiraAppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(MystiraAppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}
```

## Repository Implementations

### AccountRepository
Manages user accounts:
- `GetByIdAsync(string id)`: Get account by ID
- `GetByEmailAsync(string email)`: Find by email
- `AddAsync(Account)`: Create new account
- `UpdateAsync(Account)`: Update existing account

### ScenarioRepository
Manages interactive story scenarios:
- `GetByAgeGroupAsync(string)`: Filter by age group
- `GetAllAsync()`: Get all scenarios
- Includes navigation properties (Scenes, CharacterArchetypes)

### GameSessionRepository
Manages active game sessions:
- `GetActiveSessionsByUserIdAsync(string userId)`: User's active sessions
- `GetByScenarioIdAsync(string scenarioId)`: Sessions for a scenario
- Tracks choice history and compass values

### MediaAssetRepository
Manages media file metadata:
- `GetByBlobNameAsync(string blobName)`: Find by blob name
- `GetByScenarioIdAsync(string scenarioId)`: Media for scenario
- Links to Azure Blob Storage

### BadgeConfigurationRepository
Manages achievement badge definitions:
- `GetByAxisAsync(string axis)`: Badges for compass axis
- Badge configuration lookup

### CharacterMapRepository
Maps characters to media assets:
- `GetByCharacterIdAsync(string characterId)`: Maps for character
- `GetByMediaIdAsync(string mediaId)`: Maps using media

### UserBadgeRepository
Tracks user-earned badges:
- `GetByUserIdAsync(string userId)`: User's earned badges
- `HasBadgeAsync(string userId, string badgeId)`: Check if earned

### UserProfileRepository
Manages user profiles:
- `GetByIdAsync(string id)`: Get profile
- `GetNonGuestProfilesAsync()`: Non-guest profiles
- `GetGuestProfilesAsync()`: Guest profiles

## Database Technology

### Production: Azure Cosmos DB

Entity Framework Core with Cosmos DB provider:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Cosmos" Version="9.0.0" />
```

**Benefits**:
- Global distribution
- Automatic scaling
- Serverless pricing model
- JSON document storage
- Optimized for read-heavy workloads

**Configuration** (in API layer):
```csharp
services.AddDbContext<MystiraAppDbContext>(options =>
    options.UseCosmos(
        connectionString,
        databaseName: "MystiraAppDb"
    )
);
```

### Development: In-Memory Database

For local development and testing:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
```

**Configuration**:
```csharp
services.AddDbContext<MystiraAppDbContext>(options =>
    options.UseInMemoryDatabase("MystiraAppTestDb")
);
```

## DbContext Configuration

### MystiraAppDbContext

Centralized DbContext for all entity configurations:

```csharp
public class MystiraAppDbContext : DbContext
{
    public MystiraAppDbContext(DbContextOptions<MystiraAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<BadgeConfiguration> BadgeConfigurations => Set<BadgeConfiguration>();
    public DbSet<CharacterMap> CharacterMaps => Set<CharacterMap>();
    public DbSet<UserBadge> UserBadges => Set<UserBadges>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MystiraAppDbContext).Assembly);
    }
}
```

### PartitionKeyInterceptor

Cosmos DB optimization for partition key handling:

```csharp
public class PartitionKeyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetPartitionKeys(eventData.Context);
        return result;
    }

    private void SetPartitionKeys(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Set partition key based on entity type
            // Optimizes Cosmos DB queries
        }
    }
}
```

## Dependency Injection

Register repositories and Unit of Work in API layer `Program.cs`:

```csharp
// DbContext
builder.Services.AddDbContext<MystiraAppDbContext>(options =>
    options.UseCosmos(connectionString, databaseName)
);

// Unit of Work (implements Application port)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositories (implement Application ports)
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
builder.Services.AddScoped<IBadgeConfigurationRepository, BadgeConfigurationRepository>();
builder.Services.AddScoped<ICharacterMapRepository, CharacterMapRepository>();
builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IContentBundleRepository, ContentBundleRepository>();
builder.Services.AddScoped<IPendingSignupRepository, PendingSignupRepository>();
```

## Usage in Application Layer

Application use cases depend on port interfaces, not implementations:

```csharp
// Location: Application/UseCases/Scenarios/GetScenarioUseCase.cs
using Mystira.App.Application.Ports.Data;  // Port interface ✅

namespace Mystira.App.Application.UseCases.Scenarios;

public class GetScenarioUseCase
{
    private readonly IScenarioRepository _repository;  // Port ✅
    private readonly ILogger<GetScenarioUseCase> _logger;

    public GetScenarioUseCase(
        IScenarioRepository repository,  // Port ✅
        ILogger<GetScenarioUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Scenario?> ExecuteAsync(string scenarioId)
    {
        _logger.LogInformation("Getting scenario {ScenarioId}", scenarioId);
        return await _repository.GetByIdAsync(scenarioId);
    }
}
```

**Benefits**:
- ✅ Application never references Infrastructure.Data
- ✅ Can swap implementations without changing Application
- ✅ Easy to mock for testing
- ✅ Clear separation of concerns

## Transaction Coordination

### Atomic Operations Across Repositories

```csharp
public class CompleteGameSessionUseCase
{
    private readonly IGameSessionRepository _sessionRepository;  // Port ✅
    private readonly IUserBadgeRepository _badgeRepository;      // Port ✅
    private readonly IUnitOfWork _unitOfWork;                    // Port ✅

    public async Task ExecuteAsync(string sessionId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Load session
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                throw new SessionNotFoundException(sessionId);

            session.State = SessionState.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            // Award badges based on compass values
            var earnedBadges = DetermineEarnedBadges(session.CompassTracking);
            foreach (var badge in earnedBadges)
            {
                await _badgeRepository.AddAsync(badge);
            }

            // Commit atomically
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
```

## Query Optimization

### Eager Loading

Load related entities to avoid N+1 queries:

```csharp
public async Task<Scenario?> GetByIdAsync(string id)
{
    return await _context.Scenarios
        .Include(s => s.Scenes)
            .ThenInclude(sc => sc.Choices)
        .Include(s => s.CharacterArchetypes)
        .Include(s => s.MediaReferences)
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

### Projection

Select only needed data:

```csharp
public async Task<IEnumerable<ScenarioSummary>> GetSummariesAsync()
{
    return await _context.Scenarios
        .Select(s => new ScenarioSummary
        {
            Id = s.Id,
            Title = s.Title,
            AgeGroup = s.AgeGroup,
            SceneCount = s.Scenes.Count
        })
        .ToListAsync();
}
```

### Filtering and Paging

```csharp
public async Task<IEnumerable<Scenario>> GetPagedAsync(
    int page,
    int pageSize,
    string? ageGroup = null)
{
    var query = _context.Scenarios.AsQueryable();

    if (ageGroup != null)
        query = query.Where(s => s.AgeGroup == ageGroup);

    return await query
        .OrderByDescending(s => s.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}
```

## Testing

### Unit Testing Use Cases with Mocked Repositories

Application use cases can be tested without Infrastructure:

```csharp
[Fact]
public async Task GetScenario_WithValidId_ReturnsScenario()
{
    // Arrange
    var mockRepo = new Mock<IScenarioRepository>();  // Mock port ✅
    mockRepo.Setup(r => r.GetByIdAsync("test-123"))
        .ReturnsAsync(new Scenario { Id = "test-123", Title = "Test" });

    var useCase = new GetScenarioUseCase(
        mockRepo.Object,
        mockLogger.Object);

    // Act
    var result = await useCase.ExecuteAsync("test-123");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Title);
    mockRepo.Verify(r => r.GetByIdAsync("test-123"), Times.Once);
}
```

### Integration Testing Repositories

Use in-memory database for repository testing:

```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsScenario()
{
    // Arrange
    var options = new DbContextOptionsBuilder<MystiraAppDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;

    using var context = new MystiraAppDbContext(options);
    var repository = new ScenarioRepository(context);

    var scenario = new Scenario { Id = "test-123", Title = "Test" };
    await repository.AddAsync(scenario);
    await context.SaveChangesAsync();

    // Act
    var result = await repository.GetByIdAsync("test-123");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Title);
}
```

## Architectural Compliance Verification

Verify that Infrastructure.Data correctly implements Application ports:

```bash
# Check that Infrastructure.Data references Application
grep "Mystira.App.Application" Mystira.App.Infrastructure.Data.csproj
# Expected: <ProjectReference Include="..\Mystira.App.Application\...">

# Check that repositories use Application.Ports namespace
grep -r "using Mystira.App.Application.Ports.Data" Repositories/
# Expected: All repository files import from Application.Ports.Data

# Check NO Infrastructure references in Application
cd ../Mystira.App.Application
grep -r "using Mystira.App.Infrastructure" .
# Expected: (no output - Application never references Infrastructure)
```

**Results**:
- ✅ Infrastructure.Data references Application (correct direction)
- ✅ Repositories implement Application.Ports.Data interfaces
- ✅ Application has ZERO Infrastructure references
- ✅ Full dependency inversion achieved

## Performance Considerations

### Indexing

Ensure proper indexing for common queries:
- `Scenario.AgeGroup` - Frequent filtering
- `GameSession.UserId` - User session lookups
- `MediaAsset.BlobName` - Blob name lookups
- `Account.Email` - Account lookups

### Caching

Consider caching for read-heavy entities:
- Badge configurations (rarely change)
- Scenario metadata (frequently read)

### Batch Operations

Use batch operations for bulk inserts/updates:
```csharp
await _context.Scenarios.AddRangeAsync(scenarios);
await _context.SaveChangesAsync();
```

## Future Enhancements

- **CQRS**: Separate read and write models (Dapper for reads, EF for writes)
- **Specification Pattern**: Reusable query logic
- **Outbox Pattern**: Reliable event publishing
- **Soft Delete**: Instead of hard deletes
- **Audit Logging**: Track entity changes automatically

## Related Documentation

- **[Application](../Mystira.App.Application/README.md)** - Defines port interfaces this layer implements
- **[Domain](../Mystira.App.Domain/README.md)** - Domain entities persisted by repositories
- **[API](../Mystira.App.Api/README.md)** - Registers repository implementations via DI
- **[Admin.Api](../Mystira.App.Admin.Api/README.md)** - Also registers implementations

## Summary

**What This Layer Does**:
- ✅ Implements data access port interfaces from Application.Ports.Data
- ✅ Provides EF Core-based repository implementations
- ✅ Manages Cosmos DB / InMemory database access
- ✅ Coordinates transactions via Unit of Work
- ✅ Maintains clean hexagonal architecture

**What This Layer Does NOT Do**:
- ❌ Define port interfaces (Application does that)
- ❌ Contain business logic (Application/Domain does that)
- ❌ Make decisions about what to persist (Application decides)

**Key Success Metrics**:
- ✅ **Zero reverse dependencies** - Application never references Infrastructure
- ✅ **Clean interfaces** - All ports defined in Application layer
- ✅ **Testability** - Use cases can mock repositories
- ✅ **Swappability** - Can replace EF Core with Dapper without touching Application

## License

Copyright (c) 2025 Mystira. All rights reserved.
