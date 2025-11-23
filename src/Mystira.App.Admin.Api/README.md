# Mystira.App Admin API

Administrative API for content management and system administration.

## Overview

The Mystira.App Admin API provides backend services for administrative and content management operations, separate from the client-facing API. This separation ensures better security, scalability, and maintainability.

**Key Features:**
- **Content Management**: Create, update, and delete scenarios, media, and characters
- **Admin Dashboard**: Web-based UI for system administration
- **Media Upload**: Bulk upload and management of multimedia assets
- **System Configuration**: Badge configurations, character maps, and app status
- **Data Import/Export**: YAML-based import/export for content management

## Architecture

- **.NET 9.0 Web API** with Azure App Service deployment
- **Shared Azure Cosmos DB** with client API for data persistence
- **Azure Blob Storage** for multimedia assets
- **JWT Authentication** for admin access
- **RESTful API Design** with OpenAPI/Swagger documentation
- **MVC Views** for admin dashboard UI
- **Port 7001** for local development (client API uses 7000)

## Separation from Client API

This Admin API is separate from the client-facing API (`Mystira.App.Api`) to:
- **Enhance Security**: Admin operations isolated from public endpoints
- **Independent Scaling**: Admin and client can scale separately
- **Better Maintainability**: Clear separation of concerns
- **Flexible Deployment**: Can deploy to different infrastructure

See [Admin API Separation](../../docs/features/ADMIN_API_SEPARATION.md) for architectural details.

## Prerequisites

- .NET 9.0 SDK
- Azure subscription (for cloud deployment)
- Visual Studio 2022 or VS Code

## Getting Started

### Local Development

1. **Navigate to Admin API directory**
   ```bash
   cd src/Mystira.App.Admin.Api
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
   
   **Note**: Use the same connection strings as the client API since they share the database.

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access Admin Dashboard**
   Navigate to `https://localhost:7001/admin` or `http://localhost:7001/admin`

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

### Admin Dashboard (UI)
- `GET /admin` - Main dashboard
- `GET /admin/login` - Login page
- `GET /admin/scenarios` - Scenario management page
- `GET /admin/media` - Media management page
- `GET /admin/media-metadata` - Media metadata management
- `GET /admin/charactermaps` - Character maps management
- `GET /admin/status` - App status management
- `POST /admin/status` - Update app status

### Scenario Management (API)
- `POST /api/admin/scenariosadmin` - Create scenario
- `PUT /api/admin/scenariosadmin/{id}` - Update scenario
- `DELETE /api/admin/scenariosadmin/{id}` - Delete scenario
- `POST /api/admin/scenariosadmin/validate` - Validate scenario
- `GET /api/admin/scenariosadmin/{id}` - Get scenario details

### Media Management (API)
- `POST /api/admin/mediaadmin/upload` - Upload single media file
- `POST /api/admin/mediaadmin/bulk-upload` - Bulk upload media
- `PUT /api/admin/mediaadmin/{mediaId}` - Update media asset
- `DELETE /api/admin/mediaadmin/{mediaId}` - Delete media asset
- `POST /api/admin/mediaadmin/validate` - Validate media references
- `GET /api/admin/mediaadmin/statistics` - Get usage statistics

### Character Management (API)
- `POST /api/admin/characteradmin` - Add character
- `PUT /api/admin/characteradmin/{id}` - Update character
- `DELETE /api/admin/characteradmin/{id}` - Delete character

### Character Map Management (API)
- `POST /api/admin/charactermapsadmin` - Create character map
- `PUT /api/admin/charactermapsadmin/{id}` - Update character map
- `DELETE /api/admin/charactermapsadmin/{id}` - Delete character map
- `POST /api/admin/charactermapsadmin/import` - Import from YAML
- `GET /api/admin/charactermapsadmin/export` - Export to YAML

