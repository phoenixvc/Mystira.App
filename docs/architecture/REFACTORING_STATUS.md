# Hexagonal Architecture Refactoring - Status

## 📝 Recent Changes Summary

### Last PR (Merged to dev)
- ✅ Phase 1: Repository Implementation - All services migrated to use repositories
- ✅ Phase 2: DTOs Migration - All DTOs moved to Contracts project
- ✅ Fixed null reference bug in `AdminController.cs`
- ✅ Migrated Admin.Api services (`UserBadgeApiService`, `ClientApiService`, `AvatarApiService`) to use Contracts DTOs

### Current Branch (In Progress)
- ✅ Phase 3: Application Layer - Created 8 use cases:
  - Scenario use cases: `GetScenariosUseCase`, `CreateScenarioUseCase`, `UpdateScenarioUseCase`, `DeleteScenarioUseCase`, `ValidateScenarioUseCase`
  - GameSession use cases: `CreateGameSessionUseCase`, `MakeChoiceUseCase`, `ProgressSceneUseCase`
- ✅ Moved `ScenarioSchemaDefinitions` to `Application.Validation`
- ✅ Fixed circular dependencies and package version issues
- ⏳ Next: Create UserProfile use cases, integrate use cases into services, add AutoMapper

## ✅ Completed

### 1. Project Structure Created

- ✅ `Mystira.App.Contracts` - DTOs and API contracts
- ✅ `Mystira.App.Application` - Application layer (use cases)
- ✅ `Mystira.App.Infrastructure.Data` - Repository layer
- ✅ All projects added to solution
- ✅ Directory structure created

### 2. Security Fixes

- ✅ Updated `System.Text.Json` from 8.0.4 → 9.0.0 (fixes NU1903)
- ✅ Fixed `Microsoft.Extensions.Configuration.Binder` version mismatch (NU1603)

### 3. Foundation Files

- ✅ Created `IRepository<T>` generic repository interface
- ✅ Created `IGameSessionRepository` domain-specific repository
- ✅ Created `IUnitOfWork` interface
- ✅ Created refactoring plan document

### 4. Repository Layer Implementation

- ✅ Implemented `Repository<T>` base class
- ✅ Implemented `GameSessionRepository` with domain-specific queries
- ✅ Implemented `UserProfileRepository` with domain-specific queries
- ✅ Implemented `AccountRepository` with domain-specific queries
- ✅ Implemented `UnitOfWork` for transaction management
- ✅ Registered repositories and UnitOfWork in DI containers (Api and Admin.Api)
- ✅ Migrated `GameSessionApiService` to use repository pattern instead of direct DbContext access
- ✅ Migrated `UserProfileApiService` to use repository pattern
- ✅ Migrated `AccountApiService` to use repository pattern
- ✅ Created `IScenarioRepository` and `ScenarioRepository`
- ✅ Created `ICharacterMapRepository` and `CharacterMapRepository`
- ✅ Created `IContentBundleRepository` and `ContentBundleRepository`
- ✅ Migrated `ContentBundleService` to use repository pattern
- ✅ Migrated `CharacterMapApiService` to use repository pattern
- ✅ Removed DbContext dependency from `UserProfileApiService` (CharacterMapRepository)
- ✅ Migrated `ScenarioApiService` to use `IScenarioRepository`, `IAccountRepository`, `IGameSessionRepository`, and `IUnitOfWork`
- ✅ Created `IBadgeConfigurationRepository` and `BadgeConfigurationRepository`
- ✅ Created `IUserBadgeRepository` and `UserBadgeRepository`
- ✅ Migrated `BadgeConfigurationApiService` to use repository pattern
- ✅ Migrated `UserBadgeApiService` to use repository pattern
- ✅ Created `IPendingSignupRepository` and `PendingSignupRepository`
- ✅ Migrated `PasswordlessAuthService` to use repository pattern
- ✅ Created `IMediaAssetRepository` and `MediaAssetRepository` (in Api project)
- ✅ Migrated `MediaApiService` to use repository pattern
- ✅ Created file-based repositories (`IMediaMetadataFileRepository`, `ICharacterMediaMetadataFileRepository`, `ICharacterMapFileRepository`, `IAvatarConfigurationFileRepository`)
- ✅ Migrated `AvatarApiService`, `MediaMetadataService`, `CharacterMediaMetadataService`, and `CharacterMapFileService` to use repository pattern

## 🔄 In Progress

