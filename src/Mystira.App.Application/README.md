# Mystira.App.Application

The application layer containing use cases, business workflows, and application services. This layer orchestrates domain logic and coordinates interactions between the domain and infrastructure layers.

## Role in Hexagonal Architecture

**Layer**: **Application Layer (Use Cases)**

The Application layer sits between the domain core and external adapters:
- **Orchestrates** business workflows using domain entities
- **Coordinates** between domain logic and infrastructure services
- **Implements** use cases that fulfill user/API requests
- **Validates** input and enforces application-level rules
- **Transforms** data between domain models and DTOs

**Dependency Flow**:
```
API/UI Layer (Adapters)
    ↓ calls
Application Layer (THIS)
    ↓ uses
Domain Layer (Core)
    ↑ implemented by
Infrastructure Layer (Adapters)
```

**Key Principles**:
- ✅ **Use Case Driven** - Each use case represents a single business operation
- ✅ **Technology Agnostic** - No knowledge of HTTP, databases, or UI frameworks
- ✅ **Thin Orchestration** - Coordinates domain logic, doesn't contain it
- ✅ **Port Interfaces** - Defines ports (interfaces) for infrastructure adapters

## Project Structure

```
Mystira.App.Application/
├── UseCases/
│   ├── Accounts/
│   │   ├── CreateAccountUseCase.cs
│   │   ├── GetAccountUseCase.cs
│   │   └── UpdateAccountUseCase.cs
│   ├── BadgeConfigurations/
│   │   ├── CreateBadgeConfigurationUseCase.cs
│   │   ├── GetBadgeConfigurationsUseCase.cs
│   │   ├── ImportBadgeConfigurationUseCase.cs
│   │   └── ExportBadgeConfigurationUseCase.cs
│   ├── CharacterMaps/
│   │   ├── CreateCharacterMapUseCase.cs
│   │   ├── UpdateCharacterMapUseCase.cs
│   │   └── DeleteCharacterMapUseCase.cs
│   ├── Characters/
│   ├── GameSessions/
│   │   ├── StartGameSessionUseCase.cs
│   │   ├── MakeChoiceUseCase.cs
│   │   ├── PauseGameSessionUseCase.cs
│   │   └── EndGameSessionUseCase.cs
│   ├── MediaAssets/
│   ├── Scenarios/
│   │   ├── CreateScenarioUseCase.cs
│   │   ├── GetScenarioUseCase.cs
│   │   ├── UpdateScenarioUseCase.cs
│   │   └── ValidateScenarioUseCase.cs
│   └── UserProfiles/
├── Parsers/
│   ├── ScenarioParser.cs              # YAML scenario parsing
│   ├── SceneParser.cs                 # Scene definition parsing
│   ├── CharacterParser.cs             # Character data parsing
│   ├── EchoLogParser.cs               # Moral echo parsing
│   ├── CompassChangeParser.cs         # Compass value parsing
│   ├── BranchParser.cs                # Choice branch parsing
│   └── MediaReferencesParser.cs       # Media asset reference parsing
├── Validation/
│   ├── ScenarioSchemaDefinitions.cs   # JSON schema validation
│   └── ...
├── Ports/
│   └── (Interfaces for infrastructure)
└── Mystira.App.Application.csproj
```

## Core Concepts

### Use Cases

Each use case represents a **single business operation** triggered by a user action or API call:

#### Example: StartGameSessionUseCase
```csharp
public class StartGameSessionUseCase
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<GameSession> ExecuteAsync(string scenarioId, string userId)
    {
        // 1. Load domain entities
        var scenario = await _scenarioRepository.GetByIdAsync(scenarioId);

        // 2. Apply business logic (domain)
        var session = new GameSession
        {
            ScenarioId = scenarioId,
            UserId = userId,
            State = SessionState.Active,
            CompassTracking = new CompassTracking()
        };

        // 3. Persist changes (infrastructure)
        await _sessionRepository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        return session;
    }
}
```

### Parsers

Transform external data formats into domain models:

#### ScenarioParser
Parses YAML scenario files into domain `Scenario` entities:
- Validates structure against JSON schema
- Extracts scenes, choices, characters
- Parses echo logs and compass changes
- Handles media references

#### CharacterParser
Parses character definitions from YAML:
- Character metadata
- Archetype associations
- Media asset mappings

#### EchoLogParser & CompassChangeParser
Parse moral feedback and compass modifications:
- Echo type validation
- Strength value parsing
- Axis and value extraction

