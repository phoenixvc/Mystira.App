# Mystira.App API

Backend API infrastructure for Mystira.App - Dynamic Story App for Child Development

## Overview

The Mystira.App API provides the core backend services for the Mystira.App MAUI mobile application, implementing:

- **Scenario Management**: CRUD operations for D&D scenarios with branching narratives
- **Game Session Management**: Real-time session tracking with choice history and moral compass
- **User Profile Management**: DM account management with COPPA compliance
- **Media Asset Management**: Azure Blob Storage integration for multimedia content
- **Echo & Compass Tracking**: Moral choice tracking and achievement system
- **Health Monitoring**: Comprehensive health checks for production deployment

## Architecture

- **.NET 9.0 Web API** with Azure App Service deployment
- **Azure Cosmos DB** for structured data persistence
- **Azure Blob Storage** for multimedia assets
- **JWT Authentication** for DM-only access (no child accounts per COPPA)
- **RESTful API Design** with OpenAPI/Swagger documentation
- **Health Checks** for monitoring and diagnostics

## Prerequisites

- .NET 9.0 SDK
- Azure subscription (for cloud deployment)
- Visual Studio 2022 or VS Code

## Getting Started

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Mystira.App.Api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure settings**
   Update `appsettings.Development.json` with your Azure connection strings:
   ```json
   {
     "ConnectionStrings": {
       "CosmosDb": "your-cosmos-db-connection-string",
       "AzureStorage": "your-azure-storage-connection-string"
     },
     "Jwt": {
       "Key": "your-jwt-secret-key"
     }
   }
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access Swagger UI**
   Navigate to `https://localhost:5001` or `http://localhost:5000`

### Docker Deployment

1. **Build Docker image**
   ```bash
   docker build -t mystira-app-api .
   ```

2. **Run container**
   ```bash
   docker run -p 8080:80 -e ConnectionStrings__CosmosDb="your-connection-string" mystira-app-api
   ```

## API Endpoints

### Scenarios
- `GET /api/scenarios` - Get scenarios with filtering
- `GET /api/scenarios/{id}` - Get specific scenario
- `POST /api/scenarios` - Create new scenario (Auth required)
- `PUT /api/scenarios/{id}` - Update scenario (Auth required)
- `DELETE /api/scenarios/{id}` - Delete scenario (Auth required)
- `GET /api/scenarios/age-group/{ageGroup}` - Get age-appropriate scenarios
- `GET /api/scenarios/featured` - Get featured scenarios

### Game Sessions
- `POST /api/gamesessions` - Start new session (Auth required)
- `GET /api/gamesessions/{id}` - Get session details (Auth required)
- `GET /api/gamesessions/dm/{dmName}` - Get DM's sessions (Auth required)
- `POST /api/gamesessions/choice` - Make choice in session (Auth required)
- `POST /api/gamesessions/{id}/pause` - Pause session (Auth required)
- `POST /api/gamesessions/{id}/resume` - Resume session (Auth required)
- `POST /api/gamesessions/{id}/end` - End session (Auth required)
- `GET /api/gamesessions/{id}/stats` - Get session statistics (Auth required)
- `DELETE /api/gamesessions/{id}` - Delete session (Auth required)

### User Profiles
- `POST /api/userprofiles` - Create DM profile
- `GET /api/userprofiles/{name}` - Get profile (Auth required)
- `PUT /api/userprofiles/{name}` - Update profile (Auth required)
- `DELETE /api/userprofiles/{name}` - Delete profile (Auth required)
- `POST /api/userprofiles/{name}/complete-onboarding` - Complete onboarding (Auth required)

### Media
- `POST /api/media/upload` - Upload media file (Auth required)
- `GET /api/media/{blobName}/url` - Get media URL
- `GET /api/media/{blobName}/download` - Download media file
- `GET /api/media` - List media files (Auth required)
- `DELETE /api/media/{blobName}` - Delete media file (Auth required)

### Health
- `GET /api/health` - Comprehensive health check
- `GET /api/health/ready` - Readiness probe
- `GET /api/health/live` - Liveness probe

## Authentication