### Badge Configuration (API)
- `POST /api/badgeconfigurationsadmin` - Create badge configuration
- `PUT /api/badgeconfigurationsadmin/{id}` - Update badge configuration
- `DELETE /api/badgeconfigurationsadmin/{id}` - Delete badge configuration
- `POST /api/badgeconfigurationsadmin/import` - Import from YAML
- `GET /api/badgeconfigurationsadmin/export` - Export to YAML

## Authentication

The Admin API uses JWT Bearer token authentication for admin access. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

Admin authentication is required for all content management operations.

## Running with Client API

For full functionality, run both APIs simultaneously:

```bash
# Terminal 1 - Client API (port 7000)
cd src/Mystira.App.Api
dotnet run

# Terminal 2 - Admin API (port 7001)
cd src/Mystira.App.Admin.Api
dotnet run
```

Both APIs connect to the same database, so changes in the Admin API are immediately available in the Client API.

## Data Models

### Admin-Managed Entities

- **Scenario** - Interactive story definitions with scenes and choices
- **MediaAsset** - Multimedia files stored in Azure Blob Storage
- **Character** - Character definitions for scenarios
- **CharacterMap** - Mappings of characters to media assets
- **BadgeConfiguration** - Achievement badge definitions

All models are shared with the Client API through the Domain project.

## Azure Deployment

### Infrastructure

The Admin API should be deployed separately from the Client API:

- **Client API**: `mystiraapp.azurewebsites.net`
- **Admin API**: `admin-mystiraapp.azurewebsites.net` or subdomain

Both connect to the same Cosmos DB database and Azure Blob Storage.

### CI/CD Pipeline

Configure GitHub Actions workflow to deploy Admin API to its own App Service:

```yaml
- name: Deploy Admin API
  uses: azure/webapps-deploy@v2
  with:
    app-name: 'mystira-admin-api'
    package: './admin-api-package'
```

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment name (Development, Staging, Production)
- `ConnectionStrings__CosmosDb` - Cosmos DB connection string (shared with Client API)
- `ConnectionStrings__AzureStorage` - Azure Storage connection string (shared with Client API)
- `Jwt__Key` - JWT signing key (must match Client API)
- `Jwt__Issuer` - JWT issuer claim
- `Jwt__Audience` - JWT audience claim

**Important**: Use the same database and storage credentials as the Client API.

## CORS Configuration

The Admin API has a separate CORS policy (`MystiraAdminPolicy`) that allows:
- `http://localhost:7001`
- `https://localhost:7001`
- `https://admin.mystiraapp.azurewebsites.net`
- `https://admin.mystira.app`

## Security

- **HTTPS Only** - All endpoints require HTTPS in production
- **JWT Authentication** - Admin-only access with token validation
- **CORS Configuration** - Restricted to admin domains only
- **Input Validation** - Comprehensive validation on all endpoints
- **Isolated from Client API** - Enhanced security through separation
- **Data Encryption** - All data encrypted in transit and at rest

## Development Guidelines

### Code Structure
- **Controllers** - Admin API endpoints and dashboard views
- **Services** - Business logic for content management
- **Models** - Request/response DTOs and view models
- **Views** - Razor views for admin dashboard UI

### Admin Dashboard Features
- Scenario creation and editing
- Media upload and management
- Character map configuration
- Badge configuration
- System status monitoring

## 🔍 Architectural Analysis

### Current State Assessment

**File Count**: ~68 C# files
**Project References**: 7 (⚠️ too many, same issue as main API)
- Domain ✅
- Contracts ✅
- Application ✅
- Shared ✅
- Infrastructure.Azure ❌
- Infrastructure.Data ❌
- Infrastructure.StoryProtocol ❌

**Folders**:
- Controllers/ - 16 controllers (some thin, some fat)
- Services/ - **41 service files** ❌ (business logic in API!)
- Repositories/ - **6 repository files** ❌ (data access in API!)
- Models/ - 5 model files
- Adapters/ - 1 adapter
- Validation/ - 1 validation schema
- Views/ - MVC Razor views (acceptable for admin dashboard)

### ⚠️ Architectural Issues Found

**NOTE**: Admin.Api has **IDENTICAL architectural issues** to the main API (Mystira.App.Api). Both projects suffer from the same violations of hexagonal architecture principles.

