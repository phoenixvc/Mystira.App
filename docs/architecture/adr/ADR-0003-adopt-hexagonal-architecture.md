# ADR-0003: Adopt Hexagonal Architecture (Ports and Adapters)

**Status**: ✅ Accepted

**Date**: 2025-11-24

**Deciders**: Development Team

**Tags**: architecture, hexagonal, ports-and-adapters, clean-architecture, layering

---

## Context

The Mystira.App application initially followed a traditional layered architecture where the Application layer had direct dependencies on Infrastructure concrete implementations. This created several critical problems:

### Problems with Previous Layered Architecture

1. **Tight Coupling to Infrastructure**
   - Application layer directly referenced Infrastructure projects
   - **229 architectural violations** identified
   - **138 direct dependencies** from Application → Infrastructure
   - Hard to swap infrastructure implementations

2. **Testing Difficulties**
   - Could not unit test Application layer without database
   - Required integration tests for simple business logic
   - Mocking concrete infrastructure classes was complex
   - Test execution was slow due to infrastructure dependencies

3. **No Clear Boundaries**
   - Business logic mixed with infrastructure concerns
   - Repository interfaces in wrong layer (Infrastructure.Data)
   - Azure-specific interfaces in Application layer
   - Discord-specific interfaces leaked into business logic

4. **Limited Flexibility**
   - Impossible to swap Azure for AWS without changing Application code
   - Could not swap Discord for Slack without refactoring use cases
   - Infrastructure changes rippled through Application layer
   - Vendor lock-in to Azure and Discord

5. **Difficult Onboarding**
   - New developers struggled to understand layer boundaries
   - No clear separation between "what" (business logic) and "how" (infrastructure)
   - Architectural rules not enforced

### Considered Alternatives

1. **Continue with Layered Architecture**
   - ✅ Familiar to most developers
   - ✅ Simple to understand initially
   - ❌ Tight coupling continues
   - ❌ Testing remains difficult
   - ❌ No infrastructure swappability
   - ❌ Architectural violations accumulate

2. **Onion Architecture**
   - ✅ Similar to Hexagonal (dependency inversion)
   - ✅ Domain-centric
   - ✅ Testable
   - ⚠️ More complex than needed for current project
   - ⚠️ Additional layers (Domain Services, Application Services)

3. **Clean Architecture (Uncle Bob)**
   - ✅ Clear separation of concerns
   - ✅ Testable and flexible
   - ✅ Industry-proven
   - ⚠️ Very similar to Hexagonal Architecture
   - ⚠️ May be over-engineered for some projects

4. **Hexagonal Architecture (Ports and Adapters)** ⭐ **CHOSEN**
   - ✅ Clear separation: Core (business logic) vs Infrastructure
   - ✅ Dependency inversion: Infrastructure depends on Core
   - ✅ Testable: Core has no infrastructure dependencies
   - ✅ Flexible: Easy to swap infrastructure adapters
   - ✅ Simple mental model: Ports (interfaces) and Adapters (implementations)
   - ✅ Works well with CQRS and Domain-Driven Design
   - ⚠️ More classes/interfaces to maintain
   - ⚠️ Learning curve for team

---

## Decision

We will adopt **Hexagonal Architecture** (also known as Ports and Adapters) to decouple the business logic from infrastructure concerns.

### Core Principles

1. **Application layer is the center** ("hexagon")
   - Contains all business logic
   - Defines ports (interfaces) for external dependencies
   - Has ZERO dependencies on Infrastructure layer

2. **Ports = Interfaces** (defined in Application layer)
   - Input ports: Commands, Queries (CQRS)
   - Output ports: Repositories, External Services (IBlobService, IMessagingService)

3. **Adapters = Implementations** (in Infrastructure layer)
   - Input adapters: Controllers (API/Admin.Api)
   - Output adapters: Repositories, AzureBlobService, DiscordBotService

4. **Dependency Direction**
   ```
   Infrastructure → Application ← API
   (Adapters depend on Ports)
   ```

### Implementation Strategy

**Phase 1: Repository Interface Migration** ✅ Complete
- Move all repository interfaces from Infrastructure.Data to Application/Ports/Data
- Create Application/Ports/Data/ directory structure
- 27 repository interfaces migrated
- All Infrastructure implementations updated

