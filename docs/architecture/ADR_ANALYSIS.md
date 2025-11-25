# Architecture Decision Records - Analysis & Recommendations

**Date**: 2025-11-24
**Status**: Analysis Complete

---

## Executive Summary

This document analyzes the Mystira.App codebase to identify architectural decisions that should be documented as ADRs. The project has undergone significant architectural improvements, including hexagonal architecture refactoring and CQRS implementation, but many of these decisions are not yet formally documented.

---

## Current ADR Status

### ✅ Documented ADRs

1. **ADR-0001**: Adopt CQRS Pattern
2. **ADR-0002**: Adopt Specification Pattern

### 📝 Required ADRs (High Priority)

3. **ADR-0003**: Adopt Hexagonal Architecture (Ports and Adapters)
4. **ADR-0004**: Use MediatR for Request/Response Handling
5. **ADR-0005**: Separate API and Admin API
6. **ADR-0006**: Use Entity Framework Core for Data Access

### 📝 Recommended ADRs (Medium Priority)

7. **ADR-0007**: Infrastructure Modularity Strategy
8. **ADR-0008**: Soft Delete Pattern for Data Retention
9. **ADR-0009**: Use Azure Blob Storage for Media Assets
10. **ADR-0010**: Discord Bot Integration for Messaging

### 📝 Optional ADRs (Low Priority)

11. **ADR-0011**: Use Newtonsoft.Json for JSON Serialization
12. **ADR-0012**: Authentication Strategy (JWT/Bearer Tokens)
13. **ADR-0013**: Story Protocol Integration for Content NFTs

---

## Detailed ADR Recommendations

### High Priority ADRs

#### ADR-0003: Adopt Hexagonal Architecture (Ports and Adapters)

**Why This Needs Documentation:**
- **Major architectural shift**: Transformed from layered architecture to hexagonal
- **164 files refactored** in this effort
- **Complete dependency inversion**: Application layer has zero infrastructure dependencies
- **Critical for new developers**: Understanding ports vs adapters is essential

**Context:**
- Previous architecture had tight coupling between layers
- 229 architectural violations existed (Application → Infrastructure dependencies)
- Testing was difficult due to concrete infrastructure dependencies

**Decision:**
- Adopt Hexagonal Architecture (Ports and Adapters)
- Define ports (interfaces) in Application layer
- Implement adapters in Infrastructure layer
- Enable dependency inversion

**Consequences:**
- ✅ Complete testability (can mock all infrastructure)
- ✅ Infrastructure swappability (Azure → AWS, Discord → Slack)
- ✅ Clear boundaries between layers
- ❌ More classes and interfaces to maintain
- ❌ Learning curve for new developers

---

#### ADR-0004: Use MediatR for Request/Response Handling

**Why This Needs Documentation:**
- **Core technology choice** for CQRS implementation
- **Every command/query** flows through MediatR
- **Impacts all handler implementations**

**Context:**
- Need to decouple controllers from handler implementations
- Want to support pipeline behaviors (logging, validation, caching)
- CQRS requires request/response pattern

**Decision:**
- Use MediatR library (v12.4.1) for request/response handling
- Controllers inject `IMediator` instead of individual handlers
- Commands and Queries implement `IRequest<T>`
- Handlers implement `IRequestHandler<TRequest, TResponse>`

**Consequences:**
- ✅ Decoupled controllers from handlers
- ✅ Pipeline behaviors for cross-cutting concerns
- ✅ Auto-discovery of handlers via assembly scanning
- ✅ Strong typing and compile-time safety
- ❌ Dependency on third-party library
- ❌ Indirection may make debugging harder initially

---

#### ADR-0005: Separate API and Admin API

**Why This Needs Documentation:**
- **Unique architectural decision** not common in all projects
- **Security implications**: Admin operations isolated
- **Impacts routing and deployment**

**Context:**
- Need to separate user-facing operations from admin operations
- Different authorization requirements (user vs admin roles)
- Potential for different deployment models (public vs internal)

**Decision:**
- Create separate projects: `Mystira.App.Api` (user-facing) and `Mystira.App.Admin.Api` (admin)
- Route convention: `/api/` for user operations, `/adminapi/` for admin operations
- Both share Application layer use cases/commands/queries
- Admin API requires Admin role authorization

**Consequences:**
- ✅ Clear separation of concerns (user vs admin operations)
- ✅ Security isolation (admin endpoints not exposed to public)
- ✅ Can deploy separately if needed
- ✅ Easier to audit admin operations
- ❌ Code duplication in some controllers
- ❌ More complex routing configuration

---

#### ADR-0006: Use Entity Framework Core for Data Access

**Why This Needs Documentation:**
- **Fundamental technology choice** for data persistence
- **Impacts all repository implementations**
- **Alternative ORMs exist** (Dapper, NHibernate)

**Context:**
- Need an ORM for database access
- Want change tracking and migrations support
- Prefer LINQ queries over raw SQL
- Using .NET ecosystem

**Decision:**
- Use Entity Framework Core as primary ORM
- Implement Repository Pattern on top of EF Core
- Use Code-First approach with migrations
- Use DbContext as Unit of Work implementation

**Consequences:**
- ✅ Strong LINQ support for queries
- ✅ Automatic change tracking
- ✅ Built-in migration support
- ✅ Great .NET integration
- ❌ Performance overhead compared to Dapper
- ❌ Can be over-engineered for simple queries
- ❌ N+1 query issues require vigilance

---

### Medium Priority ADRs

#### ADR-0007: Infrastructure Modularity Strategy

