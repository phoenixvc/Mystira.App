# Hybrid Data Strategy Roadmap

**Status**: Planned
**Last Updated**: 2025-12-22
**Owner**: Development Team
**Source**: [Mystira.workspace](https://github.com/phoenixvc/Mystira.workspace)

---

## Executive Summary

This roadmap implements a transition from Cosmos DB-only architecture to a hybrid polyglot persistence model incorporating Cosmos DB, PostgreSQL, and Redis across all platform components.

---

## Current State

| Component | Current Database | Status |
|-----------|-----------------|--------|
| Mystira.App | Cosmos DB | Active |
| Mystira.Admin.Api | Cosmos DB (shared) | Active |
| Mystira.Publisher | Cosmos DB | Active |
| Mystira.Chain | PostgreSQL | Active |
| Mystira.StoryGenerator | Cosmos DB | Active |

---

## Target Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Mystira.App │  │ Admin.Api   │  │ Publisher/Chain/etc │  │
│  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘  │
│         │                │                     │             │
│  ┌──────▼────────────────▼─────────────────────▼──────────┐ │
│  │              Hybrid Data Access Layer                   │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │ │
│  │  │ PostgreSQL  │ │  Cosmos DB  │ │     Redis       │   │ │
│  │  │ (Primary)   │ │ (Document)  │ │    (Cache)      │   │ │
│  │  └─────────────┘ └─────────────┘ └─────────────────┘   │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Database Roles

| Database | Purpose | Data Types |
|----------|---------|------------|
| **PostgreSQL** | Primary transactional data | Accounts, Sessions, Royalties, Transactions |
| **Cosmos DB** | Document storage | Scenarios, ContentBundles, UserProfiles |
| **Redis** | Caching & real-time | Session cache, Leaderboards, Rate limiting |

---

## Phase 1: Foundation (Weeks 1-6)

### 1.1 Infrastructure Provisioning

- [ ] Provision PostgreSQL via Terraform
- [ ] Provision Redis via Terraform
- [ ] Configure connection strings in Key Vault
- [ ] Set up environment-specific configurations

### 1.2 New Infrastructure Projects

Create three new projects in Mystira.App:

```
src/
├── Mystira.App.Infrastructure.Postgres/
│   ├── Repositories/
│   ├── PostgresDbContext.cs
│   └── Mystira.App.Infrastructure.Postgres.csproj
├── Mystira.App.Infrastructure.Redis/
│   ├── Services/
│   ├── RedisCacheService.cs
│   └── Mystira.App.Infrastructure.Redis.csproj
└── Mystira.App.Infrastructure.Hybrid/
    ├── HybridRepository.cs
    ├── DataRouter.cs
    └── Mystira.App.Infrastructure.Hybrid.csproj
```

### 1.3 Dual-Write Pattern

- [ ] Implement dual-write to both Cosmos DB and PostgreSQL
- [ ] Add sync queue for consistency
- [ ] Create reconciliation service

### Deliverables

- [ ] PostgreSQL and Redis provisioned
- [ ] Three new infrastructure projects created
- [ ] Dual-write pattern operational
- [ ] CI/CD updated for new dependencies

---

## Phase 2: Data Migration (Weeks 7-12)

### 2.1 Migration Tools

- [ ] Build batch migration service
- [ ] Implement incremental sync
- [ ] Create data validation service
- [ ] Build reconciliation reporting

### 2.2 Entity Migration Order

| Priority | Entity | Complexity | Notes |
|----------|--------|------------|-------|
| 1 | Accounts | Low | User data, critical path |
| 2 | Sessions | Low | Transactional data |
| 3 | Royalties | Medium | Financial data, audit trail |
| 4 | Transactions | Medium | High volume |
| 5 | Scenarios | High | Complex documents, keep in Cosmos |
| 6 | ContentBundles | High | Large documents, keep in Cosmos |

### 2.3 Rollout Strategy

1. **Development** - Full migration testing
2. **Staging** - Load testing with production-like data
3. **Production** - Phased rollout by entity type

### Deliverables

- [ ] All relational data migrated to PostgreSQL
- [ ] Zero data loss verified
- [ ] Reconciliation reports clean
- [ ] Rollback procedures tested

---

## Phase 3: Cross-Service Integration (Weeks 13-20)

### 3.1 Event-Driven Architecture

Implement using Wolverine framework (see ADR-0015):

```csharp
// Domain events for cross-service communication
public record AccountCreatedEvent(Guid AccountId, string Email);
public record RoyaltyPaidEvent(string IpAssetId, decimal Amount);
public record SessionCompletedEvent(Guid SessionId, Guid AccountId);
```

### 3.2 Service Integration

| Service | Events Published | Events Consumed |
|---------|------------------|-----------------|
| Mystira.App | Account*, Session*, Royalty* | Chain*, Publisher* |
| Mystira.Chain | IpAsset*, Transaction* | Royalty* |
| Mystira.Publisher | Purchase*, Content* | Account*, Session* |
| Mystira.StoryGenerator | Story* | Session*, Content* |

### 3.3 Cache Strategy

```csharp
// Redis caching patterns
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task InvalidateAsync(string pattern);
}
```

### Deliverables

- [ ] Event-driven architecture operational
- [ ] All services connected via Azure Service Bus
- [ ] Redis caching reducing database load
- [ ] Distributed tracing enabled

---

## Phase 4: PostgreSQL Primary (Months 7-12)

### 4.1 Read Cutover

- [ ] Switch reads from Cosmos DB to PostgreSQL for migrated entities
- [ ] Implement read replicas for scale
- [ ] Add query optimization

### 4.2 Cosmos DB Optimization

- [ ] Archive historical data
- [ ] Reduce RU provisioning
- [ ] Maintain only document-type entities

### 4.3 Advanced Caching

- [ ] Implement write-through caching
- [ ] Add cache warming strategies
- [ ] Configure cache invalidation patterns

### Deliverables

- [ ] PostgreSQL serving all relational reads
- [ ] Cosmos DB costs reduced by 50%
- [ ] Sub-5ms cache hit latency
- [ ] Query performance improved

---

## Phase 5: Platform Evolution (Months 12-18)

### 5.1 Analytics & Data Warehouse

- [ ] Implement data warehouse (Azure Synapse or Snowflake)
- [ ] Build analytics pipeline
- [ ] Create business intelligence dashboards

### 5.2 Machine Learning Pipeline

- [ ] User preference modeling
- [ ] Content recommendation engine
- [ ] Royalty prediction models

### 5.3 Multi-Region Expansion

- [ ] PostgreSQL geo-replication
- [ ] Redis cluster across regions
- [ ] Global traffic management

### Deliverables

- [ ] Analytics platform operational
- [ ] ML models serving recommendations
- [ ] Multi-region deployment ready
- [ ] 30% database cost reduction achieved

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Data Loss | 0 | Migration validation |
| Read Latency Impact | < 5ms | P99 latency |
| Data Reconciliation | 100% | Daily reports |
| Event Delivery | 99.9% | Message tracking |
| Cost Reduction | 30% | Azure billing |

---

## Dependencies

```
Mystira.Infra (Terraform)
        │
        ▼
   Mystira.App ──────────────────────┐
        │                            │
        ▼                            ▼
  Admin.Api ◄──Events──► Publisher ◄──► Chain
        │                            │
        ▼                            ▼
   Admin.UI              StoryGenerator
```

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Data inconsistency | Medium | Critical | Dual-write, reconciliation |
| Migration downtime | Low | High | Phased rollout, feature flags |
| Performance regression | Medium | Medium | Load testing, monitoring |
| Cost overrun | Low | Medium | Budget alerts, optimization |

---

## Related Documents

- [ADR-0015: Wolverine Migration](wolverine-migration-roadmap.md)
- [Implementation Roadmap](implementation-roadmap.md)
- [Infrastructure Migration](../architecture/adr/migration-mystira-infra.md)

---

**Last Updated**: 2025-12-22