**Phase 2: Azure & Discord Port Interfaces** ✅ Complete
- Create infrastructure-agnostic port interfaces:
  - `IAzureBlobService` → `IBlobService` (storage-agnostic)
  - `IDiscordBotService` → `IMessagingService` (platform-agnostic)
  - `IAudioTranscodingService` (tool-agnostic)
- Update Infrastructure adapters to implement new ports
- Update Application use cases to depend on ports

**Phase 3: Fix Remaining Application → Infrastructure Dependencies** ✅ Complete
- Remove direct Infrastructure references from Application
- Create ports for all infrastructure services
- Update DI registrations

**Phase 4: Update API/Admin.Api Services** ✅ Complete
- Remove Infrastructure namespace imports from controllers
- Inject only Application layer interfaces
- **47 API services** cleaned
- **14 Admin.Api services** cleaned

**Phase 5: Validation and Documentation** ✅ Complete
- Verify zero Application → Infrastructure dependencies
- Document architectural rules
- Create [ARCHITECTURAL_RULES.md](../ARCHITECTURAL_RULES.md)

---

## Consequences

### Positive Consequences ✅

1. **Complete Testability**
   - Application layer has zero infrastructure dependencies
   - Can unit test all business logic with mocks
   - **100% testable** Application layer
   - Fast test execution (no database, no external services)

2. **Infrastructure Swappability**
   - Can swap Azure Blob Storage for AWS S3 without changing Application code
   - Can swap Discord for Slack by implementing `IMessagingService` adapter
   - Can use in-memory repositories for testing
   - Future-proof: not locked to any vendor

3. **Clear Architectural Boundaries**
   - **Zero architectural violations** (was 229)
   - Ports (interfaces) clearly defined in Application layer
   - Adapters (implementations) in Infrastructure layer
   - Easy to understand and enforce

4. **Better Separation of Concerns**
   - Business logic completely isolated from infrastructure
   - Each adapter focused on single responsibility
   - Easier to reason about code

5. **Improved Onboarding**
   - Clear mental model: Core (business) vs Infrastructure (technical)
   - Documented architectural rules
   - Easier for new developers to understand codebase

6. **Supports CQRS and DDD**
   - Hexagonal Architecture pairs well with CQRS
   - Domain layer protected from infrastructure concerns
   - Use cases (Commands/Queries) are ports

### Negative Consequences ❌

1. **Increased Number of Classes/Interfaces**
   - **164 files refactored** in migration
   - More interfaces to maintain (ports)
   - More adapter classes
   - Mitigated by: Clear naming conventions, folder structure

2. **Learning Curve**
   - Team must learn Hexagonal Architecture concepts
   - Understanding ports vs adapters takes time
   - Mitigated by: Comprehensive documentation, examples, training

3. **Initial Development Overhead**
   - Creating ports for every external dependency
   - Writing adapters for each implementation
   - Mitigated by: One-time investment, long-term benefits

4. **Indirection**
   - More layers of abstraction
   - May make debugging harder initially
   - Mitigated by: Clear naming, documentation, logging

5. **Over-Engineering Risk**
   - Temptation to create too many ports
   - May be overkill for simple CRUD operations
   - Mitigated by: Pragmatic approach, use only when needed

---

## Implementation Details

### Project Structure

```
Mystira.App/
├── src/
│   ├── Mystira.App.Domain/              # Domain models, entities
│   │   └── Models/
│   │
│   ├── Mystira.App.Application/         # Core business logic (Hexagon)
│   │   ├── Ports/                       # Interfaces (Ports)
│   │   │   ├── Data/                    # Repository interfaces
│   │   │   │   ├── IRepository<T>.cs
│   │   │   │   ├── IScenarioRepository.cs
│   │   │   │   ├── IContentBundleRepository.cs
│   │   │   │   └── IUnitOfWork.cs
│   │   │   ├── Storage/                 # Storage interfaces
│   │   │   │   └── IBlobService.cs
│   │   │   ├── Messaging/               # Messaging interfaces
│   │   │   │   └── IMessagingService.cs
│   │   │   └── Media/                   # Media interfaces
│   │   │       └── IAudioTranscodingService.cs
│   │   ├── CQRS/                        # Commands & Queries (Input Ports)
│   │   │   └── Scenarios/
│   │   │       ├── Commands/
│   │   │       └── Queries/
│   │   └── UseCases/                    # Business logic orchestration
│   │
│   ├── Mystira.App.Infrastructure.Data/  # Data adapter
│   │   ├── Repositories/                 # Repository implementations
│   │   │   ├── Repository<T>.cs
│   │   │   ├── ScenarioRepository.cs
│   │   │   └── ContentBundleRepository.cs
│   │   └── UnitOfWork/
│   │       └── UnitOfWork.cs
│   │
│   ├── Mystira.App.Infrastructure.Azure/  # Azure adapter
│   │   └── AzureBlobService.cs           # Implements IBlobService
│   │
│   ├── Mystira.App.Infrastructure.Discord/  # Discord adapter
│   │   └── DiscordBotService.cs            # Implements IMessagingService
│   │
│   ├── Mystira.App.Api/                  # Input adapter (user-facing)
│   │   └── Controllers/                  # API controllers
│   │
│   └── Mystira.App.Admin.Api/            # Input adapter (admin)
│       └── Controllers/                  # Admin controllers
```