### Validation

Application-level validation using JSON Schema:

#### ScenarioSchemaDefinitions
Defines validation rules for:
- Required fields (title, ageGroup, scenes)
- Maximum limits (4 archetypes, 4 axes)
- Value ranges (echo strength, compass values)
- Age-appropriate content

## Use Case Categories

### Account Management
- **CreateAccountUseCase**: Register new DM accounts
- **GetAccountUseCase**: Retrieve account details
- **UpdateAccountUseCase**: Modify account settings
- **DeleteAccountUseCase**: Remove accounts (GDPR compliance)

### Scenario Management
- **CreateScenarioUseCase**: Author new interactive stories
- **GetScenarioUseCase**: Retrieve scenarios with filtering
- **UpdateScenarioUseCase**: Edit existing scenarios
- **ValidateScenarioUseCase**: Validate scenario structure
- **ImportScenarioUseCase**: Import YAML scenarios
- **ExportScenarioUseCase**: Export to YAML format

### Game Session Management
- **StartGameSessionUseCase**: Begin new game session
- **MakeChoiceUseCase**: Process player choice
- **PauseGameSessionUseCase**: Pause active session
- **ResumeGameSessionUseCase**: Resume paused session
- **EndGameSessionUseCase**: Complete session
- **GetSessionStatsUseCase**: Calculate session statistics

### Badge Management
- **CreateBadgeConfigurationUseCase**: Define new badges
- **GetBadgeConfigurationsUseCase**: List available badges
- **GetBadgeConfigurationsByAxisUseCase**: Filter by compass axis
- **AwardBadgeUseCase**: Award badge to user
- **ImportBadgeConfigurationUseCase**: Import from YAML
- **ExportBadgeConfigurationUseCase**: Export to YAML

### Media Management
- **UploadMediaAssetUseCase**: Upload files to blob storage
- **GetMediaAssetUseCase**: Retrieve media metadata
- **DeleteMediaAssetUseCase**: Remove media files
- **ValidateMediaReferencesUseCase**: Check scenario media links

### Character Management
- **CreateCharacterMapUseCase**: Map characters to media
- **UpdateCharacterMapUseCase**: Modify character mappings
- **DeleteCharacterMapUseCase**: Remove character maps

## Port Interfaces (Defined in Application)

The application layer defines **port interfaces** that infrastructure adapters must implement:

### Repository Ports
```csharp
public interface IScenarioRepository : IRepository<Scenario>
{
    Task<IEnumerable<Scenario>> GetByAgeGroupAsync(AgeGroup ageGroup);
    Task<IEnumerable<Scenario>> GetFeaturedAsync();
}
```

### Storage Ports
```csharp
public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string fileName);
    Task<Stream> DownloadAsync(string blobName);
    Task DeleteAsync(string blobName);
}
```

### External Service Ports
```csharp
public interface IStoryProtocolService
{
    Task<StoryProtocolMetadata> RegisterIpAssetAsync(...);
    Task<bool> IsRegisteredAsync(string contentId);
}
```

## Dependencies

### Domain Layer
The application depends on domain entities and business logic:
```xml
<ProjectReference Include="..\Mystira.App.Domain\Mystira.App.Domain.csproj" />
```

### Contracts
For DTOs and API contracts:
```xml
<ProjectReference Include="..\Mystira.App.Contracts\Mystira.App.Contracts.csproj" />
```

### Infrastructure.Data
For repository implementations:
```xml
<ProjectReference Include="..\Mystira.App.Infrastructure.Data\Mystira.App.Infrastructure.Data.csproj" />
```

### Infrastructure.Azure
For cloud service implementations:
```xml
<ProjectReference Include="..\Mystira.App.Infrastructure.Azure\Mystira.App.Infrastructure.Azure.csproj" />
```

### NuGet Packages
- **Entity Framework Core**: For repository pattern
- **AutoMapper**: DTO mapping (if used)
- **NJsonSchema**: Schema validation

## Usage Example

### From API Controller

```csharp
[ApiController]
[Route("api/scenarios")]
public class ScenariosController : ControllerBase
{
    private readonly CreateScenarioUseCase _createScenario;
    private readonly GetScenarioUseCase _getScenario;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
    {
        // Controller calls use case
        var scenario = await _createScenario.ExecuteAsync(request);
        return Ok(scenario);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var scenario = await _getScenario.ExecuteAsync(id);
        return Ok(scenario);
    }
}
```

### Dependency Injection