The API uses JWT Bearer token authentication for DM accounts. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Data Models

### Scenario
Core adventure definition with scenes, choices, and branching narratives.

### GameSession
Session lifecycle tracking with choice history, echo logging, and compass values.

### UserProfile
DM account information with fantasy theme preferences and age group settings.

### EchoLog
Moral choice tracking with strength values and echo types from master list.

### CompassTracking
Real-time moral axis tracking with historical changes and threshold monitoring.

## Azure Deployment

### Infrastructure as Code

Use the provided Bicep templates for Azure deployment:

```bash
# Deploy all infrastructure
az deployment group create \
  --resource-group mystira-app-prod-rg \
  --template-file Infrastructure/Azure/main.bicep \
  --parameters environment=prod
```

### CI/CD Pipeline

GitHub Actions workflow is configured for:
- **Build & Test** on every push
- **Deploy to Development** on develop branch
- **Deploy to Production** on main branch
- **Infrastructure Deployment** with commit message `[deploy-infra]`

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment name (Development, Staging, Production)
- `ConnectionStrings__CosmosDb` - Cosmos DB connection string
- `ConnectionStrings__AzureStorage` - Azure Storage connection string
- `Jwt__Key` - JWT signing key
- `Jwt__Issuer` - JWT issuer claim
- `Jwt__Audience` - JWT audience claim

## Security

- **HTTPS Only** - All endpoints require HTTPS in production
- **JWT Authentication** - DM accounts only, no child accounts (COPPA compliance)
- **CORS Configuration** - Restricted to known frontend domains
- **Input Validation** - Comprehensive validation on all endpoints
- **Content Filtering** - Age-appropriate content validation
- **Data Encryption** - All personal data encrypted in transit and at rest

## Monitoring

- **Health Checks** - Database and Blob Storage connectivity
- **Application Insights** - Performance and error monitoring
- **Structured Logging** - JSON formatted logs for Azure monitoring

## Development Guidelines

### Code Structure
- **Controllers** - API endpoints with input validation
- **Services** - Business logic and data access
- **Models** - Request/response DTOs
- **Infrastructure** - Cross-cutting concerns (health checks, etc.)

### Testing
- Unit tests for all services
- Integration tests for API endpoints
- Health check validation

### Content Safety
- All scenarios validated against age appropriateness
- Echo/compass values within specified ranges
- Maximum limits enforced (4 archetypes, 4 compass axes per scenario)

## 🔍 Architectural Analysis

### Current State Assessment

**File Count**: 77 C# files
**Project References**: 7 (⚠️ too many)
- Domain
- Contracts
- Application ✅
- Infrastructure.Data ⚠️
- Infrastructure.Azure ⚠️
- Infrastructure.Discord ⚠️
- Shared ✅

**Folders**:
- Controllers/ ✅
- Services/ ⚠️ (Business logic in API layer!)
- Repositories/ ❌ (Should NOT be here!)
- Adapters/ ⚠️
- Models/ (redundant with Contracts)

### ⚠️ Architectural Issues Found

#### 1. **Business Logic in API Layer** (CRITICAL)
**Location**: `Services/` folder with 47 service files

**Issue**: API contains business logic services that should be Use Cases in Application layer:
- `ScenarioApiService.cs` (32KB!)
- `GameSessionApiService.cs` (15KB)
- `MediaMetadataService.cs`
- `CharacterMapFileService.cs`
- `BundleService.cs`
- And 42 more...

**Impact**:
- ❌ Business logic scattered across API and Application layers
- ❌ Violates Single Responsibility (API should only handle HTTP concerns)
- ❌ Can't reuse business logic in other interfaces (gRPC, CLI, background jobs)
- ❌ Hard to test business logic independently of HTTP context

**Recommendation**:
- **MOVE** all `*ApiService` classes to `Application/UseCases/`
- API Controllers should be **thin** - only handle:
  - HTTP request/response
  - Model validation
  - Calling use cases
  - HTTP status codes
- Rename services to use cases (e.g., `ScenarioApiService` → `CreateScenarioUseCase`, `GetScenarioUseCase`)

