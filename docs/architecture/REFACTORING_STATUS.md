# Hexagonal Architecture Refactoring - Status

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

## 🔄 In Progress

### Next Steps (Priority Order)

#### Phase 1: Repository Implementation (In Progress)

1. ✅ Implement `GameSessionRepository` in `Infrastructure.Data`
2. ✅ Implement `UserProfileRepository` in `Infrastructure.Data`
3. ✅ Implement `AccountRepository` in `Infrastructure.Data`
4. ✅ Implement `UnitOfWork` with DbContext
5. ✅ Register repositories in DI container (Api and Admin.Api)
6. ✅ Migrate `GameSessionApiService` to use `GameSessionRepository`
7. 🔄 Migrate `UserProfileApiService` to use `UserProfileRepository`
8. 🔄 Migrate `AccountApiService` to use `AccountRepository`
9. Create repositories for other entities:
   - `IScenarioRepository`
   - `IMediaRepository`

#### Phase 2: DTOs Migration

1. Move request DTOs from `ApiModels.cs` to `Contracts/Requests/`
2. Move response DTOs to `Contracts/Responses/`
3. Update API controllers to use Contracts
4. Remove duplicate models from PWA

#### Phase 3: Application Layer

1. Create use cases for major operations:
   - `CreateGameSessionUseCase`
   - `GetScenariosUseCase`
   - `UpdateUserProfileUseCase`
2. Create application services
3. Add AutoMapper profiles for DTO ↔ Domain mapping

#### Phase 4: Large File Refactoring

1. **ApiClient.cs (957 lines)** → Split into:
   - `BaseApiClient` (common HTTP logic)
   - `ScenarioApiClient`
   - `GameSessionApiClient`
   - `UserProfileApiClient`
   - `MediaApiClient`

2. **MediaApiService.cs (705 lines)** → Split by responsibility:
   - `MediaUploadService`
   - `MediaMetadataService`
   - `MediaTranscodingService`

3. **ScenarioApiService.cs (692 lines)** → Move to Application layer

   - `CreateScenarioUseCase`
   - `UpdateScenarioUseCase`
   - `GetScenariosUseCase`

4. **ApiModels.cs (655 lines)** → Move to Contracts project

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

#### Phase 6: Code Warnings Fix

- CS0109: Remove duplicate member declarations
- CS8618: Add nullable annotations or `required` modifier
- CS8601: Add null checks
- CS4014: Fix async warnings (use `ConfigureAwait(false)` or await)
- CS0169: Remove unused fields

## 📋 Migration Checklist

### For Each Entity

- [ ] Create repository interface in `Infrastructure.Data/Repositories/`
- [ ] Implement repository in `Infrastructure.Data/Repositories/`
- [ ] Create DTOs in `Contracts/Requests/` and `Contracts/Responses/`
- [ ] Create use case in `Application/UseCases/`
- [ ] Update API controllers to use use cases
- [ ] Update services to use repositories
- [ ] Add unit tests

### For Large Files

- [ ] Identify responsibilities
- [ ] Extract classes/interfaces
- [ ] Split into smaller files (<300 lines each)
- [ ] Update references
- [ ] Verify tests still pass

## 🎯 Success Criteria

1. ✅ No files > 500 lines
2. ✅ All DTOs in Contracts project
3. ✅ All business logic in Application layer
4. ✅ All data access through repositories
5. ✅ All JavaScript converted to TypeScript
6. ✅ No security warnings
7. ✅ No code warnings
8. ✅ All tests passing

## 📚 Resources

- [Hexagonal Architecture Guide](HEXAGONAL_REFACTORING_PLAN.md)
- [Repository Pattern Best Practices](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
