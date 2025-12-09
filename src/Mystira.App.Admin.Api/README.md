# Mystira.App Admin API

Administrative API adapter for content management and system administration. This project serves as a **primary adapter** in the hexagonal architecture, providing admin-specific HTTP endpoints separate from the client-facing API.

## ✅ Hexagonal Architecture - FULLY COMPLIANT

**Layer**: **Presentation - Admin REST API Adapter (Primary/Driver)**

The Mystira.App.Admin.Api layer is a **primary adapter** (driver adapter) that:
- **Receives** HTTP requests from admin clients (web dashboard, admin tools)
- **Translates** HTTP requests into application use case calls
- **Delegates** all business logic to Application use cases
- **Returns** HTTP responses with appropriate status codes
- **Provides** admin-specific UI views (MVC)
- **Contains** ZERO business logic (thin controllers)

**Dependency Flow** (Correct ✅):
```
Admin Clients (Dashboard, Tools)
    ↓ HTTP requests
Admin API Controllers (THIS - Primary Adapter)
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
- ✅ **Separation from Client API** - Independent deployment and scaling

## Separation from Client API

This Admin API is **separate** from the client-facing API (`Mystira.App.Api`) to:
- ✅ **Enhanced Security** - Admin operations isolated from public endpoints
- ✅ **Independent Scaling** - Admin and client can scale separately
- ✅ **Better Maintainability** - Clear separation of concerns
- ✅ **Flexible Deployment** - Can deploy to different infrastructure
- ✅ **Shared Database** - Both APIs use same Cosmos DB (different concerns, same data)

See [Admin API Separation](../../docs/features/ADMIN_API_SEPARATION.md) for architectural details.

## Project Structure

```
Mystira.App.Admin.Api/
├── Controllers/
│   ├── AdminController.cs              # Admin dashboard endpoints
│   ├── ScenariosAdminController.cs     # Scenario admin operations
│   ├── MediaAdminController.cs         # Media admin operations
│   └── SystemAdminController.cs        # System configuration
├── Views/
│   └── Admin/                          # MVC views for dashboard
├── Services/
│   ├── IYamlImportService.cs           # Thin service interfaces
│   └── YamlImportService.cs            # Delegates to use cases
├── Program.cs                          # DI composition root
└── appsettings.json                    # Configuration
```

**What Admin Controllers Do** (Thin Layer ✅):
- Accept HTTP requests
- Validate model binding
- Call Application use cases
- Return HTTP status codes
- Render admin UI views
- Handle exceptions → HTTP errors

**What Admin Controllers Do NOT Do** (Zero Business Logic ✅):
- ❌ Business validation (Application does that)
- ❌ Database access (Infrastructure.Data does that)
- ❌ Complex orchestration (Use cases do that)
- ❌ YAML parsing logic (Use cases/parsers do that)

## Example: Thin Admin Controller

### Media Admin Controller (Primary Adapter)

```csharp
// Location: Admin.Api/Controllers/MediaAdminController.cs
using Mystira.App.Application.UseCases.Media;  // Use cases ✅
using Mystira.App.Contracts.Requests.Media;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin/api/[controller]")]
public class MediaAdminController : ControllerBase
{
    private readonly UploadMediaUseCase _uploadMediaUseCase;
    private readonly DeleteMediaUseCase _deleteMediaUseCase;
    private readonly GetMediaMetadataUseCase _getMediaMetadataUseCase;
    private readonly ILogger<MediaAdminController> _logger;

