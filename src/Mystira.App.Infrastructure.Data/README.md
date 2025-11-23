# Mystira.App.Infrastructure.Data

Data persistence infrastructure implementing the repository pattern and unit of work. This project serves as a **secondary adapter** in the hexagonal architecture, providing concrete implementations of data access ports defined by the application layer.

## Role in Hexagonal Architecture

**Layer**: **Infrastructure - Data Adapter (Secondary/Driven)**

The Infrastructure.Data layer is a **secondary adapter** (driven adapter) that:
- **Implements** repository interfaces (ports) defined in the application/domain
- **Translates** domain entities to/from database representations
- **Manages** data persistence using Entity Framework Core
- **Abstracts** database technology from the core business logic
- **Coordinates** transactions via Unit of Work pattern

**Dependency Flow**:
```
Domain Layer (Core)
    ↑ defines interfaces
Application Layer
    ↓ depends on
IRepository Ports (Interfaces)
    ↑ implemented by
Infrastructure.Data (THIS - Adapter)
    ↓ uses
Entity Framework Core / Cosmos DB
```

**Key Principles**:
- ✅ **Port Implementation** - Implements repository interfaces from domain/application
- ✅ **Persistence Ignorance** - Domain models don't know about EF Core
- ✅ **Technology Adapter** - Adapts EF Core to domain needs
- ✅ **Dependency Inversion** - Depends on domain, not vice versa

## Project Structure

```
Mystira.App.Infrastructure.Data/
├── Repositories/
│   ├── IRepository.cs                       # Base repository interface (port)
│   ├── AccountRepository.cs                 # Account data access
│   ├── IScenarioRepository.cs               # Scenario port
│   ├── ScenarioRepository.cs                # Scenario implementation
│   ├── IGameSessionRepository.cs            # Game session port
│   ├── GameSessionRepository.cs             # Game session implementation
│   ├── IMediaAssetRepository.cs             # Media asset port
│   ├── MediaAssetRepository.cs              # Media asset implementation
│   ├── IBadgeConfigurationRepository.cs     # Badge config port
│   ├── BadgeConfigurationRepository.cs      # Badge config implementation
│   ├── ICharacterMapRepository.cs           # Character map port
│   ├── CharacterMapRepository.cs            # Character map implementation
│   ├── IUserBadgeRepository.cs              # User badge port
│   ├── UserBadgeRepository.cs               # User badge implementation
│   ├── IAvatarConfigurationFileRepository.cs
│   ├── AvatarConfigurationFileRepository.cs
│   ├── ContentBundleRepository.cs
│   ├── IPendingSignupRepository.cs
│   └── PendingSignupRepository.cs
├── UnitOfWork/
│   ├── IUnitOfWork.cs                       # Unit of Work interface (port)
│   └── UnitOfWork.cs                        # Unit of Work implementation
└── Mystira.App.Infrastructure.Data.csproj
```

## Core Concepts

### Repository Pattern

The repository pattern abstracts data access, allowing the application to work with domain entities without knowing about database details.

#### Base Repository Interface (Port)
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
}
```

#### Example: ScenarioRepository
```csharp
public class ScenarioRepository : IScenarioRepository
{
    private readonly DbContext _context;