#### 2. **Repositories in API Layer** (CRITICAL)
**Location**: `Repositories/` folder

**Issue**: API contains repository implementations:
```
Repositories/
├── CharacterMapFileRepository.cs
├── CharacterMediaMetadataFileRepository.cs
├── MediaMetadataFileRepository.cs
├── ICharacterMapFileRepository.cs
└── ...
```

**Impact**:
- ❌ Data access logic in presentation layer
- ❌ Violates layered architecture
- ❌ Tight coupling to persistence

**Recommendation**:
- **MOVE** repositories to `Infrastructure.Data/Repositories/`
- **MOVE** interfaces to `Application/Ports/Data/`
- API should NEVER contain data access code

#### 3. **Too Many Infrastructure References** (HIGH)
**Location**: `Mystira.App.Api.csproj` lines 27-31

**Issue**: API directly references 3 infrastructure projects:
```xml
<ProjectReference Include="..\Mystira.App.Infrastructure.Azure\..." />
<ProjectReference Include="..\Mystira.App.Infrastructure.Data\..." />
<ProjectReference Include="..\Mystira.App.Infrastructure.Discord\..." />
```

**Impact**:
- ⚠️ API knows about infrastructure implementation details
- ⚠️ Can't swap implementations without changing API
- ⚠️ Violates Dependency Inversion

**Recommendation**:
- API should ONLY reference:
  - Domain (for entities in responses)
  - Application (for use cases)
  - Contracts (for DTOs)
  - Shared (for middleware)
- Infrastructure wired via DI in `Program.cs`
- No direct infrastructure references

#### 4. **Adapters Folder** (MEDIUM)
**Location**: `Adapters/` folder

**Issue**: Contains `MediaMetadataServiceAdapter.cs` - unclear purpose

**Impact**:
- ⚠️ Adapter pattern in presentation layer is odd
- ⚠️ Suggests missing abstraction

**Recommendation**:
- If adapting external service, move to appropriate Infrastructure project
- If wrapping application service, remove and call use case directly

### ✅ What's Working Well

1. **Controllers** - Well-organized API endpoints
2. **Application Reference** - Correct to reference Application layer
3. **Swagger/OpenAPI** - Good API documentation
4. **Health Checks** - Monitoring infrastructure

## 📋 Refactoring TODO

### 🔴 Critical Priority (MUST FIX)

- [ ] **Move all business logic to Application layer**
  - Create use cases for each service method
  - `ScenarioApiService` → Multiple use cases (Create, Get, Update, Delete, List)
  - `GameSessionApiService` → Session use cases
  - `MediaMetadataService` → Media use cases
  - Location: `Api/Services/` → `Application/UseCases/`
  - Estimated: 47 service files to refactor

- [ ] **Move repositories to Infrastructure.Data**
  - Move `Api/Repositories/*Repository.cs` → `Infrastructure.Data/Repositories/`
  - Move `Api/Repositories/I*Repository.cs` → `Application/Ports/Data/`
  - Update DI registrations in Program.cs
  - Location: `Api/Repositories/`

- [ ] **Remove infrastructure project references**
  - Remove Infrastructure.Azure reference
  - Remove Infrastructure.Data reference
  - Remove Infrastructure.Discord reference
  - Keep only: Domain, Application, Contracts, Shared
  - Location: `Mystira.App.Api.csproj`

### 🟡 High Priority

- [ ] **Refactor controllers to be thin**
  - Controllers should ONLY:
    - Accept HTTP request
    - Validate input
    - Call use case
    - Return HTTP response
  - Remove ALL business logic from controllers
  - Example pattern:
    ```csharp
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
    {
        var scenario = await _createScenarioUseCase.ExecuteAsync(request);
        return CreatedAtAction(nameof(Get), new { id = scenario.Id }, scenario);
    }
    ```

- [ ] **Clean up Models folder**
  - Evaluate if needed (Contracts already has DTOs)
  - Move unique models to Contracts or remove
  - Reduce duplication

- [ ] **Investigate Adapters folder**
  - Determine purpose of adapter
  - Move to appropriate infrastructure project or remove

