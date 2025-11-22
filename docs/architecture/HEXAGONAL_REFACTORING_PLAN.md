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

### Phase 1: Foundation (Critical Fixes)
1. ✅ Fix security warnings
   - Update System.Text.Json to latest secure version
   - Fix Configuration.Binder version mismatch
2. ✅ Create Contracts project
   - Move DTOs from ApiModels.cs
   - Create request/response models
3. ✅ Fix code warnings
   - CS0109: Remove duplicate member declarations
   - CS8618: Add nullable annotations
   - CS8601: Add null checks
   - CS4014: Fix async warnings
   - CS0169: Remove unused fields

### Phase 2: Repository Layer
1. Create Infrastructure.Data project
2. Define repository interfaces
3. Implement repositories
4. Replace direct DbContext usage

### Phase 3: Application Layer
1. Create Application project
2. Extract use cases from services
3. Create application services
4. Add mapping profiles

### Phase 4: Refactor Large Files
1. ApiClient.cs → Split into:
   - BaseApiClient (common HTTP logic)
   - ScenarioApiClient
   - GameSessionApiClient
   - UserProfileApiClient
   - etc.
2. MediaApiService.cs → Split by responsibility
3. ScenarioApiService.cs → Move to Application layer
4. ApiModels.cs → Move to Contracts project

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

