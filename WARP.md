# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Build & Development Commands

### Build & Restore
```powershell
# Restore all dependencies
dotnet restore

# Build entire solution
dotnet build

# Build for Release
dotnet build --configuration Release

# Publish API for deployment
dotnet publish src/Mystira.App.Api -c Release -o ./publish/api

# Publish PWA for deployment
dotnet publish src/Mystira.App.PWA -c Release -o ./publish/pwa
```

### Run Applications

#### Backend API
```powershell
cd src/Mystira.App.Api
dotnet run
# Available at https://localhost:5001 and http://localhost:5000
# Swagger UI at https://localhost:5001/swagger
```

#### Admin API
```powershell
cd src/Mystira.App.Admin.Api
dotnet run
```

#### PWA Frontend
```powershell
cd src/Mystira.App.PWA
dotnet run
# Available at https://localhost:7000
```

#### Cosmos Console (Database Reporting)
```powershell
cd Mystira.App.CosmosConsole
dotnet run
```

### Testing

```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/DMfinity.Domain.Tests
dotnet test tests/DMfinity.Api.Tests
dotnet test tests/DMfinity.Infrastructure.Azure.Tests

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test with verbose output
dotnet test --filter "FullyQualifiedName~ScenarioTests" --verbosity normal
```

### Database Setup

The API automatically switches between database providers:
- **Local Development**: Uses in-memory database by default (no setup required)
- **Cloud**: Uses Azure Cosmos DB when `ConnectionStrings:CosmosDb` is configured

Database is automatically initialized on application startup via `EnsureCreatedAsync()`.

## Architecture Overview

### Solution Structure

**Mystira.App** is a .NET-based interactive storytelling platform with a clean architecture pattern:

```
src/
├── Mystira.App.Domain/              # Domain models & business logic (no dependencies)
├── Mystira.App.Infrastructure.Azure/ # Azure service integrations (Blob Storage, Email)
├── Mystira.App.Api/                 # Client-facing REST API (.NET 9.0)
├── Mystira.App.Admin.Api/           # Administrative API (separate from client)
└── Mystira.App.PWA/                 # Blazor WebAssembly frontend (.NET 8.0)
```

### Key Architectural Patterns

#### 1. Domain-Driven Design
- **Domain Layer** (`Mystira.App.Domain`): Pure C# models with business logic, no external dependencies
- Domain models include built-in validation (e.g., `Scenario.Validate()`)
- Uses "StringEnum" pattern for type-safe enums (e.g., `EchoType`, `Archetype`, `CoreAxis`)

#### 2. Service Layer Pattern
All business logic is encapsulated in services registered in `Program.cs`:
- Services are interface-based (e.g., `IScenarioApiService`, `IGameSessionApiService`)
- Scoped lifetime for request-based services
- Located in `src/Mystira.App.Api/Services/`

#### 3. Database Abstraction
- **DbContext**: `MystiraAppDbContext` handles both Cosmos DB and in-memory providers
- Automatic provider detection based on connection string presence
- Entity configuration supports both Cosmos DB containers and in-memory tables
- Custom value converters for complex types (lists, enums, dictionaries)

#### 4. API Separation
- **Client API** (`Mystira.App.Api`): Public-facing endpoints for game play
- **Admin API** (`Mystira.App.Admin.Api`): Separate API for administrative operations
- Prevents accidental exposure of admin functionality to clients

### Core Domain Concepts

#### Scenario System
- **Scenario**: Story template with branching scenes
- **Scene**: Individual story moments with choices (branches)
- **Branch**: Player choices that lead to different scenes
- **EchoLog**: Moral choice tracking (e.g., "honesty", "courage")
- **CompassChange**: Character development axis tracking

#### Game Session Management
- **GameSession**: Tracks active gameplay state
- Stores choice history, echo logs, compass values
- Supports pause/resume functionality
- Ties to Account and UserProfile (not child accounts - COPPA compliant)

#### Authentication Flow
- **Passwordless Authentication**: Email-based magic code system
- **PendingSignup**: Temporary signup storage (15-minute expiration)
- **Account**: Created with Auth0-compatible ID format (`auth0|<guid>`)
- Uses cookie-based authentication (not JWT) configured in `Program.cs`

### Infrastructure Layer

#### Azure Services (`Mystira.App.Infrastructure.Azure`)
- **Blob Storage**: Media asset management via `IAzureBlobService`
- **Email Service**: Azure Communication Services for passwordless auth
- **Health Checks**: Custom health checks for blob storage availability
- Extension methods pattern: `AddAzureBlobStorage()`, `AddAzureHealthChecks()`