    public async Task<Scenario?> GetByIdAsync(string id)
    {
        return await _context.Scenarios
            .Include(s => s.Scenes)
            .Include(s => s.CharacterArchetypes)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Scenario>> GetByAgeGroupAsync(AgeGroup ageGroup)
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

Coordinates multiple repository operations into a single transaction:

#### IUnitOfWork Interface (Port)
```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

#### UnitOfWork Implementation
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private IDbContextTransaction? _transaction;

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
        await _transaction?.CommitAsync();
    }
}
```

## Repository Implementations

### AccountRepository
Manages DM (Dungeon Master) accounts:
- `GetByIdAsync(string id)`: Get account by ID
- `GetByEmailAsync(string email)`: Find by email
- `GetByUsernameAsync(string username)`: Find by username
- COPPA-compliant (no child accounts)

### ScenarioRepository
Manages interactive story scenarios:
- `GetByAgeGroupAsync(AgeGroup)`: Filter by age group
- `GetFeaturedAsync()`: Get featured scenarios
- `GetByThemeAsync(FantasyTheme)`: Filter by theme
- Includes navigation properties (Scenes, Characters)

### GameSessionRepository
Manages active game sessions:
- `GetActiveSessionsAsync(string userId)`: User's active sessions
- `GetByScenarioIdAsync(string scenarioId)`: Sessions for a scenario
- `GetSessionStatsAsync(string sessionId)`: Calculate statistics
- Tracks choice history and compass values

### MediaAssetRepository
Manages media file metadata:
- `GetByBlobNameAsync(string blobName)`: Find by blob name
- `GetByScenarioIdAsync(string scenarioId)`: Media for scenario
- `GetOrphanedAssetsAsync()`: Find unused media
- Links to Azure Blob Storage

### BadgeConfigurationRepository
Manages achievement badge definitions:
- `GetByAxisAsync(string axis)`: Badges for compass axis
- `GetEligibleBadgesAsync(CompassTracking)`: Badges user can earn
- Validates threshold ranges

### CharacterMapRepository
Maps characters to media assets:
- `GetByCharacterIdAsync(string characterId)`: Maps for character
- `GetByMediaIdAsync(string mediaId)`: Maps using media
- Coordinates character-media relationships

### UserBadgeRepository
Tracks user-earned badges:
- `GetByUserIdAsync(string userId)`: User's earned badges
- `HasBadgeAsync(string userId, string badgeId)`: Check if earned
- `AwardBadgeAsync(UserBadge)`: Award badge to user

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

**Configuration**:
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
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
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("MystiraAppTestDb")
);
```

## Data Mapping

### Entity Configuration

Repositories use EF Core configurations for entity mapping:

```csharp
public class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
    public void Configure(EntityTypeBuilder<Scenario> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);

        builder.OwnsMany(s => s.Scenes, scene =>
        {
            scene.Property(sc => sc.Narrative).IsRequired();
            scene.OwnsMany(sc => sc.Choices);
        });

        builder.HasMany(s => s.CharacterArchetypes)
               .WithOne()
               .HasForeignKey("ScenarioId");
    }
}
```

### Value Conversions

Convert domain value objects to database primitives:

```csharp
builder.Property(s => s.AgeGroup)
    .HasConversion(
        v => v.Value,
        v => AgeGroup.FromValue(v)
    );

builder.Property(s => s.FantasyTheme)
    .HasConversion(
        v => v.Value,
        v => FantasyTheme.FromValue(v)
    );
