# ADR-0001: Adopt CQRS Pattern

**Status**: ✅ Accepted

**Date**: 2025-11-24

**Deciders**: Development Team

**Tags**: architecture, cqrs, patterns, application-layer

---

## Context

The Mystira.App application has been evolving from a simple CRUD-based architecture to a more complex domain-driven system with sophisticated business logic. As the application grew, we identified several pain points:

### Problems with Previous Approach

1. **Mixed Concerns in Services**
   - Services were handling both read and write operations with different requirements
   - Read operations needed performance optimizations (caching, projections)
   - Write operations needed validation, business logic, and transaction management
   - Same service methods tried to serve both needs, leading to compromises

2. **Difficult to Optimize**
   - Read operations were constrained by write operation requirements
   - Write operations were burdened with read-specific logic
   - Hard to cache read results when mixed with write operations
   - Performance tuning affected both reads and writes

3. **Testing Complexity**
   - Tests mixed read and write concerns
   - Mocking became increasingly complex
   - Hard to test query logic independently from business logic

4. **Scalability Concerns**
   - Read and write operations have different scaling characteristics
   - Impossible to scale reads independently from writes
   - Database optimization strategies conflicted (normalization vs. denormalization)

5. **Code Duplication**
   - Similar read patterns repeated across different services
   - Query logic scattered throughout the codebase
   - No reusable query abstractions

### Considered Alternatives

1. **Continue with Traditional Layered Architecture**
   - ✅ Familiar to developers
   - ✅ Simple to understand
   - ❌ Doesn't solve performance issues
   - ❌ Doesn't separate read/write concerns
   - ❌ Limited scalability

2. **Event Sourcing + CQRS**
   - ✅ Complete audit trail
   - ✅ Full separation of concerns
   - ✅ Event-driven architecture
   - ❌ Too complex for current needs
   - ❌ Significant learning curve
   - ❌ Infrastructure overhead (event store)
   - ❌ Eventual consistency challenges

3. **CQRS with MediatR** ⭐ **CHOSEN**
   - ✅ Clear separation of read/write operations
   - ✅ Optimizable independently
   - ✅ Testable in isolation
   - ✅ Reusable query patterns
   - ✅ Incremental adoption
   - ✅ Minimal infrastructure changes
   - ✅ Strong .NET ecosystem support
   - ⚠️ Learning curve for team
   - ⚠️ More classes to maintain

4. **Repository Pattern with Specifications (without CQRS)**
   - ✅ Reusable query logic
   - ✅ Testable
   - ❌ Doesn't separate commands from queries
   - ❌ Still mixed in services
   - ❌ Limited optimization opportunities

---

## Decision

We will adopt the **CQRS (Command Query Responsibility Segregation) pattern** using **MediatR** for request/response handling, combined with the **Specification Pattern** for complex queries.

### Implementation Strategy

1. **Commands (Write Operations)**
   - Represented as immutable `record` types
   - Handled by dedicated `ICommandHandler<TCommand, TResult>` implementations
   - Use `IUnitOfWork` for transactional consistency
   - Include validation and business logic
   - Return created/updated entities or void

2. **Queries (Read Operations)**
   - Represented as immutable `record` types
   - Handled by dedicated `IQueryHandler<TQuery, TResult>` implementations
   - Use `IRepository` for data access
   - Use Specification Pattern for complex filtering/sorting/paging
   - Never modify state
   - Return entities, DTOs, or collections

3. **Specifications (Query Logic Encapsulation)**
   - Inherit from `BaseSpecification<T>`
   - Encapsulate filtering, sorting, paging, eager loading
   - Reusable across multiple queries
   - Testable in isolation

4. **MediatR Integration**
   - Controllers inject `IMediator` instead of individual handlers
   - `IMediator.Send()` routes requests to appropriate handlers
   - Pipeline behaviors for cross-cutting concerns (logging, validation)
   - Auto-discovery of handlers via assembly scanning

### Phased Rollout

**Phase 1: Foundation** ✅ Complete
- Install MediatR (v12.4.1)
- Create `ICommand<T>`, `IQuery<T>` marker interfaces
- Create `ICommandHandler<TCommand, TResponse>` interface
- Create `IQueryHandler<TQuery, TResponse>` interface
- Register MediatR in DI container

