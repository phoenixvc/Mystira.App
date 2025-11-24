# Mystira.App API

RESTful HTTP/Web API adapter for Mystira.App - Dynamic Story App for Child Development. This project serves as a **primary adapter** in the hexagonal architecture, translating HTTP requests into application use case invocations.

## ✅ Hexagonal Architecture - FULLY COMPLIANT

**Layer**: **Presentation - REST API Adapter (Primary/Driver)**

The Mystira.App.API layer is a **primary adapter** (driver adapter) that:
- **Receives** HTTP requests from external clients (MAUI app, web browsers)
- **Translates** HTTP requests into application use case calls
- **Delegates** all business logic to Application use cases
- **Returns** HTTP responses with appropriate status codes
- **Registers** infrastructure implementations via dependency injection
- **Contains** ZERO business logic (thin controllers)

**Dependency Flow** (Correct ✅):
```
External Clients (MAUI, Web, Mobile)
    ↓ HTTP requests
API Controllers (THIS - Primary Adapter)
    ↓ invokes
Application Use Cases
    ↓ uses
Application.Ports (Interfaces)
    ↑ implemented by (DI at runtime)
Infrastructure Implementations
```

**Key Principles**:
- ✅ **Thin Controllers** - Only HTTP concerns, zero business logic
- ✅ **Use Case Orchestration** - Delegates to Application layer
- ✅ **Dependency Injection** - Wires Infrastructure implementations
- ✅ **Clean Architecture** - No direct Infrastructure references in controllers
- ✅ **Composition Root** - Program.cs is the only place that knows about Infrastructure

## Project Structure

```
Mystira.App.Api/
├── Controllers/
│   ├── ScenariosController.cs          # Scenario endpoints
│   ├── GameSessionsController.cs       # Game session endpoints
│   ├── UserProfilesController.cs       # User profile endpoints
│   ├── AccountsController.cs           # Account management
│   └── MediaController.cs              # Media upload/management
├── Services/
│   ├── IAccountApiService.cs           # Thin service interfaces
│   ├── AccountApiService.cs            # Delegates to use cases
│   ├── IUserProfileApiService.cs
│   └── UserProfileApiService.cs        # Coordinates multiple use cases
├── Program.cs                          # DI composition root
└── appsettings.json                    # Configuration
```

**What Controllers Do** (Thin Layer ✅):
- Accept HTTP requests
- Validate model binding
- Call Application use cases
- Return HTTP status codes
- Handle exceptions → HTTP errors

**What Controllers Do NOT Do** (Zero Business Logic ✅):
- ❌ Business validation (Application does that)
- ❌ Database access (Infrastructure.Data does that)
- ❌ Complex orchestration (Use cases do that)
- ❌ Calculations or transformations (Domain/Application does that)

## Example: Thin Controller Pattern

### Scenario Controller (Primary Adapter)

```csharp
// Location: Api/Controllers/ScenariosController.cs
using Mystira.App.Application.UseCases.Scenarios;  // Use cases ✅
using Mystira.App.Contracts.Requests.Scenarios;
using Mystira.App.Contracts.Responses;

[ApiController]
[Route("api/[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly GetScenarioUseCase _getScenarioUseCase;
    private readonly CreateScenarioUseCase _createScenarioUseCase;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(
        GetScenarioUseCase getScenarioUseCase,      // Use case ✅
        CreateScenarioUseCase createScenarioUseCase, // Use case ✅
        ILogger<ScenariosController> _logger)
    {
        _getScenarioUseCase = getScenarioUseCase;
        _createScenarioUseCase = createScenarioUseCase;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScenarioResponse>> GetScenario(string id)
    {
        try
        {
            // Thin controller - just delegates to use case ✅
            var scenario = await _getScenarioUseCase.ExecuteAsync(id);

            if (scenario == null)
                return NotFound($"Scenario {id} not found");

            return Ok(scenario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scenario {ScenarioId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [Authorize] // Only DMs can create scenarios
    public async Task<ActionResult<ScenarioResponse>> CreateScenario(
        [FromBody] CreateScenarioRequest request)
    {
        try
        {
            // Validate model binding (HTTP concern) ✅
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Delegate business logic to use case ✅
            var scenario = await _createScenarioUseCase.ExecuteAsync(request);

            // Return HTTP 201 Created ✅
            return CreatedAtAction(
                nameof(GetScenario),
                new { id = scenario.Id },
                scenario);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scenario");
            return StatusCode(500, "Internal server error");
        }
    }
}
```