```csharp
// In Program.cs
builder.Services.AddScoped<StartGameSessionUseCase>();
builder.Services.AddScoped<MakeChoiceUseCase>();
builder.Services.AddScoped<CreateScenarioUseCase>();
// ...
```

## YAML Scenario Import Example

### Input YAML
```yaml
title: "The Enchanted Forest"
ageGroup: "Ages7to9"
scenes:
  - id: "forest_entrance"
    narrative: "You stand at the edge of a mystical forest..."
    choices:
      - text: "Enter bravely"
        nextSceneId: "deep_forest"
        echoLogs:
          - echoType: "Courage"
            strength: 5
        compassChanges:
          - axis: "Courage"
            value: 10
```

### Parser Workflow
1. **ScenarioParser** reads YAML
2. **Validation** checks against schema
3. **SceneParser** extracts scenes
4. **EchoLogParser** parses moral feedback
5. **CompassChangeParser** parses compass changes
6. **Domain Entity** created: `Scenario` with `Scene` objects

## Application-Level Validation

### Example: Scenario Validation
```csharp
public class ValidateScenarioUseCase
{
    public ValidationResult Execute(Scenario scenario)
    {
        var errors = new List<string>();

        // Business rule: Maximum 4 archetypes
        if (scenario.CharacterArchetypes.Count > 4)
            errors.Add("Scenario cannot have more than 4 archetypes");

        // Business rule: Age-appropriate content
        if (scenario.AgeGroup == AgeGroup.Ages4to6 && HasComplexMoralChoices(scenario))
            errors.Add("Content too complex for age group 4-6");

        return new ValidationResult(errors);
    }
}
```

## Transaction Management

Use cases coordinate transactions via Unit of Work:

```csharp
public class UpdateScenarioUseCase
{
    private readonly IScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task ExecuteAsync(string id, UpdateScenarioRequest request)
    {
        var scenario = await _repository.GetByIdAsync(id);

        // Apply updates (domain logic)
        scenario.Title = request.Title;
        scenario.UpdatedAt = DateTime.UtcNow;

        // Persist atomically
        await _repository.UpdateAsync(scenario);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

## Error Handling

Use cases throw domain-specific exceptions:

```csharp
public class StartGameSessionUseCase
{
    public async Task<GameSession> ExecuteAsync(string scenarioId, string userId)
    {
        var scenario = await _scenarioRepository.GetByIdAsync(scenarioId);

        if (scenario == null)
            throw new ScenarioNotFoundException(scenarioId);

        if (scenario.AgeGroup != user.PreferredAgeGroup)
            throw new AgeGroupMismatchException();

        // ...
    }
}
```

## Testing

Use cases are testable without infrastructure:

```csharp
[Fact]
public async Task StartGameSession_WithValidScenario_CreatesSession()
{
    // Arrange
    var mockRepository = new Mock<IGameSessionRepository>();
    var useCase = new StartGameSessionUseCase(mockRepository.Object);

    // Act
    var session = await useCase.ExecuteAsync("scenario-123", "user-456");

    // Assert
    Assert.NotNull(session);
    Assert.Equal("scenario-123", session.ScenarioId);
    mockRepository.Verify(r => r.AddAsync(It.IsAny<GameSession>()), Times.Once);
}
```

## Design Patterns

### Command Pattern
Each use case is a command that executes a business operation.

### Repository Pattern
Use cases access data through repository abstractions.

### Unit of Work
Ensures transactional consistency across multiple repositories.

### Dependency Injection
All dependencies injected via constructor (testability).

## Best Practices

1. **Single Responsibility**: Each use case does one thing
2. **Thin Layer**: Orchestrate, don't implement business logic
3. **Port Interfaces**: Define contracts, not implementations
4. **Validation**: Validate at application boundary
5. **Transactions**: Coordinate atomic operations
6. **Error Handling**: Throw meaningful exceptions

## Future Enhancements

- **CQRS**: Separate command and query use cases
- **MediatR**: Use mediator pattern for use case dispatch
- **FluentValidation**: More robust validation framework
- **Domain Events**: React to domain events in use cases

## Related Documentation

- **[Domain](../Mystira.App.Domain/README.md)** - Core business entities and logic
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Repository implementations
- **[API](../Mystira.App.Api/README.md)** - REST API endpoints that call use cases
- **[Contracts](../Mystira.App.Contracts/README.md)** - DTOs for use case inputs/outputs

## License

Copyright (c) 2025 Mystira. All rights reserved.