**Phase 2: Specification Pattern** ✅ Complete
- Create `ISpecification<T>` interface
- Create `BaseSpecification<T>` implementation
- Create `SpecificationEvaluator<T>` for EF Core
- Update repositories to support specifications
- Document Specification Pattern

**Phase 3: Pilot Implementation** ✅ Complete
- Migrate Scenario operations to CQRS
- Create example commands: `CreateScenarioCommand`, `UpdateScenarioCommand`, `DeleteScenarioCommand`
- Create example queries: `GetScenarioQuery`, `GetScenariosQuery`
- Create specifications: `ScenarioByIdSpecification`, `ScenariosWithContentBundleSpecification`
- Gather feedback from team

**Phase 4: Documentation** ✅ Complete
- Create [CQRS_PATTERN.md](../patterns/CQRS_PATTERN.md)
- Create [SPECIFICATION_PATTERN.md](../patterns/SPECIFICATION_PATTERN.md)
- Create [QUICKSTART_COMMAND.md](../patterns/QUICKSTART_COMMAND.md)
- Create [QUICKSTART_QUERY.md](../patterns/QUICKSTART_QUERY.md)
- Create [QUICKSTART_SPECIFICATION.md](../patterns/QUICKSTART_SPECIFICATION.md)
- Update Application layer README

**Phase 5: Full Migration** 🔄 In Progress
- Migrate remaining entities (ContentBundles, Purchases, Users, etc.)
- Update all controllers to use `IMediator`
- Create specifications for common queries
- Update all tests

---

## Consequences

### Positive Consequences ✅

1. **Clear Separation of Concerns**
   - Read operations (queries) are completely separate from write operations (commands)
   - Each operation has a single responsibility
   - Easier to reason about code flow

2. **Performance Optimization**
   - Queries can be optimized independently (caching, projections, read replicas)
   - Commands can focus on business logic and validation
   - Specifications enable query plan reuse

3. **Improved Testability**
   - Commands and queries can be tested in isolation
   - Specifications can be unit tested without database
   - Mock dependencies are simpler (Repository, UnitOfWork, Logger)

4. **Reusable Query Logic**
   - Specifications encapsulate complex query patterns
   - Same specification can be used across multiple queries
   - Reduces code duplication

5. **Better Scalability**
   - Read and write paths can be scaled independently
   - Future option: Separate read/write databases
   - Supports caching strategies

6. **Explicit API Contracts**
   - Commands and queries are strongly typed
   - Clear input/output contracts
   - Self-documenting code

7. **Decoupled Controllers**
   - Controllers only depend on `IMediator`
   - No direct dependencies on handlers
   - Easier to refactor handlers without changing controllers

8. **Pipeline Behaviors**
   - Cross-cutting concerns (logging, validation, caching) can be added via MediatR pipeline
   - Applied consistently across all commands/queries
   - Reduces boilerplate code

### Negative Consequences ❌

1. **Increased Number of Classes**
   - Each operation requires: Command/Query + Handler
   - More files to navigate
   - Mitigated by: Clear folder structure, naming conventions

2. **Learning Curve**
   - Team must learn CQRS concepts
   - Team must learn MediatR library
   - Team must learn Specification Pattern
   - Mitigated by: Comprehensive documentation, quick-start guides, examples

3. **Potential Over-Engineering**
   - Simple CRUD operations now require more classes
   - Risk of creating too many specifications
   - Mitigated by: Use CQRS for complex operations, allow simpler approaches for trivial cases

4. **Initial Development Overhead**
   - Setting up CQRS infrastructure takes time
   - Creating first commands/queries/specs takes longer
   - Mitigated by: Templates, code snippets, thorough documentation

5. **Consistency Challenges**
   - Must ensure all developers follow patterns correctly
   - Risk of inconsistent implementations
   - Mitigated by: Code reviews, documentation, examples, linting rules

6. **Testing Complexity (Initial)**
   - New testing patterns to learn
   - More mocking required
   - Mitigated by: Test examples in quick-start guides, shared test utilities

---

## Implementation Details

### Technology Stack

- **MediatR** (v12.4.1) - Request/response mediator
- **Entity Framework Core** - ORM for data access
- **Specification Pattern** - Custom implementation in Domain layer
- **Dependency Injection** - .NET built-in DI container