### 🟢 Medium Priority

- [ ] **Implement API versioning**
  - Add Microsoft.AspNetCore.Mvc.Versioning
  - Version controllers (v1, v2)
  - Support evolving API

- [ ] **Add API rate limiting**
  - Protect against abuse
  - Per-user rate limits

- [ ] **Enhance error handling**
  - Global exception handler middleware
  - Consistent error response format
  - Problem Details (RFC 7807)

## 💡 Recommendations

### Immediate Actions
1. **Stop adding business logic to API** - All new logic goes in Application/UseCases
2. **Plan refactoring sprint** - Move services to use cases systematically
3. **Document new pattern** - Show team correct controller → use case pattern

### Architecture Target State

**API Layer Responsibilities** (ONLY):
- ✅ HTTP request/response handling
- ✅ Model binding and validation
- ✅ Authentication/authorization checks
- ✅ HTTP status code mapping
- ✅ Content negotiation
- ❌ NO business logic
- ❌ NO data access
- ❌ NO external service calls

**Correct Pattern**:
```
HTTP Request
  ↓
Controller (thin - validates, calls use case)
  ↓
Use Case (application layer - business workflow)
  ↓
Domain & Ports (business logic & abstractions)
  ↓
Infrastructure (implementations)
```

### Phased Migration Plan

**Phase 1** (Week 1): Infrastructure
- Move repositories out of API
- Remove infrastructure references
- Fix DI registrations

**Phase 2** (Weeks 2-4): Business Logic
- Refactor 1 controller fully (show pattern)
- Team review and approve approach
- Refactor remaining controllers systematically

**Phase 3** (Week 5): Cleanup
- Remove empty Services folder
- Clean up Models duplication
- Update documentation

## 📊 SWOT Analysis

### Strengths 💪
- ✅ **RESTful Design**: Well-structured API endpoints
- ✅ **OpenAPI/Swagger**: Excellent API documentation
- ✅ **Health Checks**: Proper monitoring endpoints
- ✅ **Authentication**: JWT security implemented
- ✅ **CORS**: Configured for known origins
- ✅ **Application Reference**: Calls use cases (when they exist)

### Weaknesses ⚠️
- ❌ **47 Service Files**: Business logic in presentation layer
- ❌ **6 Repository Files**: Data access in API layer
- ❌ **7 Project References**: Too many dependencies
- ⚠️ **Fat Controllers**: Business logic mixed with HTTP concerns
- ⚠️ **Duplication**: Models folder redundant with Contracts
- ⚠️ **Infrastructure Coupling**: Direct references to implementations

### Opportunities 🚀
- 📈 **Clean Architecture**: Move to pure hexagonal pattern
- 📈 **Reusable Logic**: Business logic usable by gRPC, CLI, background jobs
- 📈 **Better Testing**: Unit test business logic without HTTP mocking
- 📈 **API Versioning**: Support multiple API versions cleanly
- 📈 **gRPC Support**: Add high-performance gRPC alongside REST
- 📈 **GraphQL**: Add GraphQL layer using same use cases
- 📈 **Microservices**: Extract bounded contexts into separate services

### Threats 🔒
- ⚡ **Technical Debt**: 47 services to refactor
- ⚡ **Team Velocity**: Large refactoring impacts feature delivery
- ⚡ **Regression Risk**: Moving logic could introduce bugs
- ⚡ **Knowledge Gap**: Team may not understand hexagonal architecture
- ⚡ **Testing Gaps**: Need tests before refactoring

### Risk Mitigation
1. **Automated Tests**: Write tests BEFORE moving any code
2. **Incremental Migration**: One controller/feature at a time
3. **Pair Programming**: Spread architectural knowledge
4. **Code Reviews**: Enforce new patterns
5. **Documentation**: Update team wiki with patterns

## Related Documentation

- **[Application Layer](../Mystira.App.Application/README.md)** - Where business logic belongs
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Where repositories belong
- **[Contracts](../Mystira.App.Contracts/README.md)** - Request/Response DTOs

## License

Copyright (c) 2025 Mystira. All rights reserved.