    public MediaAdminController(
        UploadMediaUseCase uploadMediaUseCase,      // Use case ✅
        DeleteMediaUseCase deleteMediaUseCase,       // Use case ✅
        GetMediaMetadataUseCase getMediaMetadataUseCase,
        ILogger<MediaAdminController> logger)
    {
        _uploadMediaUseCase = uploadMediaUseCase;
        _deleteMediaUseCase = deleteMediaUseCase;
        _getMediaMetadataUseCase = getMediaMetadataUseCase;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        try
        {
            // Validate HTTP input ✅
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            // Delegate to use case ✅
            using var stream = file.OpenReadStream();
            var mediaAsset = await _uploadMediaUseCase.ExecuteAsync(
                stream,
                file.FileName,
                file.ContentType);

            // Return HTTP response ✅
            return Ok(new {
                id = mediaAsset.Id,
                url = mediaAsset.Url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media");
            return StatusCode(500, "Upload failed");
        }
    }

    [HttpDelete("{blobName}")]
    public async Task<IActionResult> DeleteMedia(string blobName)
    {
        try
        {
            // Thin delegation ✅
            await _deleteMediaUseCase.ExecuteAsync(blobName);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media {BlobName}", blobName);
            return StatusCode(500, "Delete failed");
        }
    }
}
```

## Admin Dashboard (MVC Views)

Admin API also provides web UI for administration:

```csharp
// Location: Admin.Api/Controllers/AdminController.cs
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly GetAllScenariosUseCase _getAllScenariosUseCase;

    public AdminController(GetAllScenariosUseCase getAllScenariosUseCase)
    {
        _getAllScenariosUseCase = getAllScenariosUseCase;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();  // Renders admin dashboard
    }

    [HttpGet("scenarios")]
    public async Task<IActionResult> Scenarios()
    {
        // Delegate to use case ✅
        var scenarios = await _getAllScenariosUseCase.ExecuteAsync();
        return View(scenarios);  // Renders scenario management page
    }
}
```

## YAML Import Service

Admin API includes YAML import for bulk content management:

```csharp
// Location: Admin.Api/Services/YamlImportService.cs
using Mystira.App.Application.UseCases.Scenarios;  // Use cases ✅
using Mystira.App.Application.Parsers;              // Parsers ✅

public class YamlImportService : IYamlImportService
{
    private readonly IYamlParser _yamlParser;
    private readonly CreateScenarioUseCase _createScenarioUseCase;
    private readonly ILogger<YamlImportService> _logger;

    public async Task<ImportResult> ImportScenariosAsync(Stream yamlStream)
    {
        // Parse YAML (parser handles this) ✅
        var scenarios = await _yamlParser.ParseScenariosAsync(yamlStream);

        var result = new ImportResult();

        foreach (var scenario in scenarios)
        {
            try
            {
                // Delegate to use case ✅
                await _createScenarioUseCase.ExecuteAsync(scenario);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import scenario {Title}", scenario.Title);
                result.Failures.Add(scenario.Title);
            }
        }

        return result;
    }
}
```

**Service Responsibilities**:
- Coordinate YAML parsing and use case invocation
- Handle bulk operations
- Still ZERO business logic (delegation only)

## Dependency Injection (Composition Root)

Like the main API, `Program.cs` is the **only** file that knows about Infrastructure:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register use cases (from Application)
builder.Services.AddScoped<UploadMediaUseCase>();
builder.Services.AddScoped<DeleteMediaUseCase>();
builder.Services.AddScoped<CreateScenarioUseCase>();
builder.Services.AddScoped<GetAllScenariosUseCase>();
// ... more use cases

// Register parsers (from Application)
builder.Services.AddScoped<IYamlParser, YamlParser>();

// Wire port interfaces to Infrastructure implementations
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBlobService, AzureBlobService>();

// Register admin services
builder.Services.AddScoped<IYamlImportService, YamlImportService>();

// Shared DbContext with client API
builder.Services.AddDbContext<MystiraAppDbContext>(options =>
    options.UseCosmos(
        builder.Configuration.GetConnectionString("CosmosDb"),
        databaseName: "MystiraAppDb"));  // Same DB as client API ✅

var app = builder.Build();
app.Run();
```

## API Endpoints

### Admin Dashboard (UI)
- `GET /admin` - Main dashboard
- `GET /admin/login` - Login page
- `GET /admin/scenarios` - Scenario management
- `GET /admin/media` - Media management
- `GET /admin/charactermaps` - Character maps

### Scenario Admin
- `GET /admin/api/scenarios` - List all scenarios
- `POST /admin/api/scenarios` - Create scenario
- `PUT /admin/api/scenarios/{id}` - Update scenario
- `DELETE /admin/api/scenarios/{id}` - Delete scenario
- `POST /admin/api/scenarios/import` - Bulk import from YAML

### Media Admin
- `POST /admin/api/media/upload` - Upload media file
- `DELETE /admin/api/media/{blobName}` - Delete media
- `GET /admin/api/media` - List all media
- `POST /admin/api/media/bulk-upload` - Bulk media upload

### System Admin
- `GET /admin/api/system/status` - System health status
- `POST /admin/api/system/badge-config` - Configure badges
- `GET /admin/api/system/stats` - System statistics

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
  "AdminSettings": {
    "RequireHttps": true,
    "AllowedOrigins": ["https://admin.mystira.app"]
  }
}
```

**Note**: Use the **same connection strings** as the client API since they share the database.

## Authentication & Authorization

### Admin-Only Access

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});
```