### Next Steps (Priority Order)

#### Phase 1: Repository Implementation ✅ COMPLETED

1. ✅ Implement `GameSessionRepository` in `Infrastructure.Data`
2. ✅ Implement `UserProfileRepository` in `Infrastructure.Data`
3. ✅ Implement `AccountRepository` in `Infrastructure.Data`
4. ✅ Implement `UnitOfWork` with DbContext
5. ✅ Register repositories in DI container (Api and Admin.Api)
6. ✅ Migrate `GameSessionApiService` to use `GameSessionRepository`
7. ✅ Migrate `UserProfileApiService` to use `UserProfileRepository`
8. ✅ Migrate `AccountApiService` to use `AccountRepository`
9. ✅ Create repositories for other entities:
   - ✅ `IScenarioRepository` and migrated `ScenarioApiService`
   - ✅ `IBadgeConfigurationRepository` and migrated `BadgeConfigurationApiService`
   - ✅ `IUserBadgeRepository` and migrated `UserBadgeApiService`
   - ✅ `IPendingSignupRepository` and migrated `PasswordlessAuthService`
   - ✅ `IMediaAssetRepository` (in Api project) and migrated `MediaApiService`
   - ✅ File-based repositories for singleton entities

#### Phase 2: DTOs Migration ✅ COMPLETED

1. ✅ Move request DTOs from `ApiModels.cs` to `Contracts/Requests/`
2. ✅ Move response DTOs to `Contracts/Responses/`
3. ✅ Update API controllers to use Contracts
4. ✅ Update Admin.Api controllers and services to use Contracts (with aliases for ambiguous types)
5. ✅ Delete Api.Api's `ApiModels.cs` (all DTOs migrated to Contracts)
6. ⚠️ Admin.Api's `ApiModels.cs` kept temporarily (has Admin-specific differences: ProgressSceneRequest with NewSceneId, CreateUserProfileRequest without Id/SelectedAvatarMediaId, PasswordlessVerifyResponse without token expiration fields)

#### Phase 3: Application Layer 🔄 IN PROGRESS

1. ✅ Created Application project structure
2. ✅ Created Scenario use cases:
   - ✅ `GetScenariosUseCase` - Handles scenario querying with filtering and pagination
   - ✅ `CreateScenarioUseCase` - Handles scenario creation with schema validation
   - ✅ `UpdateScenarioUseCase` - Handles scenario updates with validation
   - ✅ `DeleteScenarioUseCase` - Handles scenario deletion
   - ✅ `ValidateScenarioUseCase` - Validates scenario business rules (scene references, etc.)
3. ✅ Created GameSession use cases:
   - ✅ `CreateGameSessionUseCase` - Handles starting a new game session
   - ✅ `MakeChoiceUseCase` - Handles making choices in game sessions
   - ✅ `ProgressSceneUseCase` - Handles progressing to specific scenes
4. ✅ Moved `ScenarioSchemaDefinitions` to `Application.Validation` (shared validation logic)
5. ✅ Fixed circular dependencies and package versions
6. ✅ Created UserProfile use cases:
   - ✅ `CreateUserProfileUseCase`
   - ✅ `UpdateUserProfileUseCase`
   - ✅ `GetUserProfileUseCase`
   - ✅ `DeleteUserProfileUseCase`
7. ✅ Registered all use cases in DI containers (`Program.cs` for both Api and Admin.Api)
8. 🔄 Update services to use use cases instead of direct repository access:
   - ✅ `ScenarioApiService` → Delegates to `GetScenariosUseCase`, `CreateScenarioUseCase`, `UpdateScenarioUseCase`, `DeleteScenarioUseCase`, `ValidateScenarioUseCase`
   - ⏳ `GameSessionApiService` → Use `CreateGameSessionUseCase`, `MakeChoiceUseCase`, `ProgressSceneUseCase`
   - ⏳ `UserProfileApiService` → Use `CreateUserProfileUseCase`, `UpdateUserProfileUseCase`, `GetUserProfileUseCase`, `DeleteUserProfileUseCase`
9. ⏳ Create application services (coordinate multiple use cases if needed)
10. ⏳ Add AutoMapper profiles for DTO ↔ Domain mapping
11. ⏳ Update API controllers to use use cases (via services or directly)

#### Phase 4: Large File Refactoring ⏳ PENDING