**Controller Responsibilities** (HTTP Only):
- ✅ HTTP method routing (`[HttpGet]`, `[HttpPost]`)
- ✅ Model binding validation (`ModelState.IsValid`)
- ✅ Calling use cases
- ✅ HTTP status codes (`Ok`, `NotFound`, `BadRequest`, `Created`)
- ✅ Exception handling → HTTP errors

**NOT Controller Responsibilities** (Business Logic):
- ❌ Scenario validation logic (use case does this)
- ❌ Database access (Infrastructure.Data does this)
- ❌ File storage (Infrastructure.Azure does this)
- ❌ Age group validation (Domain/Application does this)

## API Service Pattern

Some controllers use thin API services that coordinate multiple use cases:

```csharp
// Location: Api/Services/UserProfileApiService.cs
using Mystira.App.Application.UseCases.UserProfiles;  // Use cases ✅

public class UserProfileApiService : IUserProfileApiService
{
    private readonly CreateUserProfileUseCase _createUserProfileUseCase;
    private readonly UpdateUserProfileUseCase _updateUserProfileUseCase;
    private readonly GetUserProfileUseCase _getUserProfileUseCase;
    private readonly DeleteUserProfileUseCase _deleteUserProfileUseCase;

    public UserProfileApiService(
        CreateUserProfileUseCase createUserProfileUseCase,
        UpdateUserProfileUseCase updateUserProfileUseCase,
        GetUserProfileUseCase getUserProfileUseCase,
        DeleteUserProfileUseCase deleteUserProfileUseCase)
    {
        _createUserProfileUseCase = createUserProfileUseCase;
        _updateUserProfileUseCase = updateUserProfileUseCase;
        _getUserProfileUseCase = getUserProfileUseCase;
        _deleteUserProfileUseCase = deleteUserProfileUseCase;
    }

    public async Task<UserProfile> CreateProfileAsync(CreateUserProfileRequest request)
    {
        // Thin delegation - no business logic ✅
        return await _createUserProfileUseCase.ExecuteAsync(request);
    }

    public async Task<UserProfile?> GetProfileByIdAsync(string id)
    {
        return await _getUserProfileUseCase.ExecuteAsync(id);
    }

    // ... other thin delegation methods
}
```

**API Service Purpose**:
- Coordinate multiple related use cases
- Provide single injection point for controllers
- Still ZERO business logic (just delegation)

## Dependency Injection (Composition Root)

`Program.cs` is the **only** file that knows about Infrastructure implementations:

```csharp
using Mystira.App.Application.UseCases.Scenarios;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Application.Ports.Storage;
using Mystira.App.Application.Ports.Messaging;
using Mystira.App.Infrastructure.Data.Repositories;
using Mystira.App.Infrastructure.Azure.Services;
using Mystira.App.Infrastructure.Discord.Services;

var builder = WebApplication.CreateBuilder(args);

// Register use cases (from Application)
builder.Services.AddScoped<GetScenarioUseCase>();
builder.Services.AddScoped<CreateScenarioUseCase>();
builder.Services.AddScoped<UpdateScenarioUseCase>();
builder.Services.AddScoped<DeleteScenarioUseCase>();
// ... more use cases

// Wire port interfaces to Infrastructure implementations
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();          // Data
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();    // Data
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();                          // Data
builder.Services.AddScoped<IBlobService, AzureBlobService>();                   // Azure
builder.Services.AddScoped<IAudioTranscodingService, FfmpegAudioTranscodingService>(); // Azure
builder.Services.AddScoped<IMessagingService, DiscordBotService>();             // Discord

// Register API services (thin coordinators)
builder.Services.AddScoped<IUserProfileApiService, UserProfileApiService>();
builder.Services.AddScoped<IAccountApiService, AccountApiService>();

// DbContext
builder.Services.AddDbContext<MystiraAppDbContext>(options =>
    options.UseCosmos(
        builder.Configuration.GetConnectionString("CosmosDb"),
        databaseName: "MystiraAppDb"));

// Azure Blob Storage
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    return new BlobServiceClient(connectionString);
});

var app = builder.Build();
app.Run();
```