#### 1. **Business Logic in API Layer** (CRITICAL)
**Location**: `Services/` folder with **41 service files**

**Issue**: Admin API contains business logic services that should be Use Cases in Application layer:
- `ScenarioApiService.cs`
- `GameSessionApiService.cs`
- `MediaApiService.cs`
- `BadgeConfigurationApiService.cs`
- `CharacterMapApiService.cs`
- `UserProfileApiService.cs`
- `ContentBundleAdminService.cs`
- And 34 more...

**Impact**:
- ❌ Business logic scattered across API and Application layers
- ❌ Violates Single Responsibility (API should only handle HTTP concerns)
- ❌ Can't reuse admin logic in other contexts (CLI tools, background jobs)
- ❌ Hard to test business logic independently of HTTP context
- ❌ Duplicates pattern from main API (technical debt accumulating)

**Recommendation**:
- **MOVE** all `*ApiService` classes to `Application/UseCases/Admin/`
- Create admin-specific use cases (e.g., `CreateScenarioAdminUseCase`, `BulkUploadMediaUseCase`)
- Admin API Controllers should be **thin** - only handle:
  - HTTP request/response
  - Model validation
  - Calling use cases
  - HTTP status codes
  - Admin authorization checks
- Rename services to use cases

**Example Refactoring**:
```diff
- Services/ScenarioApiService.cs
+ Application/UseCases/Admin/CreateScenarioUseCase.cs
+ Application/UseCases/Admin/UpdateScenarioUseCase.cs
+ Application/UseCases/Admin/ValidateScenarioUseCase.cs
```

#### 2. **Repositories in API Layer** (CRITICAL)
**Location**: `Repositories/` folder

**Issue**: Admin API contains repository implementations:
```
Repositories/
├── CharacterMapFileRepository.cs
├── ICharacterMapFileRepository.cs
├── CharacterMediaMetadataFileRepository.cs
├── ICharacterMediaMetadataFileRepository.cs
├── MediaMetadataFileRepository.cs
└── IMediaMetadataFileRepository.cs
```

**Impact**:
- ❌ Data access logic in presentation layer
- ❌ Violates layered architecture
- ❌ Tight coupling to persistence
- ❌ Duplicates issue from main API

**Recommendation**:
- **MOVE** repositories to `Infrastructure.Data/Repositories/`
- **MOVE** interfaces to `Application/Ports/Data/`
- Admin API should NEVER contain data access code

#### 3. **Too Many Infrastructure References** (HIGH)
**Location**: `Mystira.App.Admin.Api.csproj` lines 27-29

**Issue**: Admin API directly references 3 infrastructure projects:
```xml
<ProjectReference Include="..\Mystira.App.Infrastructure.Azure\..." />
<ProjectReference Include="..\Mystira.App.Infrastructure.Data\..." />
<ProjectReference Include="..\Mystira.App.Infrastructure.StoryProtocol\..." />
```

**26 infrastructure namespace imports** across 19 files.

**Impact**:
- ⚠️ Admin API knows about infrastructure implementation details
- ⚠️ Can't swap implementations without changing Admin API
- ⚠️ Violates Dependency Inversion Principle
- ⚠️ Same issue as main API (systemic problem)

**Recommendation**:
- Admin API should ONLY reference:
  - Domain (for entities in responses)
  - Application (for use cases)
  - Contracts (for DTOs)
  - Shared (for middleware)
- Infrastructure wired via DI in `Program.cs`
- No direct infrastructure references

#### 4. **Adapters Folder** (MEDIUM)
**Location**: `Adapters/HealthCheckServiceAdapter.cs`

**Issue**: Contains adapter in presentation layer

**Impact**:
- ⚠️ Adapter pattern in presentation layer is odd
- ⚠️ Suggests missing abstraction

**Recommendation**:
- Move to appropriate Infrastructure project
- Or wrap via use case if needed

### ✅ What's Working Well