```

## Dependency Injection

Register repositories and Unit of Work in `Program.cs`:

```csharp
// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseCosmos(connectionString, databaseName)
);

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositories
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
builder.Services.AddScoped<IBadgeConfigurationRepository, BadgeConfigurationRepository>();
builder.Services.AddScoped<ICharacterMapRepository, CharacterMapRepository>();
builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
```

## Usage Example

### In Application Use Case

```csharp
public class StartGameSessionUseCase
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<GameSession> ExecuteAsync(string scenarioId, string userId)
    {
        // Load from repository
        var scenario = await _scenarioRepository.GetByIdAsync(scenarioId);

        if (scenario == null)
            throw new ScenarioNotFoundException(scenarioId);

        // Create domain entity
        var session = new GameSession
        {
            Id = Guid.NewGuid().ToString(),
            ScenarioId = scenarioId,
            UserId = userId,
            State = SessionState.Active,
            StartedAt = DateTime.UtcNow
        };

        // Persist via repository
        await _sessionRepository.AddAsync(session);

        // Commit transaction
        await _unitOfWork.SaveChangesAsync();

        return session;
    }
}
```

## Transaction Coordination

### Atomic Operations Across Repositories

```csharp
public class CompleteGameSessionUseCase
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IUserBadgeRepository _badgeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task ExecuteAsync(string sessionId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Load session
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            session.State = SessionState.Completed;
            session.CompletedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(session);

            // Award badges based on compass values
            var earnedBadges = DetermineEarnedBadges(session.CompassTracking);
            foreach (var badge in earnedBadges)
            {
                await _badgeRepository.AwardBadgeAsync(badge);
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
public async Task<IEnumerable<Scenario>> GetPagedAsync(int page, int pageSize, AgeGroup? ageGroup = null)
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

### Unit Testing Repositories

Use in-memory database for testing:

```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsScenario()
{
    // Arrange
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;

    using var context = new ApplicationDbContext(options);
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

## Performance Considerations

### Indexing

Ensure proper indexing for common queries:
- `Scenario.AgeGroup` - Frequent filtering
- `GameSession.UserId` - User session lookups
- `MediaAsset.BlobName` - Blob name lookups

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

- **CQRS**: Separate read and write models
- **Dapper**: Use for read-heavy queries
- **Outbox Pattern**: For reliable event publishing
- **Soft Delete**: Instead of hard deletes
- **Audit Logging**: Track entity changes

## Related Documentation

- **[Domain](../Mystira.App.Domain/README.md)** - Domain entities persisted by repositories
- **[Application](../Mystira.App.Application/README.md)** - Use cases that consume repositories
- **[Azure Infrastructure](../Mystira.App.Infrastructure.Azure/README.md)** - Cosmos DB deployment

## 🔍 Architectural Analysis

### Current State Assessment

**File Count**: 28 C# files
**Key Files**:
- `MystiraAppDbContext.cs` (moved here from API - good! ✅)
- `PartitionKeyInterceptor.cs` (moved here from Admin.Api - good! ✅)
- 20+ repository implementations
- UnitOfWork implementation

**Dependencies**:
- Domain (correct ✅)
- EF Core Cosmos, InMemory (correct ✅)

**Target Framework**: net9.0

### ⚠️ Architectural Issues Found

#### 1. **Repository Interfaces Location** (MEDIUM)
**Location**: `Repositories/I*Repository.cs` files

**Issue**: Repository interfaces (`IRepository<T>`, `IScenarioRepository`, etc.) are defined in Infrastructure.Data

**Impact**:
- ⚠️ Application layer depends on Infrastructure to get interfaces
- ⚠️ Violates Dependency Inversion (infrastructure should depend on application, not vice versa)
- ⚠️ Makes it harder to swap implementations

**Current (Wrong)**:
```
Application → Infrastructure.Data (for interfaces)
Infrastructure.Data (implements own interfaces)
```

**Should Be**:
```
Application (defines ports/interfaces)
    ↑
Infrastructure.Data (implements application's interfaces)
```

**Recommendation**:
- **MOVE** all `I*Repository.cs` interfaces to `Application/Ports/Data/`
- **MOVE** `IUnitOfWork.cs` to `Application/Ports/Data/`
- Keep only **implementations** in Infrastructure.Data
- Update namespaces: `Mystira.App.Infrastructure.Data.Repositories` → `Mystira.App.Application.Ports.Data`

#### 2. **DbContext Location** (RESOLVED ✅)
**Location**: `MystiraAppDbContext.cs`

**Status**: **Recently fixed!** Moved from API to Infrastructure.Data

**Previous Issue**: Was in `Api/Data/` and `Admin.Api/Data/` (violation)
**Current State**: Correctly in Infrastructure.Data ✅

**Impact**: This was a major violation that has been fixed

### ✅ What's Working Well

1. **DbContext Centralized** - Single DbContext in Infrastructure (recently fixed!)
2. **Repository Pattern** - Proper abstraction of data access
3. **Unit of Work** - Transaction coordination
4. **Cosmos DB + InMemory** - Good dual provider support
5. **Clean Separation** - No business logic in repositories
6. **Partition Key Interceptor** - Cosmos DB optimization

## 📋 Refactoring TODO

### 🟡 High Priority

- [ ] **Move repository interfaces to Application/Ports**
  - Create `Application/Ports/Data/` folder
  - Move all `I*Repository.cs` interfaces
  - Move `IUnitOfWork.cs` interface
  - Update all `using` statements in Application layer
  - Location: `Infrastructure.Data/Repositories/I*.cs` → `Application/Ports/Data/`
  - Estimated: ~15 interface files

- [ ] **Update namespaces after move**
  - Change namespace from `Mystira.App.Infrastructure.Data.Repositories`
  - To: `Mystira.App.Application.Ports.Data`
  - Update DI registrations in API/Admin.Api `Program.cs`

### 🟢 Medium Priority

- [ ] **Add specification pattern**
  - Create `ISpecification<T>` interface in Application/Ports
  - Implement in Infrastructure.Data
  - Enables reusable query logic

- [ ] **Implement generic repository**
  - Create `Repository<T>` base class
  - Reduce code duplication across repositories
  - Inherit specific repositories from base

### 🔵 Low Priority

- [ ] **Add audit fields tracking**
  - Automatically set CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
  - Implement in SaveChangesAsync override

- [ ] **Implement soft delete**
  - Add IsDeleted flag to entities
  - Global query filter to exclude deleted
  - Change Delete methods to set flag instead of removing

## 💡 Recommendations

### Immediate Actions
1. **Coordinate with Application layer refactoring** - Move interfaces when Application is ready
2. **Document interface locations** - Update team wiki about where interfaces live
3. **Plan migration** - Interfaces move is dependency for Application refactoring

### Short-term
1. **Move interfaces to Application/Ports** - Proper dependency direction
2. **Update all using statements** - Across Application and API projects
3. **Fix DI registrations** - Update Program.cs in API projects

### Long-term
1. **Specification pattern** - Reusable query logic
2. **Generic repository** - Reduce boilerplate
3. **CQRS read models** - Separate read and write concerns

## 📊 SWOT Analysis

### Strengths 💪
- ✅ **DbContext Centralized**: Recently moved to correct location
- ✅ **Repository Pattern**: Proper data access abstraction
- ✅ **Unit of Work**: Transaction management
- ✅ **Dual Providers**: Cosmos DB + InMemory for testing
- ✅ **Partition Strategy**: Cosmos DB optimization with interceptor
- ✅ **Clean Implementation**: No business logic leakage
- ✅ **Type Safety**: Strongly typed repositories
- ✅ **Good Structure**: 28 files, well-organized

### Weaknesses ⚠️
- ⚠️ **Interfaces in Wrong Layer**: Should be in Application/Ports
- ⚠️ **Some Duplication**: Repository methods could share base class
- ⚠️ **No Specifications**: Query logic scattered
- ⚠️ **Hard Deletes**: No soft delete support
- ⚠️ **No Audit Trail**: Missing CreatedBy, UpdatedBy tracking

### Opportunities 🚀
- 📈 **Move to Ports**: Achieve true dependency inversion
- 📈 **Specification Pattern**: Reusable, testable query logic
- 📈 **Generic Repository**: Reduce code duplication
- 📈 **CQRS**: Separate read/write models (Dapper for reads)
- 📈 **Event Sourcing**: Append-only event store
- 📈 **Audit Logging**: Full entity change tracking
- 📈 **Soft Deletes**: Better data recovery
- 📈 **Read Replicas**: Scale reads independently

### Threats 🔒
- ⚡ **Interface Move Coordination**: Must coordinate with Application refactoring
- ⚡ **Breaking Changes**: Moving interfaces affects multiple projects
- ⚡ **Cosmos DB Costs**: RU consumption can grow
- ⚡ **Testing Gaps**: Need integration tests for repositories
- ⚡ **N+1 Queries**: Easy to create with lazy loading

### Risk Mitigation
1. **Phased Migration**: Move interfaces with Application layer refactoring
2. **Comprehensive Tests**: Test all repositories before and after move
3. **Query Analysis**: Monitor Cosmos DB RU consumption
4. **Code Reviews**: Ensure eager loading used appropriately

## Current vs Target Architecture

### Current (Needs Improvement)
```
Application Layer
    ↓ depends on
Infrastructure.Data (defines interfaces + implements)
```

### Target (Correct Hexagonal)
```
Application Layer (defines ports/interfaces)
    ↑ implemented by
Infrastructure.Data (adapters/implementations only)
```

## Integration Points

### Used By
- **Application Layer**: Uses repository interfaces (currently from here, should be from Application/Ports)
- **API Layer**: Registers implementations via DI
- **Admin.Api Layer**: Registers implementations via DI

### Depends On
- **Domain Layer**: For entity definitions
- **EF Core**: For ORM functionality
- **Cosmos DB SDK**: For Azure Cosmos DB provider

## Related Documentation

- **[Domain](../Mystira.App.Domain/README.md)** - Entities persisted by this layer
- **[Application](../Mystira.App.Application/README.md)** - Should define repository interfaces (ports)
- **[API](../Mystira.App.Api/README.md)** - Registers implementations via DI

## License

Copyright (c) 2025 Mystira. All rights reserved.