**Benefits of This Pattern**:
- ✅ Controllers know ZERO about Infrastructure
- ✅ Easy to swap implementations (Azure → AWS, Cosmos → SQL Server)
- ✅ Testable (mock use cases, not infrastructure)
- ✅ Single place to change wiring (Program.cs)

## API Endpoints

### Scenarios
- `GET /api/scenarios` - List scenarios with filtering
- `GET /api/scenarios/{id}` - Get specific scenario
- `POST /api/scenarios` - Create new scenario (Auth required)
- `PUT /api/scenarios/{id}` - Update scenario (Auth required)
- `DELETE /api/scenarios/{id}` - Delete scenario (Auth required)
- `GET /api/scenarios/age-group/{ageGroup}` - Age-appropriate scenarios
- `GET /api/scenarios/featured` - Featured scenarios

### Game Sessions
- `POST /api/gamesessions` - Start new session (Auth required)
- `GET /api/gamesessions/{id}` - Get session details (Auth required)
- `GET /api/gamesessions/dm/{dmName}` - DM's sessions (Auth required)
- `POST /api/gamesessions/choice` - Make choice (Auth required)
- `POST /api/gamesessions/{id}/pause` - Pause session (Auth required)
- `POST /api/gamesessions/{id}/resume` - Resume session (Auth required)
- `POST /api/gamesessions/{id}/end` - End session (Auth required)

### User Profiles
- `POST /api/userprofiles` - Create profile (Auth required)
- `GET /api/userprofiles/{id}` - Get profile (Auth required)
- `PUT /api/userprofiles/{id}` - Update profile (Auth required)
- `DELETE /api/userprofiles/{id}` - Delete profile (Auth required)
- `GET /api/userprofiles` - List all profiles (Auth required)
- `GET /api/userprofiles/non-guest` - Non-guest profiles (Auth required)

### Accounts
- `POST /api/accounts` - Create account
- `GET /api/accounts/{id}` - Get account (Auth required)
- `PUT /api/accounts/{id}` - Update account (Auth required)
- `POST /api/accounts/{id}/profiles` - Link profiles (Auth required)

### Media
- `POST /api/media/upload` - Upload media (Auth required)
- `GET /api/media/{blobName}` - Get media URL
- `DELETE /api/media/{blobName}` - Delete media (Auth required)

### Health
- `GET /health` - Comprehensive health check
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://...;AccountKey=...;",
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=..."
  },
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "https://mystira.app",
    "Audience": "https://mystira.app"
  },
  "Azure": {
    "BlobStorage": {
      "ContainerName": "mystira-app-media"
    }
  },
  "Discord": {
    "BotToken": "your-bot-token"
  }
}
```

### Environment Variables (Production)

Set via Azure App Service configuration:
- `ConnectionStrings__CosmosDb`
- `ConnectionStrings__AzureStorage`
- `Jwt__Key`
- `Discord__BotToken`

## Authentication & Authorization

### JWT Authentication

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
```

### COPPA Compliance

- **DM-only accounts** - No child accounts
- **Age-appropriate content** - Scenarios filtered by age group
- **Parental controls** - DMs manage all child profiles

## Health Checks