1. **ApiClient.cs (957 lines)** → Split into:
   - `BaseApiClient` (common HTTP logic)
   - `ScenarioApiClient`
   - `GameSessionApiClient`
   - `UserProfileApiClient`
   - `MediaApiClient`
   - `AuthApiClient`

2. **MediaApiService.cs (555 lines)** → Split by responsibility:
   - `MediaUploadService` (upload logic)
   - `MediaMetadataService` (metadata management)
   - `MediaTranscodingService` (transcoding logic)

3. **ScenarioApiService.cs (692 lines)** → Refactor to use Application layer:
   - ✅ Use cases created: `CreateScenarioUseCase`, `UpdateScenarioUseCase`, `GetScenariosUseCase`, `DeleteScenarioUseCase`, `ValidateScenarioUseCase`
   - ⏳ Update `ScenarioApiService` to delegate to use cases instead of direct repository access
   - ⏳ Remove business logic from service (move to use cases)

4. **GameSessionApiService.cs** → Refactor to use Application layer:
   - ✅ Use cases created: `CreateGameSessionUseCase`, `MakeChoiceUseCase`, `ProgressSceneUseCase`
   - ⏳ Update `GameSessionApiService` to delegate to use cases instead of direct repository access

5. **ScenarioRequestCreator.cs (727 lines)** → Consider refactoring:
   - Extract validation logic to use cases
   - Simplify mapping logic
   - Consider AutoMapper for complex mappings

6. **ApiModels.cs** → ✅ COMPLETED (moved to Contracts project)

#### Phase 5: TypeScript Migration

1. Create `tsconfig.json` in PWA
2. Convert `.js` files to `.ts`:
   - `service-worker.js` → `service-worker.ts`
   - `pwaInstall.js` → `pwaInstall.ts`
   - `imageCacheManager.js` → `imageCacheManager.ts`
   - `audioPlayer.js` → `audioPlayer.ts`
   - `dice.js` → `dice.ts`
   - `outside-click-handler.js` → `outside-click-handler.ts`
3. Add type definitions
4. Update build process

#### Phase 6: Code Warnings Fix ⏳ PENDING

- ⏳ CS0109: Remove duplicate member declarations
- ⏳ CS8618: Add nullable annotations or `required` modifier
- ⏳ CS8601: Add null checks
- ⏳ CS4014: Fix async warnings (use `ConfigureAwait(false)` or await)
- ⏳ CS0169: Remove unused fields

#### Phase 7: Integration & Testing ⏳ PENDING

1. ⏳ Register all use cases in DI containers
2. ⏳ Update services to delegate to use cases
3. ⏳ Update controllers if needed (may continue using services)
4. ⏳ Add integration tests for use cases
5. ⏳ Verify all existing tests still pass
6. ⏳ Clean up Admin.Api.Models.ApiModels.cs (remove migrated DTOs, keep only Admin-specific ones)

## 📋 Migration Checklist

### For Each Entity

- [x] Create repository interface in `Infrastructure.Data/Repositories/` ✅
- [x] Implement repository in `Infrastructure.Data/Repositories/` ✅
- [x] Create DTOs in `Contracts/Requests/` and `Contracts/Responses/` ✅
- [x] Create use case in `Application/UseCases/` (Scenarios ✅, GameSessions ✅, UserProfiles ⏳)
- [ ] Update API controllers to use use cases (pending - services still use repositories directly)
- [x] Update services to use repositories ✅
- [ ] Add unit tests (pending)

### For Large Files

- [ ] Identify responsibilities
- [ ] Extract classes/interfaces
- [ ] Split into smaller files (<300 lines each)
- [ ] Update references
- [ ] Verify tests still pass

## 🎯 Success Criteria

1. ⏳ No files > 500 lines (in progress - ApiClient.cs, ScenarioRequestCreator.cs still large)
2. ✅ All DTOs in Contracts project (Api.Api completed, Admin.Api has Admin-specific differences)
3. 🔄 All business logic in Application layer (use cases created, services not yet migrated)
4. ✅ All data access through repositories (all services migrated)
5. ⏳ All JavaScript converted to TypeScript (pending)
6. ✅ No security warnings (System.Text.Json updated, Configuration.Binder fixed)
7. ⏳ No code warnings (partially addressed, some remain)
8. ⏳ All tests passing (needs verification after use case migration)

## 📚 Resources

- [Hexagonal Architecture Guide](HEXAGONAL_REFACTORING_PLAN.md)
- [Repository Pattern Best Practices](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
