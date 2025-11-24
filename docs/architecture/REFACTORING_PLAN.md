# Hexagonal Architecture Refactoring Plan

**Status**: 🟡 Planning Phase
**Estimated Duration**: 16-25 weeks
**Priority**: High
**Last Updated**: 2025-11-23

## 📋 Executive Summary

This document outlines a comprehensive plan to refactor the Mystira.App codebase to achieve proper hexagonal (ports & adapters) architecture. The current codebase has significant architectural violations that impact maintainability, testability, and adherence to SOLID principles.

### Key Issues Identified

| Issue | Severity | Files Affected | Effort |
|-------|----------|----------------|--------|
| Business logic in API layers | 🔴 CRITICAL | 88 service files | 8-10 weeks |
| Repositories in presentation layers | 🔴 CRITICAL | 12 repository files | 1 week |
| Infrastructure dependencies in Application | 🔴 CRITICAL | 138 dependencies | 2-3 weeks |
| Port interfaces in Infrastructure | 🟡 HIGH | ~27 interface files | 2-3 weeks |
| Model duplication (PWA) | 🟡 MEDIUM | 10+ model files | 1-2 weeks |

**Total Estimated Effort**: 16-25 weeks (phased approach)

### Business Impact

**Benefits of Refactoring:**
- ✅ **Testability**: Unit test business logic without HTTP/database mocking
- ✅ **Maintainability**: Clear separation of concerns, easier to understand
- ✅ **Flexibility**: Swap implementations (database, cloud provider) without changing core logic
- ✅ **Reusability**: Use cases can be called from APIs, CLI tools, background jobs
- ✅ **Team Velocity**: Faster feature development with proper architecture

**Risks of NOT Refactoring:**
- ❌ Accumulating technical debt (already at critical levels)
- ❌ Difficulty onboarding new developers
- ❌ Increased bug introduction rate
- ❌ Slow feature development
- ❌ Hard to test business logic

## 🎯 Current State Assessment

### Architecture Violations Summary

**✅ Exemplar Projects** (Follow these patterns):
- `Infrastructure.StoryProtocol` - Perfect hexagonal architecture ⭐
- `Contracts` - Excellent CQRS readiness
- `Shared` - Small, focused, no scope creep

**❌ Critical Refactoring Needed**:
- `Application` - 138 infrastructure dependencies
- `API` - 47 misplaced services + 6 repositories
- `Admin.Api` - 41 misplaced services + 6 repositories

**⚠️ Moderate Refactoring Needed**:
- `Infrastructure.Azure` - Port interfaces in wrong layer
- `Infrastructure.Discord` - Port interfaces in wrong layer
- `Infrastructure.Data` - Repository interfaces in wrong layer
- `PWA` - Model duplication, missing Contracts reference

### Detailed Issue Breakdown

#### 1. Business Logic in Presentation Layers (CRITICAL)

**Problem**: 88 service files containing business logic in API/Admin.Api projects.

**Files**:
- `API/Services/*ApiService.cs` (47 files)
- `Admin.Api/Services/*ApiService.cs` (41 files)

**Impact**:
- Cannot test business logic without HTTP context
- Logic cannot be reused in CLI tools or background jobs
- Violates Single Responsibility Principle
- Makes controllers fat instead of thin

**Example Violation**:
```csharp
// WRONG: Business logic in API layer
// API/Services/ScenarioApiService.cs
public class ScenarioApiService
{
    public async Task<Scenario> CreateScenarioAsync(CreateScenarioRequest request)
    {
        // Business logic here (validation, domain operations, etc.)
        // This should be in Application/UseCases!
    }
}
```

**Target State**:
```csharp
// CORRECT: Use case in Application layer
// Application/UseCases/Scenarios/CreateScenarioUseCase.cs
public class CreateScenarioUseCase
{
    public async Task<Scenario> ExecuteAsync(CreateScenarioRequest request)
    {
        // Business logic here
    }
}

// API Controller just orchestrates
// API/Controllers/ScenariosController.cs
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
{
    var scenario = await _createScenarioUseCase.ExecuteAsync(request);
    return CreatedAtAction(nameof(Get), new { id = scenario.Id }, scenario);
}
```

#### 2. Repositories in Presentation Layers (CRITICAL)

**Problem**: 12 repository files in API/Admin.Api projects.

**Files**:
- `API/Repositories/*Repository.cs` (6 files)
- `Admin.Api/Repositories/*Repository.cs` (6 files)

**Impact**:
- Data access logic in presentation layer
- Violates layered architecture
- Tight coupling to persistence mechanism
- Cannot swap database implementations