#### Data Layer Patterns
- **Owned Entities**: Nested objects use EF Core's `OwnsMany`/`OwnsOne` (e.g., Scenario.Scenes)
- **Value Converters**: Custom converters for Cosmos DB compatibility (lists to CSV strings, dictionaries to JSON)
- **Partition Keys**: Cosmos DB partitioning strategy per entity (e.g., GameSession partitioned by AccountId)

### Frontend (PWA) Architecture

#### Blazor WebAssembly Structure
```
Components/  # Reusable UI components
Pages/       # Route-based pages (Home, SignUp, etc.)
Services/    # Frontend services (AuthService, GameSessionService, IndexedDbService)
Models/      # Client-side DTOs and view models
wwwroot/     # Static assets, service worker for PWA
```

#### Key Services
- **IApiClient**: HTTP client wrapper for backend API calls
- **IAuthService**: Manages authentication state
- **IGameSessionService**: Game session state management
- **IIndexedDbService**: Client-side persistence for offline support
- **ICharacterAssignmentService**: Character selection logic

## Development Guidelines

### Domain Model Changes
When adding or modifying domain models:
1. Update model in `Mystira.App.Domain/Models/`
2. Update `MystiraAppDbContext.cs` entity configuration
3. Add value converters if needed for complex types
4. Update both Cosmos DB and in-memory configurations
5. Create corresponding tests in `DMfinity.Domain.Tests`

### Adding API Endpoints
1. Create/update service interface in `Services/I*Service.cs`
2. Implement service in `Services/*Service.cs`
3. Register service in `Program.cs` with `AddScoped<IYourService, YourService>()`
4. Add controller endpoint in `Controllers/*Controller.cs`
5. Update Swagger documentation via XML comments

### Testing Approach
- **Domain Tests**: Unit tests using FluentAssertions and xUnit
- **API Tests**: Integration tests with in-memory database
- **Pattern**: Arrange-Act-Assert with descriptive test method names
- Example: `Validate_ReturnsFalse_WhenNextSceneIdIsInvalid()`

### Configuration Management
- **Development**: Uses `appsettings.Development.json` with in-memory database
- **Production**: Requires environment variables:
  - `ConnectionStrings__CosmosDb`: Cosmos DB connection string
  - `ConnectionStrings__AzureStorage`: Azure Storage connection string
  - `AzureCommunicationServices__ConnectionString`: Email service connection
  - `AzureCommunicationServices__SenderEmail`: Verified sender email

### CORS Configuration
CORS is configured in API `Program.cs` with allowed origins:
- `http://localhost:7000` (PWA dev HTTP)
- `https://localhost:7000` (PWA dev HTTPS)
- `https://mystiraapp.azurewebsites.net` (Azure hosting)
- `https://mystira.app` (Production domain)

Update the CORS policy when adding new frontend origins.

## CI/CD Pipeline

### GitHub Actions Workflows
Located in `.github/workflows/`:

#### API CI/CD (`mystira.app.api_ci.yml`)
- Triggers on push to `main` or `dev` branches
- Runs: restore → build → test → publish
- Deploys to Azure App Service on `develop` branch
- Uses .NET 9.0

#### Static Web App (`azure-static-web-apps-mango-water-04fdb1c03.yml`)
- Deploys PWA to Azure Static Web Apps
- Provides global CDN and offline support

### Manual Deployment
```powershell
# Deploy API to Azure App Service
az webapp deployment source config-zip `
  --resource-group mystira-app-rg `
  --name mystira-app-api `
  --src ./publish/api.zip

# Deploy PWA to Azure Static Web Apps
swa deploy ./publish/pwa --deployment-token $SWA_TOKEN
```

## Important Notes

### Cosmos DB Considerations
- Entity configurations must handle both Cosmos DB and in-memory database
- Check `Database.ProviderName` in `OnModelCreating` to conditionally apply Cosmos-specific config
- Cosmos DB uses container-based storage with partition keys
- Lists and dictionaries require custom value converters for serialization

### Authentication Architecture
- Currently uses cookie-based auth, NOT JWT (despite Auth0-compatible IDs)
- Passwordless flow is development-ready; production requires email service integration
- Admin endpoints should implement proper authorization checks
- Child accounts not supported - COPPA compliance enforced through design

### Testing Database Behavior
Tests use in-memory database which behaves differently from Cosmos DB:
- In-memory doesn't require partition keys
- Some Cosmos-specific features (like `ToContainer`) are skipped in tests
- Always test against real Cosmos DB in staging before production deployment

### Media Management
- Media files stored in Azure Blob Storage
- Metadata tracked in `MediaAsset` entities in database
- URLs generated via `MediaController` with blob name references
- Support for images, audio, video with metadata tags

### PWA Offline Support
- Service worker configured in `wwwroot/`
- IndexedDB service (`IIndexedDbService`) for client-side data persistence
- API calls should handle offline scenarios gracefully
- Test offline functionality during development