### Naming Conventions

**Ports (Interfaces)**:
- Prefix with `I`: `IBlobService`, `IMessagingService`
- Platform-agnostic names (not `IAzureBlobService`)
- Describe "what" not "how"

**Adapters (Implementations)**:
- Specific names: `AzureBlobService`, `DiscordBotService`
- Describe "how" and "with what technology"
- Live in Infrastructure projects

### Dependency Rules

```
✅ ALLOWED:
Infrastructure → Application
API → Application
Admin.Api → Application
Application → Domain

❌ FORBIDDEN:
Application → Infrastructure
Application → API
Application → Admin.Api
Domain → Application
Domain → Infrastructure
```

---

## Migration Results

### Before (Layered Architecture)

| Metric | Count |
|--------|-------|
| Application → Infrastructure dependencies | 138 |
| API services with Infrastructure imports | 47 |
| Admin.Api services with Infrastructure imports | 14 |
| Repository interfaces in wrong layer | 27 |
| Infrastructure-specific interfaces in Application | 3 |
| **Total architectural violations** | **229** |

### After (Hexagonal Architecture)

| Metric | Count |
|--------|-------|
| Application → Infrastructure dependencies | **0** ✅ |
| API services with Infrastructure imports | **0** ✅ |
| Admin.Api services with Infrastructure imports | **0** ✅ |
| Repository interfaces in wrong layer | **0** ✅ |
| Infrastructure-specific interfaces in Application | **0** ✅ |
| **Total architectural violations** | **0** ✅ |

### Files Refactored

- **164 files** changed across 5 phases
- **27 repository interfaces** moved to Application layer
- **3 infrastructure services** made platform-agnostic
- **61 controllers** cleaned of Infrastructure references

---

## Enforcement

### Automated Checks

1. **Dependency Analysis**
   - Use NDepend or similar to detect Application → Infrastructure references
   - Fail CI/CD builds on violations

2. **Namespace Rules**
   - No `using Mystira.App.Infrastructure.*` in Application layer
   - Enforce via code review and linters

### Code Review Checklist

- [ ] New interfaces go in Application/Ports
- [ ] New implementations go in Infrastructure projects
- [ ] Application layer has no Infrastructure references
- [ ] Controllers only inject Application interfaces
- [ ] Repository interfaces in Application/Ports/Data

---

## Related Decisions

- **ADR-0001**: Adopt CQRS Pattern (Commands/Queries are input ports)
- **ADR-0002**: Adopt Specification Pattern (used in repositories)
- **ADR-0005**: Separate API and Admin API (both are input adapters)
- **ADR-0006**: Use Entity Framework Core (Repository adapter implementation)

---

## References

- [Hexagonal Architecture - Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)
- [Ports and Adapters Pattern](https://herbertograca.com/2017/11/16/explicit-architecture-01-ddd-hexagonal-onion-clean-cqrs-how-i-put-it-all-together/)
- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [HEXAGONAL_ARCHITECTURE_REFACTORING_SUMMARY.md](../HEXAGONAL_ARCHITECTURE_REFACTORING_SUMMARY.md) - Internal refactoring documentation
- [ARCHITECTURAL_RULES.md](../ARCHITECTURAL_RULES.md) - Enforcement rules

---

## Notes

- This ADR documents the hexagonal architecture refactoring completed in November 2025
- Migration was completed in 5 phases over multiple commits
- All 229 architectural violations have been resolved
- Architecture is now enforced via code review and documentation

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