**Why This Needs Documentation:**
- **Unique project structure**: Separate projects for each infrastructure adapter
- **Swappability strategy**: Azure, Discord, StoryProtocol as separate modules

**Context:**
- Want to isolate infrastructure dependencies
- Need ability to swap implementations (Azure → AWS)
- Different infrastructure components have different lifecycles

**Decision:**
- Create separate projects for each infrastructure adapter:
  - `Mystira.App.Infrastructure.Data` (EF Core, repositories)
  - `Mystira.App.Infrastructure.Azure` (Azure Blob Storage)
  - `Mystira.App.Infrastructure.Discord` (Discord bot)
  - `Mystira.App.Infrastructure.StoryProtocol` (NFT integration)
- Each infrastructure project implements ports from Application layer
- Register implementations via ServiceCollectionExtensions

**Consequences:**
- ✅ Clear module boundaries
- ✅ Easy to swap implementations (replace entire project)
- ✅ Can version infrastructure components independently
- ✅ Parallel development on different adapters
- ❌ More projects to manage
- ❌ Increased solution complexity

---

#### ADR-0008: Soft Delete Pattern for Data Retention

**Why This Needs Documentation:**
- **Pervasive pattern** across all entities
- **Audit/compliance implications**

**Context:**
- Need to retain deleted data for audit trails
- Want to prevent accidental data loss
- Regulatory compliance may require data retention

**Decision:**
- Implement soft delete pattern across all entities
- Add `IsDeleted` boolean flag to `BaseEntity`
- Filter out deleted entities in queries (via Specifications)
- Physical deletes only for GDPR/compliance

**Consequences:**
- ✅ Data retention for audit trails
- ✅ Easier data recovery
- ✅ Compliance-friendly
- ❌ Must remember to filter `IsDeleted` in all queries
- ❌ Database grows larger over time
- ❌ Soft deleted data still counts toward storage

---

### Implementation Status of Undocumented Decisions

| Decision | Implemented | Documented | ADR Needed |
|----------|-------------|------------|------------|
| Hexagonal Architecture | ✅ Yes (100%) | ❌ No | ✅ High Priority |
| MediatR for CQRS | ✅ Yes (Partial) | ❌ No | ✅ High Priority |
| API/Admin API Separation | ✅ Yes | ❌ No | ✅ High Priority |
| Entity Framework Core | ✅ Yes | ❌ No | ✅ High Priority |
| Infrastructure Modularity | ✅ Yes | ❌ No | ⚠️ Medium Priority |
| Soft Delete Pattern | ✅ Yes | ❌ No | ⚠️ Medium Priority |
| Azure Blob Storage | ✅ Yes | ❌ No | ⚠️ Low Priority |
| Discord Integration | ✅ Yes | ❌ No | ⚠️ Low Priority |

---

## CQRS Migration Status (Phase 5)

### ✅ Migrated to CQRS

- **Scenario** entity
  - Commands: `CreateScenarioCommand`, `UpdateScenarioCommand`, `DeleteScenarioCommand`
  - Queries: `GetScenarioQuery`, `GetScenariosQuery`
  - Specifications: 8 specifications created

### 🔄 Needs CQRS Migration (Priority Order)

1. **ContentBundle** (High Priority)
   - Used frequently in queries
   - Complex relationships (Scenarios, Purchases)
   - Needs specifications for filtering by age group, price range, etc.

2. **GameSession** (High Priority)
   - Active user sessions
   - Complex state management
   - Needs specifications for active sessions, user history

3. **UserProfile** (High Priority)
   - Core user entity
   - Complex queries (badges, sessions, purchases)
   - Needs specifications for user lookup, progress tracking

4. **Account** (Medium Priority)
   - Authentication/authorization entity
   - Simple CRUD operations
   - May not need many specifications

5. **MediaAsset** (Medium Priority)
   - Media file metadata
   - Simple queries
   - Mostly accessed by ID

6. **BadgeConfiguration** (Low Priority)
   - Configuration entity
   - Rare changes
   - Simple queries

7. **UserBadge** (Low Priority)
   - User achievement tracking
   - Simple relationships
   - Mostly accessed via UserProfile

### Controllers Needing IMediator Integration

Current status of controllers:

| Controller | Uses IMediator? | Needs Migration? |
|------------|----------------|------------------|
| `ScenariosController` | ✅ Yes | ✅ Complete |
| `BundlesController` | ❌ No | ⏳ Phase 5 |
| `GameSessionsController` | ❌ No | ⏳ Phase 5 |
| `UserProfilesController` | ❌ No | ⏳ Phase 5 |
| `AccountsController` | ❌ No | ⏳ Phase 5 |
| `MediaController` | ❌ No | ⏳ Phase 5 |
| `UserBadgesController` | ❌ No | ⏳ Phase 5 |
| `BadgeConfigurationsController` | ❌ No | ⏳ Phase 5 |

---

## Recommended Actions

### Immediate (This Session)

1. ✅ Create ADR-0003: Adopt Hexagonal Architecture
2. ✅ Create ADR-0004: Use MediatR for Request/Response Handling
3. ✅ Create ADR-0005: Separate API and Admin API
4. ⏳ Begin Phase 5 CQRS migration for ContentBundle

### Short Term (Next Sprint)

5. Complete Phase 5 CQRS migration:
   - ContentBundle
   - GameSession
   - UserProfile
6. Create ADR-0006: Use Entity Framework Core
7. Create ADR-0007: Infrastructure Modularity Strategy

### Medium Term

8. Complete remaining CQRS migrations
9. Create ADR-0008: Soft Delete Pattern
10. Update all tests for CQRS handlers

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
