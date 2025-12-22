# ADR-0015: Migrate from MediatR to Wolverine

**Status**: 📋 Proposed (Supersedes ADR-0004)

**Date**: 2025-12-22

**Deciders**: Development Team

**Tags**: technology, wolverine, messaging, cqrs, migration

---

## Context

MediatR (ADR-0004) has served well for in-process CQRS, but Mystira's evolution toward a distributed architecture requires messaging capabilities beyond what MediatR offers:

### Current State (MediatR)

| Aspect | MediatR | Limitation |
|--------|---------|------------|
| Messaging | In-process only | No cross-service communication |
| Durability | None | Messages lost on failure |
| Retries | Manual | Complex to implement |
| Transactions | Separate | No outbox pattern |
| Performance | ~2-5ms overhead | Higher than alternatives |

### Target State (Wolverine)

| Aspect | Wolverine | Benefit |
|--------|-----------|---------|
| Messaging | In-process + Distributed | Cross-service events |
| Durability | Outbox/Inbox | Guaranteed delivery |
| Retries | Built-in | Automatic with backoff |
| Transactions | Integrated | Transactional outbox |
| Performance | ~0.5-1ms overhead | 4x faster |

### Key Drivers

1. **Cross-Service Events**: Need to publish events from Mystira.App to Mystira.Chain, Publisher, and StoryGenerator
2. **Message Durability**: Financial transactions (royalties) require guaranteed delivery
3. **Simplified Architecture**: Single framework for in-process and distributed messaging
4. **Performance**: Wolverine's static code generation provides better throughput

---

## Decision

We will **migrate from MediatR to Wolverine** as the unified messaging framework across Mystira.App.

### Implementation Approach

**Phase 1: Infrastructure Setup (Weeks 1-2)**

1. Add Wolverine packages:
   ```xml
   <ItemGroup>
     <PackageReference Include="WolverineFx" Version="5.9.1" />
     <PackageReference Include="WolverineFx.AzureServiceBus" Version="3.9.1" />
     <PackageReference Include="WolverineFx.EntityFrameworkCore" Version="5.2.0" />
   </ItemGroup>
   ```

2. Configure Wolverine (Program.cs):
   ```csharp
   builder.Host.UseWolverine(opts =>
   {
       // In-process message handling (replaces MediatR)
       opts.Policies.AutoApplyTransactions();

       // Azure Service Bus for cross-service
       opts.UseAzureServiceBus(connectionString)
           .AutoProvision()
           .AutoPurgeOnStartup();

       // Durable outbox for reliability
       opts.Durability.Mode = DurabilityMode.Solo;

       // OpenTelemetry integration
       opts.UseOpenTelemetry();
   });
   ```

3. Add envelope tables for durability:
   ```sql
   CREATE TABLE wolverine_incoming_envelopes (
       id UUID PRIMARY KEY,
       status VARCHAR(50),
       owner_id INT,
       execution_time TIMESTAMP,
       attempts INT,
       body BYTEA
   );

   CREATE TABLE wolverine_outgoing_envelopes (
       id UUID PRIMARY KEY,
       owner_id INT,
       destination VARCHAR(500),
       deliver_by TIMESTAMP,
       body BYTEA
   );
   ```

**Phase 2: Handler Migration (Weeks 3-8)**

Before (MediatR):
```csharp
public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountResponse>
{
    public async Task<AccountResponse> Handle(GetAccountQuery request, CancellationToken ct)
    {
        // Implementation
    }
}
```

After (Wolverine):
```csharp
public static class GetAccountQueryHandler
{
    public static async Task<AccountResponse> HandleAsync(
        GetAccountQuery query,
        IAccountRepository repository,
        CancellationToken ct)
    {
        // Implementation
    }
}
```

**Phase 3: Controller Updates**

Before:
```csharp
private readonly ISender _mediator;

[HttpGet("{id}")]
public async Task<ActionResult<AccountResponse>> Get(Guid id)
{
    return await _mediator.Send(new GetAccountQuery(id));
}
```

After:
```csharp
private readonly IMessageBus _bus;

[HttpGet("{id}")]
public async Task<ActionResult<AccountResponse>> Get(Guid id)
{
    return await _bus.InvokeAsync<AccountResponse>(new GetAccountQuery(id));
}
```

**Phase 4: Pipeline Behavior Migration**

Before (MediatR Pipeline):
```csharp
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    // Validation logic
}
```

After (Wolverine Middleware):
```csharp
public class ValidationMiddleware
{
    public async Task<object?> HandleAsync(
        object message,
        IMessageContext context,
        MessageBusDelegate next)
    {
        // Validation logic
        return await next(message, context);
    }
}
```

---

## Consequences

### Positive Consequences

1. **Unified Messaging**
   - Single framework for in-process and distributed messaging
   - Reduced cognitive load for developers
   - Consistent patterns across services

2. **Message Durability**
   - Outbox pattern ensures messages are not lost
   - Automatic retries with exponential backoff
   - Dead letter queue support

3. **Cross-Service Communication**
   - Native Azure Service Bus integration
   - Topic/subscription model for fan-out
   - Correlation ID propagation

4. **Better Performance**
   - Static code generation vs. reflection
   - ~4x improvement in handler invocation
   - Lower memory allocations

5. **Distributed Tracing**
   - Built-in OpenTelemetry support
   - Automatic span creation
   - Correlation across services

### Negative Consequences

1. **Migration Effort**
   - All handlers must be refactored
   - Pipeline behaviors need rewriting
   - Mitigated by: Phased approach over weeks

2. **Learning Curve**
   - Different conventions from MediatR
   - Team needs training
   - Mitigated by: Wolverine docs are excellent

3. **Infrastructure Requirements**
   - Azure Service Bus namespace needed
   - Database tables for envelope storage
   - Mitigated by: Terraform modules available

4. **Complexity Increase**
   - More infrastructure to manage
   - Envelope tables need maintenance
   - Mitigated by: Wolverine handles cleanup

---

## Migration Timeline

| Phase | Focus | Duration |
|-------|-------|----------|
| Phase 1 | Infrastructure Setup | Weeks 1-2 |
| Phase 2 | Domain Events | Weeks 3-4 |
| Phase 3 | Handler Migration | Weeks 5-8 |
| Phase 4 | Cross-Service Events | Weeks 9-10 |
| Phase 5 | MediatR Removal | Weeks 11-12 |

---

## Success Criteria

| Metric | Target | Verification |
|--------|--------|--------------|
| MediatR References | 0 | Code search |
| Handler Conversion | 100% | Audit |
| Cross-Service Events | Operational | Integration tests |
| Message Latency (P95) | < 100ms | Monitoring |
| Message Loss | 0 | Outbox verification |

---

## Related Documents

- [ADR-0004: Use MediatR for CQRS](ADR-0004-use-mediatr-for-cqrs.md) (Superseded)
- [ADR-0001: Adopt CQRS Pattern](ADR-0001-adopt-cqrs-pattern.md)
- [ADR-0013: gRPC for C#/Python Integration](ADR-0013-grpc-for-csharp-python-integration.md)
- [Wolverine Migration Roadmap](../../planning/wolverine-migration-roadmap.md)
- [Hybrid Data Strategy](../../planning/hybrid-data-strategy-roadmap.md)

---

## References

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [Wolverine GitHub](https://github.com/JasperFx/wolverine)
- [Azure Service Bus with Wolverine](https://wolverine.netlify.app/guide/messaging/transports/azure-service-bus/)
- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