1. **Separation from Client API** - Good security and scaling strategy
2. **MVC Admin Dashboard** - Appropriate use of Razor views for admin UI
3. **Controllers Relatively Thin** - Validation and delegation pattern visible
4. **Contracts Usage** - Uses DTOs for request/response
5. **Application Reference** - Correctly references Application layer (when used)
6. **Health Checks** - Monitoring infrastructure
7. **YAML Import/Export** - Good content management features

### 🔄 Comparison with Main API

| Aspect | Main API | Admin API | Status |
|--------|----------|-----------|--------|
| **Service Files** | 47 | 41 | Both ❌ |
| **Repository Files** | 6 | 6 | Both ❌ |
| **Infrastructure Refs** | 3 | 3 | Both ❌ |
| **Total Project Refs** | 7 | 7 | Both ❌ |
| **MVC Views** | No | Yes | Admin ✅ |

**Key Insight**: Both API projects have identical architectural violations. This is a **systemic issue** requiring coordinated refactoring across both projects.

## 📋 Refactoring TODO

### 🔴 Critical Priority (MUST FIX)

- [ ] **Move all business logic to Application layer**
  - Create `Application/UseCases/Admin/` folder
  - Move all 41 service files to use cases
  - Examples:
    - `ScenarioApiService` → `CreateScenarioAdminUseCase`, `UpdateScenarioAdminUseCase`, etc.
    - `MediaApiService` → `UploadMediaUseCase`, `BulkUploadMediaUseCase`, etc.
    - `BadgeConfigurationApiService` → Badge-related use cases
  - Location: `Admin.Api/Services/` → `Application/UseCases/Admin/`
  - Estimated: **41 service files** to refactor

- [ ] **Move repositories to Infrastructure.Data**
  - Move `Admin.Api/Repositories/*Repository.cs` → `Infrastructure.Data/Repositories/`
  - Move `Admin.Api/Repositories/I*Repository.cs` → `Application/Ports/Data/`
  - Update DI registrations in Program.cs
  - Location: `Admin.Api/Repositories/`
  - Estimated: **6 repository files** to move

- [ ] **Remove infrastructure project references**
  - Remove Infrastructure.Azure reference
  - Remove Infrastructure.Data reference
  - Remove Infrastructure.StoryProtocol reference
  - Keep only: Domain, Application, Contracts, Shared
  - Location: `Mystira.App.Admin.Api.csproj`

### 🟡 High Priority

- [ ] **Refactor controllers to be thin**
  - Controllers should ONLY:
    - Accept HTTP request
    - Validate input (ModelState)
    - Check admin authorization
    - Call use case
    - Return HTTP response
  - Remove ALL business logic from controllers
  - Pattern:
    ```csharp
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
    {
        var scenario = await _createScenarioAdminUseCase.ExecuteAsync(request);
        return CreatedAtAction(nameof(Get), new { id = scenario.Id }, scenario);
    }
    ```

- [ ] **Clean up Models folder**
  - Evaluate if needed (Contracts already has DTOs)
  - Move unique admin view models to appropriate location
  - Reduce duplication

- [ ] **Investigate Adapters folder**
  - Move `HealthCheckServiceAdapter` to Infrastructure
  - Or remove if unnecessary

### 🟢 Medium Priority

- [ ] **Coordinate with main API refactoring**
  - Use same patterns for both Admin API and Client API
  - Share common use cases where appropriate
  - Document differences (admin vs client use cases)

- [ ] **Add API versioning**
  - Version admin endpoints (v1, v2)
  - Support evolving admin API

- [ ] **Enhance MVC dashboard**
  - Ensure views use use cases (not services)
  - Add HTMX for better interactivity
  - Improve UX

### 🔵 Low Priority

- [ ] **Add admin-specific middleware**
  - Admin audit logging middleware
  - Admin activity tracking
  - Admin rate limiting

## 💡 Recommendations

### Immediate Actions
1. **Stop adding business logic to Admin API** - All new logic goes in Application/UseCases/Admin
2. **Coordinate with main API refactoring** - Fix both projects together
3. **Document admin use case patterns** - Show team correct approach