Comprehensive health monitoring:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MystiraAppDbContext>("cosmos-db")
    .AddAzureBlobStorage(
        builder.Configuration.GetConnectionString("AzureStorage"),
        name: "azure-blob-storage");
```

Access:
- `/health` - Overall status
- `/health/ready` - Kubernetes readiness probe
- `/health/live` - Kubernetes liveness probe

## Deployment

### Local Development

```bash
dotnet restore
dotnet run
```

Access Swagger UI at: `https://localhost:5001`

### Docker

```bash
docker build -t mystira-app-api .
docker run -p 8080:80 mystira-app-api
```

### Azure App Service

```bash
# Deploy using Azure CLI
az webapp up \
  --name mystira-app-api \
  --resource-group mystira-rg \
  --runtime "DOTNETCORE:9.0"
```

Or use GitHub Actions (see `.github/workflows/deploy.yml`)

## Testing

### Controller Testing

```csharp
[Fact]
public async Task GetScenario_WithValidId_ReturnsOk()
{
    // Arrange
    var mockUseCase = new Mock<GetScenarioUseCase>();
    mockUseCase
        .Setup(u => u.ExecuteAsync("test-123"))
        .ReturnsAsync(new Scenario { Id = "test-123", Title = "Test" });

    var controller = new ScenariosController(
        mockUseCase.Object,
        mockLogger.Object);

    // Act
    var result = await controller.GetScenario("test-123");

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    var scenario = Assert.IsType<Scenario>(okResult.Value);
    Assert.Equal("test-123", scenario.Id);
}
```

## Architectural Compliance Verification

Verify the API layer follows hexagonal architecture:

```bash
# Check controllers only reference Application (use cases)
grep -r "using Mystira.App.Infrastructure" Controllers/
# Expected: (no output - controllers don't reference Infrastructure)

# Check Program.cs is the only file with Infrastructure references
find . -name "*.cs" -exec grep -l "Infrastructure\.(Data|Azure|Discord)" {} \; | grep -v Program.cs
# Expected: (no output - only Program.cs knows about Infrastructure)

# Check controllers are thin (delegate to use cases)
grep -r "new DbContext\|SaveChangesAsync\|BlobClient" Controllers/
# Expected: (no output - controllers don't do data access)
```

**Results**:
- ✅ Controllers reference Application.UseCases, not Infrastructure
- ✅ Program.cs is the only composition root
- ✅ Controllers are thin (no business logic)
- ✅ Full hexagonal architecture compliance

## Related Documentation

- **[Application](../Mystira.App.Application/README.md)** - Use cases invoked by this API
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Data implementations wired in DI
- **[Infrastructure.Azure](../Mystira.App.Infrastructure.Azure/README.md)** - Azure implementations wired in DI
- **[Domain](../Mystira.App.Domain/README.md)** - Domain entities returned in responses
- **[Contracts](../Mystira.App.Contracts/README.md)** - Request/Response DTOs

## Summary

**What This Layer Does**:
- ✅ Receives HTTP requests from external clients
- ✅ Translates requests into use case invocations
- ✅ Returns HTTP responses with proper status codes
- ✅ Wires Infrastructure implementations via DI (Program.cs only)
- ✅ Provides RESTful API with OpenAPI documentation

**What This Layer Does NOT Do**:
- ❌ Contain business logic (Application/Domain does that)
- ❌ Access databases directly (Infrastructure.Data does that)
- ❌ Access cloud storage directly (Infrastructure.Azure does that)
- ❌ Perform validations (Application/Domain does that)

**Key Success Metrics**:
- ✅ **Thin controllers** - Zero business logic, only HTTP concerns
- ✅ **Use case delegation** - All logic in Application layer
- ✅ **Composition root** - Only Program.cs knows Infrastructure
- ✅ **Testability** - Mock use cases, not infrastructure
- ✅ **Swappability** - Change implementations in Program.cs

## License

Copyright (c) 2025 Mystira. All rights reserved.
