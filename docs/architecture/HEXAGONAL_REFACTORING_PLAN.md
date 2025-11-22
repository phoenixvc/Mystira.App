# Hexagonal Architecture Refactoring Plan

## Overview
This document outlines the plan to restructure the Mystira.App repository to follow hexagonal (ports and adapters) architecture principles, improve project organization, introduce a repository layer, and address technical debt.

## Current Architecture Issues

### 1. **Lack of Clear Layer Separation**
- Domain models mixed with DTOs
- Business logic in API controllers
- No clear application layer
- Direct DbContext usage in services

### 2. **Large Files (>500 lines)**
- `ApiClient.cs` (957 lines)
- `MediaApiService.cs` (705 lines)
- `ScenarioApiService.cs` (692 lines)
- `ApiModels.cs` (655 lines)
- `ScenarioRequestCreator.cs` (637 lines)
- And 9 more files...

### 3. **Security & Dependency Issues**
- System.Text.Json 8.0.4 has security vulnerability (NU1903)
- Microsoft.Extensions.Configuration.Binder version mismatch (NU1603)
- Multiple code warnings (CS0109, CS8618, CS8601, CS4014, CS0169)

### 4. **JavaScript Instead of TypeScript**
- 7 JavaScript files need conversion to TypeScript

## Target Architecture

### Hexagonal Architecture Layers

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│  (APIs, Controllers, PWA)               │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         Application Layer               │
│  (Use Cases, Application Services)      │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         Domain Layer                    │
│  (Entities, Value Objects, Domain Logic)│
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│      Infrastructure Layer               │
│  (Repositories, External Services)     │
└─────────────────────────────────────────┘
```

### New Project Structure

```
src/
├── Mystira.App.Domain/              # Core domain (no changes)
│   ├── Models/
│   ├── ValueObjects/
│   └── DomainServices/
│
├── Mystira.App.Contracts/           # NEW: DTOs and API contracts
│   ├── Requests/
│   ├── Responses/
│   └── DTOs/
│
├── Mystira.App.Application/        # NEW: Application layer
│   ├── UseCases/
│   │   ├── Scenarios/
│   │   ├── GameSessions/
│   │   └── UserProfiles/
│   ├── Services/
│   └── Mappings/
│
├── Mystira.App.Infrastructure.Data/ # NEW: Repository layer
│   ├── Repositories/
│   │   ├── IGameSessionRepository.cs
│   │   ├── GameSessionRepository.cs
│   │   └── ...
│   ├── UnitOfWork/
│   └── DbContext/
│
├── Mystira.App.Infrastructure.Azure/ # Existing (keep)
│
├── Mystira.App.Api/                # Refactored
│   ├── Controllers/                # Thin controllers
│   ├── Middleware/
│   └── Program.cs
│
├── Mystira.App.Admin.Api/          # Refactored
│   └── (same structure as Api)
│
└── Mystira.App.PWA/                # Refactored
    ├── Components/
    ├── Pages/
    ├── Services/                   # Refactored ApiClient
    └── wwwroot/
        └── ts/                      # TypeScript instead of JS