### Architecture Target State

**Admin API Layer Responsibilities** (ONLY):
- ✅ HTTP request/response handling
- ✅ MVC views for admin dashboard
- ✅ Model binding and validation
- ✅ Admin authentication/authorization checks
- ✅ HTTP status code mapping
- ❌ NO business logic
- ❌ NO data access
- ❌ NO external service calls

**Correct Pattern for Admin**:
```
Admin HTTP Request
  ↓
Admin Controller (thin - validates, checks admin auth, calls use case)
  ↓
Admin Use Case (application layer - admin workflow)
  ↓
Domain & Ports (business logic & abstractions)
  ↓
Infrastructure (implementations)
```

### Phased Migration Plan

**Note**: Should be coordinated with main API refactoring for consistency.

**Phase 1** (Week 1): Infrastructure
- Move repositories out of Admin API
- Remove infrastructure references
- Fix DI registrations

**Phase 2** (Weeks 2-4): Business Logic
- Refactor 1 admin controller fully (establish pattern)
- Team review and approve approach
- Refactor remaining admin controllers systematically

**Phase 3** (Week 5): Cleanup & Dashboard
- Remove empty Services folder
- Clean up Models duplication
- Enhance MVC dashboard to use new use cases
- Update documentation

## 📊 SWOT Analysis

### Strengths 💪
- ✅ **Separation from Client API** - Security and scaling benefits
- ✅ **MVC Admin Dashboard** - Good admin UX
- ✅ **RESTful Design** - Well-structured endpoints
- ✅ **YAML Import/Export** - Content management features
- ✅ **Swagger/OpenAPI** - API documentation
- ✅ **Health Checks** - Monitoring
- ✅ **Shared Database** - Consistency with client API

### Weaknesses ⚠️
- ❌ **41 Service Files** - Business logic in presentation layer
- ❌ **6 Repository Files** - Data access in API layer
- ❌ **7 Project References** - Too many dependencies
- ⚠️ **Mirrors Main API Issues** - Same violations in both projects
- ⚠️ **Infrastructure Coupling** - Direct references to implementations
- ⚠️ **Model Duplication** - Some duplication with Contracts

### Opportunities 🚀
- 📈 **Clean Architecture** - Move to pure hexagonal pattern (along with main API)
- 📈 **Reusable Admin Logic** - Admin use cases for CLI tools, background jobs
- 📈 **Better Testing** - Unit test admin logic without HTTP mocking
- 📈 **Enhanced Dashboard** - Modern SPA dashboard using Blazor or React
- 📈 **gRPC Admin API** - High-performance gRPC for admin tools
- 📈 **Audit Logging** - Comprehensive admin action tracking
- 📈 **Webhook Management** - Admin UI for webhook configuration

### Threats 🔒
- ⚡ **Technical Debt** - 41 services + 6 repositories to refactor
- ⚡ **Coordination Needed** - Must align with main API refactoring
- ⚡ **Team Velocity** - Large refactoring impacts feature delivery
- ⚡ **Regression Risk** - Moving logic could introduce bugs
- ⚡ **Two API Projects** - Doubled refactoring effort

### Risk Mitigation
1. **Automated Tests** - Write tests BEFORE moving any code
2. **Incremental Migration** - One controller/feature at a time
3. **Coordinate with Main API** - Use consistent patterns
4. **Pair Programming** - Spread architectural knowledge
5. **Code Reviews** - Enforce new patterns
6. **Shared Documentation** - Update team wiki with patterns for BOTH APIs

## Related Documentation

- **[Admin API Separation](../../docs/features/ADMIN_API_SEPARATION.md)** - Architecture details
- **[Client API README](../Mystira.App.Api/README.md)** - Client API (has same issues!)
- **[Application Layer](../Mystira.App.Application/README.md)** - Where admin business logic belongs
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Where repositories belong
- **[Main README](../../README.md)** - Project overview

## License

Copyright (c) 2025 Mystira. All rights reserved.