### Project Structure

```
src/Mystira.App.Application/
├── CQRS/
│   ├── Scenarios/
│   │   ├── Commands/
│   │   │   ├── CreateScenarioCommand.cs
│   │   │   ├── CreateScenarioCommandHandler.cs
│   │   │   ├── UpdateScenarioCommand.cs
│   │   │   ├── UpdateScenarioCommandHandler.cs
│   │   │   └── DeleteScenarioCommand.cs
│   │   │   └── DeleteScenarioCommandHandler.cs
│   │   └── Queries/
│   │       ├── GetScenarioQuery.cs
│   │       ├── GetScenarioQueryHandler.cs
│   │       ├── GetScenariosQuery.cs
│   │       └── GetScenariosQueryHandler.cs
│   ├── ContentBundles/
│   │   ├── Commands/
│   │   └── Queries/
│   └── ... (other aggregates)
├── Interfaces/
│   ├── ICommand.cs
│   ├── IQuery.cs
│   ├── ICommandHandler.cs
│   └── IQueryHandler.cs
└── README.md

src/Mystira.App.Domain/
├── Specifications/
│   ├── ISpecification.cs
│   ├── BaseSpecification.cs
│   ├── ScenarioSpecifications.cs
│   └── ContentBundleSpecifications.cs
└── README.md

src/Mystira.App.Infrastructure.Data/
├── Specifications/
│   └── SpecificationEvaluator.cs
└── README.md
```

### Naming Conventions

**Commands**:
- Verb-based names: `CreateScenarioCommand`, `UpdateUserCommand`
- End with `Command`
- Use `record` type for immutability

**Queries**:
- Noun-based names: `GetScenarioQuery`, `GetScenariosQuery`
- Start with `Get`
- End with `Query`
- Use `record` type for immutability

**Handlers**:
- Named after command/query: `CreateScenarioCommandHandler`
- End with `Handler`
- Implement `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>`

**Specifications**:
- Descriptive names: `ScenarioByIdSpecification`, `ActiveContentBundlesWithScenariosSpecification`
- End with `Specification`
- Inherit from `BaseSpecification<T>`

---

## Metrics for Success

After full implementation, we expect to see:

1. **Code Quality**
   - ✅ Reduced cyclomatic complexity in services
   - ✅ Higher test coverage (isolated testing)
   - ✅ Reduced code duplication (reusable specifications)

2. **Performance**
   - ✅ Faster query response times (optimized read path)
   - ✅ Reduced database roundtrips (specifications with includes)
   - ✅ Improved caching effectiveness

3. **Developer Experience**
   - ✅ Faster onboarding for new developers (clear patterns)
   - ✅ Easier to locate code (organized by operation type)
   - ✅ Clearer code intent (explicit commands/queries)

4. **Maintainability**
   - ✅ Easier to add new features (follow existing pattern)
   - ✅ Easier to modify existing features (isolated handlers)
   - ✅ Reduced regression bugs (better test isolation)

---

## Related Decisions

- **ADR-0002**: Adopt Specification Pattern (companion to CQRS for query logic)
- **Future ADR**: Consider Event Sourcing (if audit requirements become critical)
- **Future ADR**: Consider separate read/write databases (if scale requires it)

---

## References

- [CQRS Pattern - Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Library](https://github.com/jbogard/MediatR)
- [Command Query Separation - Martin Fowler](https://martinfowler.com/bliki/CommandQuerySeparation.html)
- [Specification Pattern - Martin Fowler](https://martinfowler.com/apsupp/spec.pdf)
- [CQRS_PATTERN.md](../patterns/CQRS_PATTERN.md) - Internal documentation
- [QUICKSTART_COMMAND.md](../patterns/QUICKSTART_COMMAND.md) - Developer guide
- [QUICKSTART_QUERY.md](../patterns/QUICKSTART_QUERY.md) - Developer guide
- [QUICKSTART_SPECIFICATION.md](../patterns/QUICKSTART_SPECIFICATION.md) - Developer guide

---

## Notes

- This ADR documents the decision made in November 2025 to adopt CQRS
- Implementation is already in progress with Scenario entity as pilot
- Team training is ongoing through documentation and code reviews
- Pattern will be evaluated after 3 months of use

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