```

## Implementation Plan

### Phase 1: Foundation (Critical Fixes) ✅ COMPLETED
1. ✅ Fix security warnings
   - ✅ Updated System.Text.Json from 8.0.4 → 9.0.0 (fixes NU1903)
   - ✅ Fixed Microsoft.Extensions.Configuration.Binder version mismatch (NU1603)
2. ✅ Create Contracts project
   - ✅ Created `Mystira.App.Contracts` project
   - ✅ Moved all DTOs from `ApiModels.cs` to `Contracts/Requests/` and `Contracts/Responses/`
   - ✅ Organized DTOs by domain (Scenarios, GameSessions, UserProfiles, Auth, Badges, etc.)
   - ✅ Updated all API controllers and services to use Contracts DTOs
   - ✅ Deleted `Api.Api/Models/ApiModels.cs` (fully migrated)
   - ⚠️ Kept `Admin.Api/Models/ApiModels.cs` temporarily (Admin-specific differences)
3. ⏳ Fix code warnings (partially completed)
   - CS0109: Remove duplicate member declarations
   - CS8618: Add nullable annotations
   - CS8601: Add null checks
   - CS4014: Fix async warnings
   - CS0169: Remove unused fields

### Phase 2: Repository Layer ✅ COMPLETED
1. ✅ Created Infrastructure.Data project
2. ✅ Defined repository interfaces (`IRepository<T>`, domain-specific interfaces)
3. ✅ Implemented repositories for all entities:
   - ✅ `GameSessionRepository`, `UserProfileRepository`, `AccountRepository`
   - ✅ `ScenarioRepository`, `CharacterMapRepository`, `ContentBundleRepository`
   - ✅ `BadgeConfigurationRepository`, `UserBadgeRepository`
   - ✅ `PendingSignupRepository`
   - ✅ File-based repositories (`MediaMetadataFileRepository`, `CharacterMediaMetadataFileRepository`, `CharacterMapFileRepository`, `AvatarConfigurationFileRepository`)
   - ✅ `MediaAssetRepository` (in Api project to avoid circular dependencies)
4. ✅ Implemented `UnitOfWork` pattern for transaction management
5. ✅ Replaced direct DbContext usage in all services:
   - ✅ `GameSessionApiService`, `UserProfileApiService`, `AccountApiService`
   - ✅ `ScenarioApiService`, `CharacterMapApiService`, `ContentBundleService`
   - ✅ `BadgeConfigurationApiService`, `UserBadgeApiService`
   - ✅ `PasswordlessAuthService`, `MediaApiService`
   - ✅ `AvatarApiService`, `MediaMetadataService`, `CharacterMediaMetadataService`, `CharacterMapFileService`
6. ✅ Registered all repositories and UnitOfWork in DI containers (Api and Admin.Api)

### Phase 3: Application Layer 🔄 IN PROGRESS
1. ✅ Created Application project (`Mystira.App.Application`)
2. ✅ Created use cases for Scenarios:
   - ✅ `GetScenariosUseCase` - Query scenarios with filtering and pagination
   - ✅ `CreateScenarioUseCase` - Create scenarios with schema validation
   - ✅ `UpdateScenarioUseCase` - Update scenarios with validation
   - ✅ `DeleteScenarioUseCase` - Delete scenarios
   - ✅ `ValidateScenarioUseCase` - Validate scenario business rules
3. ✅ Created use cases for GameSessions:
   - ✅ `CreateGameSessionUseCase` - Start new game sessions
   - ✅ `MakeChoiceUseCase` - Handle choices in game sessions
   - ✅ `ProgressSceneUseCase` - Progress to specific scenes
4. ✅ Moved `ScenarioSchemaDefinitions` to `Application.Validation` (shared validation logic)
5. ✅ Fixed circular dependencies (removed Application reference from Infrastructure.Data)
6. ✅ Updated package versions (Microsoft.Extensions.Logging.Abstractions to 9.0.0)
7. ⏳ Remaining use cases to create:
   - `CreateUserProfileUseCase`
   - `UpdateUserProfileUseCase`
   - `GetUserProfileUseCase`
   - `DeleteUserProfileUseCase`
8. ⏳ Create application services (coordinate multiple use cases)
9. ⏳ Add AutoMapper profiles for DTO ↔ Domain mapping
10. ⏳ Update services to use use cases instead of direct repository access
11. ⏳ Register use cases in DI containers

### Phase 4: Refactor Large Files ⏳ PENDING
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
3. **ScenarioApiService.cs (692 lines)** → Refactor to use Application layer use cases:
   - ✅ Use cases created: `CreateScenarioUseCase`, `UpdateScenarioUseCase`, `GetScenariosUseCase`, `DeleteScenarioUseCase`
   - ⏳ Update `ScenarioApiService` to delegate to use cases
   - ⏳ Remove business logic from service (move to use cases)
4. **ScenarioRequestCreator.cs (727 lines)** → Consider:
   - Extract validation logic to use cases
   - Simplify mapping logic
   - Consider AutoMapper for complex mappings
5. **GameSessionApiService.cs** → Refactor to use Application layer use cases:
   - ✅ Use cases created: `CreateGameSessionUseCase`, `MakeChoiceUseCase`, `ProgressSceneUseCase`
   - ⏳ Update `GameSessionApiService` to delegate to use cases

### Phase 5: TypeScript Migration
1. Set up TypeScript configuration
2. Convert .js files to .ts
3. Add type definitions
4. Update build process

### Phase 6: Cleanup & Documentation
1. Update README files
2. Add architecture diagrams
3. Update CI/CD if needed
4. **DRY and SOLID Analysis**: 
   - Analyze repository for code duplication (DRY violations)
   - Review classes for Single Responsibility Principle (SRP)
   - Identify opportunities for Interface Segregation
   - Refactor large classes/methods to improve maintainability
   - Extract common functionality into shared services/utilities

## Benefits

1. **Separation of Concerns**: Clear boundaries between layers
2. **Testability**: Easy to mock repositories and services
3. **Maintainability**: Smaller, focused files
4. **Scalability**: Easy to add new features
5. **Type Safety**: TypeScript provides better type checking
6. **Security**: Updated dependencies

## Migration Strategy

- Incremental refactoring (not big bang)
- Maintain backward compatibility during transition
- Update tests as we go
- Document breaking changes