**Target State**:
- Move implementations to `Infrastructure.Data/Repositories/`
- Move interfaces to `Application/Ports/Data/`

#### 3. Infrastructure Dependencies in Application Layer (CRITICAL)

**Problem**: 138 direct infrastructure dependencies in Application layer.

**Violations**:
- Direct references to `Infrastructure.Data` and `Infrastructure.Azure` projects
- `using Mystira.App.Infrastructure.*` in 97 use case files
- Application layer knows about concrete implementations

**Impact**:
- Violates Dependency Inversion Principle
- Cannot swap infrastructure implementations
- Application layer coupled to specific technologies (Azure, Cosmos DB)

**Target State**:
- Application depends ONLY on abstractions (ports)
- Infrastructure implements ports defined in Application
- Dependency flow: Infrastructure → Application → Domain

#### 4. Port Interfaces in Infrastructure Layers (HIGH)

**Problem**: ~27 port interfaces defined in Infrastructure projects.

**Files**:
- `Infrastructure.Data/Repositories/I*Repository.cs` (~15 files)
- `Infrastructure.Azure/Services/I*Service.cs` (2 files)
- `Infrastructure.Discord/Services/IDiscordBotService.cs` (1 file)
- Similar in API/Admin.Api repositories (~9 files)

**Impact**:
- Application must reference Infrastructure to use interfaces
- Breaks Dependency Inversion Principle
- Cannot easily swap implementations

**Target State**:
- All port interfaces in `Application/Ports/`
- Infrastructure projects reference Application
- Clear separation: Port (Application) vs Adapter (Infrastructure)

#### 5. Model Duplication (MEDIUM)

**Problem**: PWA defines its own models instead of using Contracts.

**Files**: `PWA/Models/*.cs` (10+ duplicate models)

**Impact**:
- Models can drift out of sync
- API contract changes require manual PWA updates
- Violates DRY principle

**Target State**:
- PWA references Contracts project
- Uses DTOs from Contracts for all API communication
- Delete duplicate models

## 🎯 Target Architecture

### Dependency Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       │
│  │   API    │  │Admin.Api │  │   PWA    │  │  CLI     │       │
│  │ (thin)   │  │ (thin)   │  │ (UI)     │  │ (tools)  │       │
│  └─────┬────┘  └─────┬────┘  └─────┬────┘  └─────┬────┘       │
└────────┼─────────────┼─────────────┼─────────────┼─────────────┘
         │             │             │             │
         └─────────────┴─────────────┴─────────────┘
                       ▼
         ┌─────────────────────────────────────────┐
         │      APPLICATION LAYER (Use Cases)       │
         │  ┌────────────────────────────────┐     │
         │  │ UseCases/Scenarios/            │     │
         │  │ UseCases/GameSessions/         │     │
         │  │ UseCases/Admin/                │     │
         │  └────────────────────────────────┘     │
         │  ┌────────────────────────────────┐     │
         │  │ Ports/ (Interfaces)            │     │
         │  │   Data/IScenarioRepository     │     │
         │  │   Storage/IBlobService         │     │
         │  │   Messaging/IMessagingService  │     │
         │  └────────────────────────────────┘     │
         └───────────────┬─────────────────────────┘
                         ▼
         ┌─────────────────────────────────────────┐
         │        DOMAIN LAYER (Core Logic)         │
         │  Entities, Value Objects, Domain Events  │
         └───────────────┬─────────────────────────┘
                         ▲
         ┌───────────────┴─────────────────────────┐
         │      INFRASTRUCTURE LAYER (Adapters)     │
         │  ┌─────────────┐  ┌──────────────┐      │
         │  │ Data        │  │ Azure        │      │
         │  │ (Cosmos DB) │  │ (Blob,       │      │
         │  │             │  │  Key Vault)  │      │
         │  └─────────────┘  └──────────────┘      │
         │  ┌─────────────┐  ┌──────────────┐      │
         │  │ Discord     │  │ Story        │      │
         │  │ (Messaging) │  │ Protocol     │      │
         │  └─────────────┘  └──────────────┘      │
         └─────────────────────────────────────────┘
