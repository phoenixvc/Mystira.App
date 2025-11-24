# ADR-0004: Use MediatR for Request/Response Handling

**Status**: ✅ Accepted

**Date**: 2025-11-24

**Deciders**: Development Team

**Tags**: technology, mediatr, cqrs, request-response, library

---

## Context

After adopting CQRS (ADR-0001), we needed a mechanism to route Commands and Queries to their respective handlers. We required:

1. **Decoupling**: Controllers should not depend directly on handler implementations
2. **Discoverability**: Handlers should be auto-discovered without manual registration
3. **Pipeline Behaviors**: Cross-cutting concerns (logging, validation, caching) should be easy to add
4. **Type Safety**: Compile-time validation of request/response types

###

 Considered Alternatives

1. **Manual Handler Registration**
   - ✅ No third-party dependencies
   - ✅ Full control over routing
   - ❌ Must manually register every handler in DI
   - ❌ No auto-discovery
   - ❌ No pipeline behavior support
   - ❌ Lots of boilerplate code

2. **Custom Mediator Implementation**
   - ✅ Tailored to project needs
   - ✅ No external dependencies
   - ❌ Must implement from scratch
   - ❌ Reinventing the wheel
   - ❌ Maintenance burden
   - ❌ No community support

3. **MediatR Library** ⭐ **CHOSEN**
   - ✅ Battle-tested, industry-standard
   - ✅ Auto-discovery of handlers via assembly scanning
   - ✅ Pipeline behaviors built-in
   - ✅ Strong typing with generics
   - ✅ Large community, good documentation
   - ✅ Works seamlessly with .NET DI
   - ❌ External dependency
   - ❌ Slight learning curve

---

## Decision

We will use **MediatR** (v12.4.1) as the mediator library for routing Commands and Queries to their handlers.

### Implementation Approach

1. **Install MediatR** via NuGet:
   ```bash
   dotnet add package MediatR
   ```

2. **Register in DI** (Program.cs):
   ```csharp
   builder.Services.AddMediatR(cfg => {
       cfg.RegisterServicesFromAssembly(typeof(CreateScenarioCommand).Assembly);
   });
   ```

3. **Define marker interfaces** (Application layer):
   ```csharp
   public interface ICommand<T> : IRequest<T> { }
   public interface IQuery<T> : IRequest<T> { }
   ```

4. **Commands and Queries** implement marker interfaces:
   ```csharp
   public record CreateScenarioCommand(CreateScenarioRequest Request) : ICommand<Scenario>;
   ```

5. **Handlers** implement `IRequestHandler<TRequest, TResponse>`:
   ```csharp
   public class CreateScenarioCommandHandler : IRequestHandler<CreateScenarioCommand, Scenario>
   {
       public async Task<Scenario> Handle(CreateScenarioCommand request, CancellationToken cancellationToken)
       {
           // Implementation
       }
   }
   ```

6. **Controllers** inject `IMediator`:
   ```csharp
   public class ScenariosController : ControllerBase
   {
       private readonly IMediator _mediator;

       [HttpPost]
       public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
       {
           var command = new CreateScenarioCommand(request);
           var scenario = await _mediator.Send(command);
           return CreatedAtAction(nameof(Get), new { id = scenario.Id }, scenario);
       }
   }
   ```

---

## Consequences

### Positive Consequences ✅

1. **Decoupled Controllers**
   - Controllers only depend on `IMediator` interface
   - No direct dependencies on handler implementations
   - Easy to refactor handlers without changing controllers

2. **Auto-Discovery**
   - Handlers automatically registered via assembly scanning
   - No manual DI registration for each handler
   - Reduces boilerplate code

3. **Pipeline Behaviors**
   - Cross-cutting concerns easily added:
     - Logging: Log all commands/queries
     - Validation: Validate requests before handlers
     - Caching: Cache query results
     - Performance monitoring: Track handler execution time
   - Example pipeline behavior:
     ```csharp
     public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
     {
         public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
         {
             _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
             var response = await next();
             _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
             return response;
         }
     }
     ```

4. **Type Safety**
   - Compile-time validation of request/response types
   - `IRequest<T>` ensures correct return type
   - No runtime type errors

5. **Testability**
   - Easy to test handlers in isolation
   - Mock `IMediator` in controller tests
   - Clear separation of concerns

6. **Community Support**
   - Large community, extensive documentation
   - Well-maintained library
   - Frequent updates and bug fixes

### Negative Consequences ❌

1. **External Dependency**
   - Project depends on MediatR library
   - Must keep library up-to-date
   - Mitigated by: MediatR is stable, widely used

2. **Indirection**
   - Extra layer between controller and handler
   - May make debugging harder initially
   - Mitigated by: Clear naming conventions, logging

3. **Learning Curve**
   - Team must learn MediatR concepts
   - Understanding pipeline behaviors takes time
   - Mitigated by: Good documentation, examples

4. **Performance Overhead**
   - Slight overhead from mediator routing
   - Negligible in most scenarios
   - Mitigated by: Benefits outweigh minimal overhead

---

## Implementation Details

### Version

- **MediatR**: v12.4.1

### Registration

```csharp
// Program.cs
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateScenarioCommand).Assembly);
});
```

### Marker Interfaces

```csharp
// Application/Interfaces/ICommand.cs
public interface ICommand<T> : IRequest<T> { }

// Application/Interfaces/IQuery.cs
public interface IQuery<T> : IRequest<T> { }
```

### Handler Interfaces

```csharp
// Application/Interfaces/ICommandHandler.cs
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{ }

// Application/Interfaces/IQueryHandler.cs
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{ }
```

---

## Related Decisions

- **ADR-0001**: Adopt CQRS Pattern (MediatR routes Commands and Queries)
- **ADR-0003**: Adopt Hexagonal Architecture (MediatR is an implementation detail)

---

## References

- [MediatR GitHub](https://github.com/jbogard/MediatR)
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
