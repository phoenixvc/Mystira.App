# Wolverine Migration Roadmap

**Status**: Planned
**Last Updated**: 2025-12-22
**Owner**: Development Team
**Source**: [Mystira.workspace ADR-0015](https://github.com/phoenixvc/Mystira.workspace)

---

## Executive Summary

This roadmap establishes a systematic approach to replacing MediatR with Wolverine as the unified messaging framework across the Mystira platform, enabling durable messaging and cross-service event-driven communication.

---

## Why Wolverine?

### Current State (MediatR)

| Aspect | MediatR | Limitation |
|--------|---------|------------|
| Messaging | In-process only | No cross-service communication |
| Durability | None | Messages lost on failure |
| Retries | Manual | Complex to implement |
| Transactions | Separate | No outbox pattern |

### Target State (Wolverine)

| Aspect | Wolverine | Benefit |
|--------|-----------|---------|
| Messaging | In-process + Distributed | Cross-service events |
| Durability | Outbox/Inbox | Guaranteed delivery |
| Retries | Built-in | Automatic with backoff |
| Transactions | Integrated | Transactional outbox |

---

## Phase Summary

| Phase | Focus | Duration |
|-------|-------|----------|
| Phase 1 | Infrastructure Setup | Weeks 1-2 |
| Phase 2 | Domain Events | Weeks 3-4 |
| Phase 3 | Handler Migration | Weeks 5-8 |
| Phase 4 | Cross-Service Events | Weeks 9-10 |
| Phase 5 | MediatR Removal | Weeks 11-12 |

---

## Phase 1: Infrastructure Setup (Weeks 1-2)

### 1.1 Azure Service Bus Provisioning

```hcl
# Terraform configuration
resource "azurerm_servicebus_namespace" "mystira" {
  name                = "mys-${var.environment}-events-san"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
}

resource "azurerm_servicebus_topic" "domain_events" {
  name         = "domain-events"
  namespace_id = azurerm_servicebus_namespace.mystira.id
}
```

### 1.2 NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="WolverineFx" Version="2.0.0" />
  <PackageReference Include="WolverineFx.AzureServiceBus" Version="2.0.0" />
  <PackageReference Include="WolverineFx.EntityFrameworkCore" Version="2.0.0" />
</ItemGroup>
```

### 1.3 Wolverine Configuration

```csharp
// Program.cs
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

### 1.4 Database Migrations

```sql
-- Wolverine envelope tables
CREATE TABLE wolverine_incoming_envelopes (
    id UUID PRIMARY KEY,
    status VARCHAR(50),
    owner_id INT,
    execution_time TIMESTAMP,
    attempts INT,
    body BYTEA,
    ...
);

CREATE TABLE wolverine_outgoing_envelopes (
    id UUID PRIMARY KEY,
    owner_id INT,
    destination VARCHAR(500),
    deliver_by TIMESTAMP,
    body BYTEA,
    ...
);
```

### Deliverables

- [ ] Azure Service Bus provisioned
- [ ] Wolverine packages added
- [ ] Basic configuration in place
- [ ] Envelope tables created

---

## Phase 2: Domain Events (Weeks 3-4)

### 2.1 Event Definitions

```csharp
namespace Mystira.App.Domain.Events;

// Account lifecycle events
public record AccountCreatedEvent(
    Guid AccountId,
    string Email,
    DateTime CreatedAt);

public record AccountUpdatedEvent(
    Guid AccountId,
    string Email,
    DateTime UpdatedAt);

public record AccountDeletedEvent(
    Guid AccountId,
    DateTime DeletedAt);

// Session events
public record SessionStartedEvent(
    Guid SessionId,
    Guid AccountId,
    Guid ScenarioId,
    DateTime StartedAt);

public record SessionCompletedEvent(
    Guid SessionId,
    Guid AccountId,
    int Score,
    TimeSpan Duration);

// Royalty events
public record RoyaltyPaidEvent(
    string IpAssetId,
    decimal Amount,
    string TransactionHash,
    DateTime PaidAt);

public record RoyaltyClaimedEvent(
    string IpAssetId,
    string WalletAddress,
    decimal Amount,
    string TransactionHash);
```

### 2.2 Event Handlers

```csharp
namespace Mystira.App.Application.EventHandlers;

public class AccountEventHandlers
{
    // Analytics tracking
    public async Task Handle(AccountCreatedEvent e, ILogger<AccountEventHandlers> logger)
    {
        logger.LogInformation("Account created: {AccountId}", e.AccountId);
        // Track in analytics
    }

    // Cache invalidation
    public async Task Handle(AccountUpdatedEvent e, ICacheService cache)
    {
        await cache.InvalidateAsync($"account:{e.AccountId}");
    }
}

public class SessionEventHandlers
{
    public async Task Handle(SessionCompletedEvent e, IAnalyticsService analytics)
    {
        await analytics.TrackSessionAsync(e.SessionId, e.AccountId, e.Score);
    }
}
```

### Deliverables

- [ ] All domain events defined
- [ ] Event handlers for analytics
- [ ] Event handlers for cache invalidation
- [ ] Unit tests for handlers

---

## Phase 3: Handler Migration (Weeks 5-8)

### 3.1 Migration Order

| Week | Handler Type | Examples |
|------|--------------|----------|
| 5 | Simple Queries | GetAccountQuery, GetScenarioQuery |
| 6 | Basic Commands | CreateAccountCommand, UpdateProfileCommand |
| 7 | Complex Queries | GetSessionHistoryQuery, GetLeaderboardQuery |
| 8 | Complex Commands | ProcessPaymentCommand, RegisterIpAssetCommand |

### 3.2 Migration Pattern

**Before (MediatR):**
```csharp
public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountResponse>
{
    public async Task<AccountResponse> Handle(GetAccountQuery request, CancellationToken ct)
    {
        // Implementation
    }
}
```

**After (Wolverine):**
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

### 3.3 Controller Updates

**Before:**
```csharp
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly ISender _mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountResponse>> Get(Guid id)
    {
        return await _mediator.Send(new GetAccountQuery(id));
    }
}
```

**After:**
```csharp
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly IMessageBus _bus;

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountResponse>> Get(Guid id)
    {
        return await _bus.InvokeAsync<AccountResponse>(new GetAccountQuery(id));
    }
}
```

### Deliverables

- [ ] All queries migrated
- [ ] All commands migrated
- [ ] Controllers updated
- [ ] Integration tests passing

---

## Phase 4: Cross-Service Events (Weeks 9-10)

### 4.1 Service Bus Topics

```csharp
// Configure topics and subscriptions
opts.UseAzureServiceBus(connectionString)
    .AutoProvision()
    // Publish events to topics
    .ConfigureTopicPublishing("mystira.app.events")
    // Subscribe to other services
    .ConfigureSubscription("mystira.chain.events", sub =>
    {
        sub.ProcessingRules.Add<IpAssetRegisteredEvent>();
    })
    .ConfigureSubscription("mystira.publisher.events", sub =>
    {
        sub.ProcessingRules.Add<ContentPurchasedEvent>();
    });
```

### 4.2 Cross-Service Events

```csharp
// Events from Mystira.Chain
public record IpAssetRegisteredEvent(
    string ContentId,
    string IpAssetId,
    string TransactionHash);

// Events from Mystira.Publisher
public record ContentPurchasedEvent(
    string ContentId,
    Guid BuyerAccountId,
    decimal Amount);

// Handlers in Mystira.App
public class CrossServiceEventHandlers
{
    public async Task Handle(
        IpAssetRegisteredEvent e,
        IContentRepository repository)
    {
        await repository.UpdateIpAssetIdAsync(e.ContentId, e.IpAssetId);
    }

    public async Task Handle(
        ContentPurchasedEvent e,
        IRoyaltyService royaltyService)
    {
        await royaltyService.ProcessPurchaseAsync(e.ContentId, e.Amount);
    }
}
```

### 4.3 Idempotency & Correlation

```csharp
// Idempotency via message deduplication
opts.Durability.DeduplicationWindow = TimeSpan.FromMinutes(5);

// Correlation tracking
public async Task Handle(
    SessionCompletedEvent e,
    IMessageContext context)
{
    context.CorrelationId = e.SessionId.ToString();
    // Events published will carry correlation
}
```

### Deliverables

- [ ] Service Bus topics configured
- [ ] Cross-service handlers implemented
- [ ] Idempotency patterns in place
- [ ] Distributed tracing working

---

## Phase 5: MediatR Removal (Weeks 11-12)

### 5.1 Cleanup Tasks

- [ ] Remove `MediatR` package reference
- [ ] Remove `IRequest<T>` interfaces from queries/commands
- [ ] Remove `IRequestHandler<T>` interfaces from handlers
- [ ] Delete `ISender` injection from controllers
- [ ] Remove pipeline behaviors

### 5.2 Pipeline Behavior Migration

**Before (MediatR Pipeline):**
```csharp
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    // Validation logic
}
```

**After (Wolverine Middleware):**
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

### Deliverables

- [ ] Zero MediatR references
- [ ] All behaviors migrated to middleware
- [ ] Clean compilation
- [ ] All tests passing

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

## Technical Benefits

### Performance

```
Before (MediatR):
  Request → Handler → Response
  ~2-5ms overhead

After (Wolverine):
  Request → Handler → Response
  ~0.5-1ms overhead

  + Durable outbox for reliability
  + Built-in retries
  + Distributed tracing
```

### Reliability

```
With Outbox Pattern:
  1. Command received
  2. Business logic executes
  3. Events written to outbox (same transaction)
  4. Transaction commits
  5. Background process sends events

  Result: Zero message loss, exactly-once delivery
```

---

## Related Documents

- [Hybrid Data Strategy](hybrid-data-strategy-roadmap.md)
- [Implementation Roadmap](implementation-roadmap.md)
- [ADR-0004: MediatR for CQRS](../architecture/adr/ADR-0004-use-mediatr-for-cqrs.md)

---

**Last Updated**: 2025-12-22