```

### Layer Responsibilities

#### Presentation Layer (API, Admin.Api, PWA, CLI)
**Allowed**:
- ✅ HTTP request/response handling
- ✅ Model binding and validation
- ✅ Authentication/authorization checks
- ✅ HTTP status code mapping
- ✅ Calling use cases from Application layer

**Forbidden**:
- ❌ Business logic
- ❌ Data access
- ❌ Direct infrastructure calls
- ❌ Domain logic

#### Application Layer (Use Cases + Ports)
**Allowed**:
- ✅ Use case orchestration
- ✅ Business workflow coordination
- ✅ Port interface definitions (abstractions)
- ✅ Application-specific business rules
- ✅ DTO transformations

**Forbidden**:
- ❌ HTTP concerns
- ❌ Database concerns
- ❌ Cloud provider concerns
- ❌ Framework-specific code

#### Domain Layer (Core Business Logic)
**Allowed**:
- ✅ Rich domain models
- ✅ Business rules
- ✅ Domain events
- ✅ Value objects
- ✅ Aggregates

**Forbidden**:
- ❌ Infrastructure code
- ❌ Application concerns
- ❌ Persistence logic
- ❌ External service calls

#### Infrastructure Layer (Adapters)
**Allowed**:
- ✅ Implementing Application ports
- ✅ Database access
- ✅ External service integration
- ✅ Technology-specific code

**Forbidden**:
- ❌ Business logic
- ❌ Defining abstractions (use Application ports)

## 📅 Phased Refactoring Plan

### Phase 0: Preparation (Week 0)

**Goal**: Set up for success before moving code.

**Tasks**:
- [x] Complete architectural analysis (DONE)
- [ ] Create refactoring branch from dev
- [ ] Set up automated tests (or document test strategy)
- [ ] Document current behavior (integration tests)
- [ ] Team kickoff meeting - review plan
- [ ] Assign DRI (Directly Responsible Individual) for each phase

**Success Criteria**:
- Team understands the plan
- Test baseline established
- Branch created and protected

**Estimated Effort**: 1 week

---

### Phase 1: Repository Interface Migration (Weeks 1-2)

**Goal**: Move all repository interfaces to Application/Ports/Data/

**Why Start Here**:
- Smallest change with biggest architectural impact
- Unblocks Application layer DIP fixes
- Relatively low risk
- Sets pattern for other interface migrations

**Tasks**:

#### Week 1: Infrastructure.Data Repositories

1. **Create Application/Ports/Data folder structure**
   ```bash
   mkdir -p src/Mystira.App.Application/Ports/Data
   ```

2. **Move repository interfaces from Infrastructure.Data** (~15 files)
   - Source: `Infrastructure.Data/Repositories/I*.cs`
   - Destination: `Application/Ports/Data/I*.cs`
   - Files to move:
     - `IScenarioRepository.cs`
     - `IGameSessionRepository.cs`
     - `IUserProfileRepository.cs`
     - `ICharacterRepository.cs`
     - `IMediaAssetRepository.cs`
     - `IBadgeConfigurationRepository.cs`
     - ... (~10 more)

3. **Add Application reference to Infrastructure.Data**
   ```xml
   <ProjectReference Include="..\Mystira.App.Application\Mystira.App.Application.csproj" />
   ```

4. **Update Infrastructure.Data implementations**
   - Update `using` statements to reference `Application.Ports.Data`
   - Update interface implementations to use new location
   - Fix DI registrations in `ServiceCollectionExtensions.cs`

5. **Update Application layer use cases**
   - Change `using Mystira.App.Infrastructure.Data.Repositories`
   - To `using Mystira.App.Application.Ports.Data`

6. **Test and verify**
   - Run all tests
   - Verify application builds
   - Check DI container resolution

#### Week 2: API and Admin.Api Repositories

1. **Move repository interfaces from API** (6 files)
   - Source: `API/Repositories/I*.cs`
   - Destination: `Application/Ports/Data/I*.cs`
   - Files: `IScenarioRepository.cs`, etc.

2. **Move repository implementations from API** (6 files)
   - Source: `API/Repositories/*Repository.cs`
   - Destination: `Infrastructure.Data/Repositories/*Repository.cs`

3. **Move repository interfaces from Admin.Api** (6 files)
   - Source: `Admin.Api/Repositories/I*.cs`
   - Destination: `Application/Ports/Data/I*.cs`

4. **Move repository implementations from Admin.Api** (6 files)
   - Source: `Admin.Api/Repositories/*Repository.cs`
   - Destination: `Infrastructure.Data/Repositories/*Repository.cs`

5. **Update DI registrations**
   - Update `API/Program.cs`
   - Update `Admin.Api/Program.cs`
   - Register implementations from Infrastructure.Data

6. **Delete empty Repositories folders**
   - Delete `API/Repositories/`
   - Delete `Admin.Api/Repositories/`

7. **Test and verify**
   - All tests pass
   - No compilation errors
   - Repository resolution works via DI

**Deliverables**:
- ✅ ~27 interface files moved to `Application/Ports/Data/`
- ✅ 12 implementation files moved to `Infrastructure.Data/Repositories/`
- ✅ Infrastructure.Data references Application
- ✅ All tests pass

**Success Criteria**:
- Zero repository files in API/Admin.Api
- All repository interfaces in Application/Ports/Data
- Application builds successfully
- All tests pass

**Risk Mitigation**:
- Do one repository at a time, test after each
- Keep old files until verified working
- Rollback plan: revert git commits

**Estimated Effort**: 2 weeks

---

### Phase 2: Infrastructure Port Interfaces (Weeks 3-4)

**Goal**: Move remaining port interfaces from Infrastructure to Application/Ports/

**Tasks**:

#### Week 3: Azure and Discord Ports

1. **Create Application/Ports folder structure**
   ```bash
   mkdir -p src/Mystira.App.Application/Ports/Storage
   mkdir -p src/Mystira.App.Application/Ports/Media
   mkdir -p src/Mystira.App.Application/Ports/Messaging
   ```

2. **Move Azure port interfaces** (2 files)
   - Move `Infrastructure.Azure/Services/IAzureBlobService.cs`
   - To `Application/Ports/Storage/IBlobService.cs`
   - Rename to remove "Azure" prefix (implementation-agnostic)

   - Move `Infrastructure.Azure/Services/IAudioTranscodingService.cs`
   - To `Application/Ports/Media/IAudioTranscodingService.cs`

3. **Add Application reference to Infrastructure.Azure**
   ```xml
   <ProjectReference Include="..\Mystira.App.Application\Mystira.App.Application.csproj" />
   ```

4. **Update Azure implementations**
   - `AzureBlobService : IBlobService` (from Application.Ports.Storage)
   - `FfmpegAudioTranscodingService : IAudioTranscodingService` (from Application.Ports.Media)
   - Update DI registrations

5. **Move Discord port interface** (1 file)
   - Move `Infrastructure.Discord/Services/IDiscordBotService.cs`
   - To `Application/Ports/Messaging/IMessagingService.cs`
   - Rename to platform-agnostic name

6. **Add Application reference to Infrastructure.Discord**
   ```xml
   <ProjectReference Include="..\Mystira.App.Application\Mystira.App.Application.csproj" />
   ```

7. **Update Discord implementation**
   - `DiscordBotService : IMessagingService` (from Application.Ports.Messaging)
   - Update DI registrations

#### Week 4: Application Layer Updates

1. **Update Application use cases**
   - Change all `using` statements to reference new port locations
   - Verify dependency injection works

2. **Update API/Admin.Api Program.cs**
   - Update DI registrations for new interface locations
   - Verify infrastructure implementations resolve correctly

3. **Test and verify**
   - All tests pass
   - Application builds
   - DI container resolves all dependencies

**Deliverables**:
- ✅ 3 port interfaces moved to Application/Ports/
- ✅ Infrastructure projects reference Application
- ✅ Implementations updated to use Application ports

**Success Criteria**:
- All port interfaces in Application/Ports/
- Infrastructure.Azure references Application
- Infrastructure.Discord references Application
- All tests pass

**Estimated Effort**: 2 weeks

---

### Phase 3: Application Layer DIP Fix (Weeks 5-7)

**Goal**: Remove 138 infrastructure dependencies from Application layer.

**Tasks**:

#### Week 5: Remove Infrastructure Project References

1. **Remove Infrastructure.Data reference from Application**
   - Edit `Application/Mystira.App.Application.csproj`
   - Remove `<ProjectReference Include="..\Mystira.App.Infrastructure.Data\..." />`
   - Remove `<ProjectReference Include="..\Mystira.App.Infrastructure.Azure\..." />`

2. **Fix compilation errors**
   - Replace direct repository usage with port interfaces
   - Replace direct Azure service usage with port interfaces
   - Remove all `using Mystira.App.Infrastructure.*` statements

3. **Update use cases** (97 files affected)
   - Change constructor injection from concrete types to interfaces
   - Example:
     ```csharp
     // BEFORE
     public CreateScenarioUseCase(ScenarioRepository repository) // Concrete!

     // AFTER
     public CreateScenarioUseCase(IScenarioRepository repository) // Interface!
     ```

#### Week 6: Port Interface Completion

1. **Audit missing port interfaces**
   - Review Application use cases
   - Identify any missing port abstractions
   - Create missing interfaces in Application/Ports/

2. **Create new ports as needed**
   - Example: `IFileService` if file operations needed
   - Example: `IEmailService` if email needed
   - Example: `ICacheService` if caching needed

3. **Update Infrastructure implementations**
   - Implement any new ports
   - Register in DI containers

#### Week 7: Testing and Verification

1. **Run all tests**
   - Unit tests
   - Integration tests
   - Fix any failures

2. **Verify dependency flow**
   - Application references: Domain only (+ maybe Contracts)
   - Infrastructure references: Application + Domain
   - API references: Application, Domain, Contracts, Shared (NO Infrastructure)

3. **Update documentation**
   - Update READMEs with corrected dependency flow
   - Document new port interfaces

**Deliverables**:
- ✅ Application layer has ZERO infrastructure dependencies
- ✅ All use cases depend on port interfaces only
- ✅ 138 infrastructure dependencies removed

**Success Criteria**:
- Application.csproj has no Infrastructure references
- No `using Mystira.App.Infrastructure.*` in Application layer
- All tests pass
- Dependency flow correct: Infrastructure → Application → Domain

**Risk Mitigation**:
- Comprehensive testing before removing references
- Feature flags for rollback if needed
- Incremental approach - one namespace at a time

**Estimated Effort**: 3 weeks

---

### Phase 4: API Service Migration (Weeks 8-12)

**Goal**: Move 47 service files from API to Application/UseCases/

**Strategy**: Pilot one feature end-to-end, then parallelize.

**Tasks**:

#### Week 8: Pilot Feature (Scenarios)

1. **Create use case structure**
   ```bash
   mkdir -p src/Mystira.App.Application/UseCases/Scenarios
   ```

2. **Migrate ScenarioApiService** (pilot)
   - Source: `API/Services/ScenarioApiService.cs` (32KB - largest file)
   - Analyze methods and split into use cases:
     - `CreateScenarioUseCase.cs`
     - `UpdateScenarioUseCase.cs`
     - `DeleteScenarioUseCase.cs`
     - `GetScenarioUseCase.cs`
     - `ListScenariosUseCase.cs`
     - `ValidateScenarioUseCase.cs`

3. **Implement use cases**
   - Extract business logic from service
   - Use constructor injection for dependencies (ports only!)
   - Add proper error handling
   - Example:
     ```csharp
     public class CreateScenarioUseCase
     {
         private readonly IScenarioRepository _repository;
         private readonly ILogger<CreateScenarioUseCase> _logger;

         public CreateScenarioUseCase(
             IScenarioRepository repository,
             ILogger<CreateScenarioUseCase> logger)
         {
             _repository = repository;
             _logger = logger;
         }

         public async Task<Scenario> ExecuteAsync(CreateScenarioRequest request)
         {
             _logger.LogInformation("Creating scenario: {Title}", request.Title);

             // Business logic here
             var scenario = Scenario.Create(request.Title, request.Description);

             await _repository.AddAsync(scenario);

             return scenario;
         }
     }
     ```

4. **Update ScenariosController**
   - Change from injecting `IScenarioApiService`
   - To injecting individual use cases
   - Make controller thin:
     ```csharp
     [ApiController]
     [Route("api/[controller]")]
     public class ScenariosController : ControllerBase
     {
         private readonly CreateScenarioUseCase _createUseCase;
         private readonly GetScenarioUseCase _getUseCase;
         // ... other use cases

         [HttpPost]
         public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
         {
             if (!ModelState.IsValid)
                 return BadRequest(ModelState);

             var scenario = await _createUseCase.ExecuteAsync(request);
             return CreatedAtAction(nameof(Get), new { id = scenario.Id }, scenario);
         }
     }
     ```

5. **Update DI registration**
   - Register use cases in `API/Program.cs` or Application `ServiceCollectionExtensions`
   ```csharp
   services.AddScoped<CreateScenarioUseCase>();
   services.AddScoped<GetScenarioUseCase>();
   // etc.
   ```

6. **Test pilot**
   - Unit test use cases
   - Integration test API endpoints
   - Verify all scenario operations work

7. **Team review**
   - Review pilot implementation
   - Gather feedback
   - Adjust approach if needed

#### Week 9-12: Remaining API Services (46 files)

**Batch 1 (Week 9)**: Game Sessions (~10 services)
- `GameSessionApiService.cs` → Multiple use cases
- `PlayerActionApiService.cs` → Use cases
- Etc.

**Batch 2 (Week 10)**: Media & Characters (~12 services)
- `MediaApiService.cs` → Use cases
- `CharacterApiService.cs` → Use cases
- Etc.

**Batch 3 (Week 11)**: Users & Auth (~12 services)
- `UserProfileApiService.cs` → Use cases
- `AccountApiService.cs` → Use cases
- Etc.

**Batch 4 (Week 12)**: Remaining (~12 services)
- All other `*ApiService.cs` files
- Update all controllers
- Delete API/Services folder

**Per-Service Process**:
1. Create use case folder
2. Analyze service methods
3. Create use case classes (one per operation)
4. Extract business logic
5. Update controller
6. Register in DI
7. Test
8. Delete service file

**Parallelization**:
- After pilot approval, assign batches to different developers
- Each developer follows same pattern
- Daily standups to coordinate

**Deliverables**:
- ✅ 47 service files migrated to use cases
- ✅ All controllers thin (5-10 lines per action)
- ✅ API/Services folder deleted

**Success Criteria**:
- Zero service files in API/Services
- All business logic in Application/UseCases
- All API tests pass
- Controllers are thin (< 10 lines per action)

**Risk Mitigation**:
- One service at a time
- Keep old service until new use cases tested
- Feature flags for gradual rollout

**Estimated Effort**: 5 weeks

---

### Phase 5: Admin.Api Service Migration (Weeks 13-16)

**Goal**: Move 41 service files from Admin.Api to Application/UseCases/Admin/

**Strategy**: Follow same pattern as API migration, but coordinate patterns.

**Tasks**:

#### Week 13: Admin Pilot Feature

1. **Create admin use case structure**
   ```bash
   mkdir -p src/Mystira.App.Application/UseCases/Admin
   ```

2. **Migrate one admin service** (pilot)
   - Choose: `Admin.Api/Services/ScenarioApiService.cs`
   - Create use cases in `Application/UseCases/Admin/Scenarios/`
   - Update admin controller
   - Test

3. **Team review**
   - Ensure consistency with main API pattern
   - Identify admin-specific patterns

#### Week 14-16: Remaining Admin Services (40 files)

**Batch 1 (Week 14)**: Content Management (~15 services)
- Media upload services
- Scenario management
- Character management

**Batch 2 (Week 15)**: Configuration (~13 services)
- Badge configurations
- Character maps
- App status

**Batch 3 (Week 16)**: Users & System (~12 services)
- User management
- System health
- Analytics

**Deliverables**:
- ✅ 41 admin service files migrated to use cases
- ✅ Admin.Api/Services folder deleted
- ✅ Admin controllers thin

**Success Criteria**:
- Zero service files in Admin.Api/Services
- All admin business logic in Application/UseCases/Admin
- Admin controllers thin
- All admin tests pass

**Estimated Effort**: 4 weeks

---

### Phase 6: PWA Model Migration (Weeks 17-18)

**Goal**: Remove model duplication in PWA, use Contracts instead.

**Tasks**:

#### Week 17: Add Contracts Reference

1. **Add Contracts reference to PWA**
   ```xml
   <ProjectReference Include="..\Mystira.App.Contracts\Mystira.App.Contracts.csproj" />
   ```

2. **Analyze PWA models** (10+ files)
   - `PWA/Models/Scenario.cs`
   - `PWA/Models/UserProfile.cs`
   - `PWA/Models/Account.cs`
   - `PWA/Models/Character.cs`
   - Etc.

3. **Identify duplicates**
   - Compare PWA models with Contracts DTOs
   - List models that can be replaced
   - List models that are PWA-specific (view models)

4. **Create missing Contract DTOs**
   - If any PWA models don't have Contracts equivalents, create them
   - Add to Contracts/Responses/

#### Week 18: Replace PWA Models

1. **Update API clients**
   - Change API client return types from PWA models to Contracts DTOs
   - Example:
     ```csharp
     // BEFORE
     public Task<PWA.Models.Scenario> GetScenarioAsync(string id);

     // AFTER
     public Task<Contracts.Responses.ScenarioResponse> GetScenarioAsync(string id);
     ```

2. **Update Blazor components**
   - Change component parameters and fields
   - Use Contracts types instead of PWA models

3. **Delete duplicate PWA models**
   - Remove `PWA/Models/Scenario.cs`
   - Remove other duplicates
   - Keep only PWA-specific view models

4. **Minimize Domain reference**
   - Review PWA usage of Domain types
   - Replace with Contracts where possible
   - Goal: Remove Domain reference if feasible

5. **Test PWA**
   - Run PWA locally
   - Test all API interactions
   - Verify data displays correctly

**Deliverables**:
- ✅ PWA references Contracts
- ✅ Duplicate models deleted
- ✅ PWA uses Contracts DTOs for API communication

**Success Criteria**:
- PWA/Models folder has minimal files (only view models)
- PWA uses Contracts for all API communication
- All PWA features work

**Estimated Effort**: 2 weeks

---

### Phase 7: Infrastructure Project References Cleanup (Week 19)

**Goal**: Remove infrastructure references from API/Admin.Api.

**Tasks**:

1. **Remove Infrastructure.Data reference from API**
   - Edit `API/Mystira.App.Api.csproj`
   - Remove `<ProjectReference Include="..\Mystira.App.Infrastructure.Data\..." />`
   - Remove `<ProjectReference Include="..\Mystira.App.Infrastructure.Azure\..." />`

2. **Remove Infrastructure.Data reference from Admin.Api**
   - Edit `Admin.Api/Mystira.App.Admin.Api.csproj`
   - Remove infrastructure references

3. **Update Program.cs DI**
   - Wire infrastructure via DI, not direct references
   - Register implementations from Infrastructure in API/Admin.Api Program.cs
   - Example:
     ```csharp
     // API/Program.cs
     builder.Services.AddInfrastructureData(builder.Configuration);
     builder.Services.AddInfrastructureAzure(builder.Configuration);
     ```

4. **Verify dependency flow**
   - API references: Application, Domain, Contracts, Shared ONLY
   - Admin.Api references: Application, Domain, Contracts, Shared ONLY
   - Infrastructure wired via DI

5. **Test**
   - All tests pass
   - Applications run correctly
   - DI resolution works

**Deliverables**:
- ✅ API has no infrastructure project references
- ✅ Admin.Api has no infrastructure project references
- ✅ Infrastructure wired via DI only

**Success Criteria**:
- API.csproj: 4 project references (Application, Domain, Contracts, Shared)
- Admin.Api.csproj: 4 project references (Application, Domain, Contracts, Shared)
- All tests pass

**Estimated Effort**: 1 week

---

### Phase 8: Testing & Documentation (Weeks 20-21)

**Goal**: Comprehensive testing and documentation updates.

**Tasks**:

#### Week 20: Testing

1. **Unit test coverage**
   - Add unit tests for all use cases
   - Target: 80%+ coverage of use cases
   - Use mocks for port dependencies

2. **Integration tests**
   - Test API endpoints
   - Test admin API endpoints
   - Test PWA API clients
   - Verify end-to-end workflows

3. **Performance testing**
   - Baseline performance metrics
   - Compare before/after refactoring
   - Ensure no regressions

4. **Manual testing**
   - Test all features manually
   - Admin dashboard
   - PWA functionality
   - Scenario creation flow

#### Week 21: Documentation

1. **Update all READMEs**
   - Reflect new architecture
   - Update dependency diagrams
   - Add use case documentation

2. **Create architecture guide**
   - Document hexagonal architecture patterns
   - Provide examples
   - Code review guidelines

3. **Update team wiki**
   - Onboarding documentation
   - Architecture decision records (ADRs)
   - Migration guide for new code

4. **Code comments**
   - Add XML documentation to use cases
   - Document port interfaces
   - Explain complex business logic

**Deliverables**:
- ✅ 80%+ test coverage
- ✅ All documentation updated
- ✅ Architecture guide created

**Success Criteria**:
- All tests pass
- Test coverage meets targets
- Documentation complete and accurate

**Estimated Effort**: 2 weeks

---

### Phase 9: Deployment & Monitoring (Week 22-23)

**Goal**: Deploy refactored codebase to production safely.

**Tasks**:

#### Week 22: Staging Deployment

1. **Deploy to staging**
   - Deploy refactored codebase
   - Run smoke tests
   - Monitor for issues

2. **Staged rollout**
   - Enable feature flags gradually
   - Monitor metrics
   - Gather feedback

3. **Performance validation**
   - Verify performance metrics
   - Check for regressions
   - Optimize if needed

4. **Bug fixes**
   - Fix any issues found
   - Re-test
   - Re-deploy

#### Week 23: Production Deployment

1. **Production deployment**
   - Deploy to production
   - Blue-green or canary deployment
   - Monitor closely

2. **Monitoring**
   - Set up alerts
   - Monitor error rates
   - Monitor performance
   - Track user feedback

3. **Rollback plan**
   - Have rollback ready
   - Document rollback procedure
   - Test rollback in staging

4. **Post-deployment review**
   - Team retrospective
   - Document lessons learned
   - Update process for future

**Deliverables**:
- ✅ Deployed to production
- ✅ Monitoring in place
- ✅ No critical issues

**Success Criteria**:
- Production deployment successful
- No increase in error rates
- Performance metrics acceptable
- Team satisfied with result

**Estimated Effort**: 2 weeks

---

### Phase 10: Cleanup & Optimization (Weeks 24-25)

**Goal**: Final cleanup and optimization.

**Tasks**:

1. **Remove dead code**
   - Delete old service files (if any remain)
   - Remove unused dependencies
   - Clean up commented code

2. **Code quality**
   - Run code analysis tools
   - Fix warnings
   - Improve code smells

3. **Performance optimization**
   - Profile application
   - Optimize hot paths
   - Reduce allocations

4. **Technical debt**
   - Address remaining TODOs
   - Fix technical debt introduced during migration
   - Document future improvements

5. **Team knowledge sharing**
   - Final presentation
   - Share learnings
   - Update best practices

**Deliverables**:
- ✅ Codebase clean
- ✅ Performance optimized
- ✅ Team trained

**Success Criteria**:
- Zero technical debt from migration
- Performance meets targets
- Team confident with new architecture

**Estimated Effort**: 2 weeks

---

## 📊 Risk Management

### High Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Breaking existing functionality | HIGH | MEDIUM | Comprehensive testing before moving code |
| Performance regression | MEDIUM | LOW | Performance testing, profiling |
| Team resistance to change | MEDIUM | MEDIUM | Clear communication, training, pair programming |
| Scope creep | HIGH | HIGH | Strict phase boundaries, no feature work during refactoring |
| Extended timeline | MEDIUM | MEDIUM | Buffer weeks, phased approach allows early stopping |

### Risk Mitigation Strategies

1. **Comprehensive Testing**
   - Write tests BEFORE moving code
   - Integration tests to catch regressions
   - Automated test suite runs on every commit

2. **Incremental Approach**
   - One file at a time
   - Test after each change
   - Commit frequently

3. **Feature Flags**
   - Toggle between old/new implementations
   - Gradual rollout to users
   - Quick rollback if issues

4. **Pair Programming**
   - Knowledge sharing
   - Fewer mistakes
   - Better code quality

5. **Code Reviews**
   - All changes reviewed
   - Architecture patterns enforced
   - Team alignment

## 📈 Success Metrics

### Architectural Metrics

**Before Refactoring**:
- Service files in API layers: 88
- Repository files in API layers: 12
- Infrastructure deps in Application: 138
- Port interfaces in Infrastructure: ~27
- Test coverage: Unknown

**Target After Refactoring**:
- Service files in API layers: 0 ✅
- Repository files in API layers: 0 ✅
- Infrastructure deps in Application: 0 ✅
- Port interfaces in Infrastructure: 0 ✅
- Test coverage: 80%+ ✅

### Business Metrics

- Deployment frequency: Increase (easier to deploy)
- Change failure rate: Decrease (better testing)
- Mean time to recovery: Decrease (better architecture)
- Lead time for changes: Decrease (clearer code)

### Team Metrics

- Developer satisfaction: Increase
- Onboarding time: Decrease
- Code review time: Decrease (clearer patterns)

## 🎓 Team Training

### Required Knowledge

1. **Hexagonal Architecture**
   - Ports & adapters pattern
   - Dependency Inversion Principle
   - Clean architecture

2. **SOLID Principles**
   - Single Responsibility
   - Open/Closed
   - Liskov Substitution
   - Interface Segregation
   - Dependency Inversion

3. **Use Case Pattern**
   - Single responsibility use cases
   - Command/query separation
   - Input/output DTOs

### Training Plan

1. **Week -1**: Architecture workshop (4 hours)
2. **Week 0**: Hands-on refactoring session (2 hours)
3. **Ongoing**: Pair programming, code reviews
4. **Monthly**: Architecture review sessions

## 📚 References

### Internal Documentation

- [Infrastructure.StoryProtocol README](../../src/Mystira.App.Infrastructure.StoryProtocol/README.md) - Exemplar project ⭐
- [Application Layer README](../../src/Mystira.App.Application/README.md) - Analysis & TODOs
- [API README](../../src/Mystira.App.Api/README.md) - Analysis & TODOs
- [Admin.Api README](../../src/Mystira.App.Admin.Api/README.md) - Analysis & TODOs

### External Resources

- [Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/) - Alistair Cockburn
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [Ports and Adapters](https://herbertograca.com/2017/11/16/explicit-architecture-01-ddd-hexagonal-onion-clean-cqrs-how-i-put-it-all-together/) - Herberto Graça
- [SOLID Principles](https://www.digitalocean.com/community/conceptual-articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design)

## 🔄 Review & Iteration

### Weekly Reviews

- Monday: Week planning
- Friday: Week retrospective
- Adjust plan based on learnings

### Phase Gates

- End of each phase: Go/No-Go decision
- Review metrics
- Adjust timeline if needed

### Communication

- Daily: Standup updates
- Weekly: Team sync, stakeholder update
- Monthly: Executive summary

---

**Document Version**: 1.0
**Last Updated**: 2025-11-23
**Next Review**: 2025-12-01
**DRI**: [To be assigned]