All admin controllers require `[Authorize(Roles = "Admin")]`.

## Deployment

### Local Development

```bash
dotnet restore
dotnet run
```

Admin dashboard: `https://localhost:7001/admin`

**Port**: 7001 (Client API uses 7000)

### Docker

```bash
docker build -t mystira-admin-api .
docker run -p 8081:80 mystira-admin-api
```

### Azure App Service

```bash
az webapp up \
  --name mystira-admin-api \
  --resource-group mystira-rg \
  --runtime "DOTNETCORE:9.0"
```

Deploy to **separate App Service** from client API for better security and scaling.

## Security Considerations

### Admin API Isolation

- ✅ **Separate deployment** - Not exposed to public internet directly
- ✅ **Role-based auth** - Only admin users can access
- ✅ **HTTPS only** - Enforce secure connections
- ✅ **IP restrictions** - Limit access to known IPs (Azure App Service)
- ✅ **Audit logging** - Track all admin operations

### Shared Database Access

- ✅ **Same Cosmos DB** - Shared with client API
- ✅ **Same entities** - No data duplication
- ✅ **Different concerns** - Admin writes, client reads
- ✅ **Cosmos DB partition strategy** - Optimized for both access patterns

## Architectural Compliance Verification

Verify the Admin API layer follows hexagonal architecture:

```bash
# Check controllers only reference Application (use cases)
grep -r "using Mystira.App.Infrastructure" Controllers/
# Expected: (no output - controllers don't reference Infrastructure)

# Check Program.cs is the only file with Infrastructure references
find . -name "*.cs" -exec grep -l "Infrastructure\.(Data|Azure)" {} \; | grep -v Program.cs
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

## Testing

### Admin Controller Testing

```csharp
[Fact]
public async Task UploadMedia_WithValidFile_ReturnsOk()
{
    // Arrange
    var mockUseCase = new Mock<UploadMediaUseCase>();
    var mockFile = new Mock<IFormFile>();

    mockFile.Setup(f => f.FileName).Returns("test.mp3");
    mockFile.Setup(f => f.ContentType).Returns("audio/mpeg");
    mockFile.Setup(f => f.Length).Returns(1024);
    mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

    mockUseCase
        .Setup(u => u.ExecuteAsync(It.IsAny<Stream>(), "test.mp3", "audio/mpeg"))
        .ReturnsAsync(new MediaAsset { Id = "test-123", Url = "https://..." });

    var controller = new MediaAdminController(
        mockUseCase.Object,
        Mock.Of<DeleteMediaUseCase>(),
        Mock.Of<GetMediaMetadataUseCase>(),
        mockLogger.Object);

    // Act
    var result = await controller.UploadMedia(mockFile.Object);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(okResult.Value);
}
```

## Related Documentation

- **[Application](../Mystira.App.Application/README.md)** - Use cases invoked by this API
- **[API](../Mystira.App.Api/README.md)** - Client-facing API (same pattern)
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Shared data layer
- **[Infrastructure.Azure](../Mystira.App.Infrastructure.Azure/README.md)** - Shared Azure services
- **[Admin API Separation](../../docs/features/ADMIN_API_SEPARATION.md)** - Architectural details

## Summary

**What This Layer Does**:
- ✅ Receives HTTP requests from admin clients
- ✅ Translates requests into use case invocations
- ✅ Provides admin dashboard UI (MVC views)
- ✅ Returns HTTP responses with proper status codes
- ✅ Wires Infrastructure implementations via DI
- ✅ Deployed separately from client API

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
- ✅ **Security isolation** - Separate from client API
- ✅ **Shared data** - Same database, different access patterns

## License

Copyright (c) 2025 Mystira. All rights reserved.
