# ADR-0014: Mystira.App Implementation Roadmap

**Status**: ✅ Accepted

**Date**: 2025-12-22

**Deciders**: Development Team

**Tags**: planning, roadmap, implementation, strategy, polyglot, infrastructure

**Relates To**: ADR-0011 (Workspace Orchestration), ADR-0013 (gRPC Integration), ADR-0012 (Infrastructure as Code)

---

## Approvals

| Role | Name | Date | Status |
|------|------|------|--------|
| Tech Lead | | | ✅ Approved |
| DevOps | | | ⏳ Pending |

---

## Context

Mystira.App requires a structured approach to implementation that balances multiple priorities:

1. **Infrastructure Foundation**: Complete Terraform migration, establish naming conventions
2. **Polyglot Integration**: Enable gRPC communication between .NET, Python, and TypeScript services
3. **Admin API Extraction**: Complete migration to separate repositories (already in progress)
4. **Observability & Security**: Monitoring, compliance, and security hardening
5. **Developer Experience**: Streamline local development and CI/CD

### Current State

| Area | Status | Notes |
|------|--------|-------|
| CQRS Migration | Complete | All 8 entities migrated (ADR-0006) |
| Hexagonal Architecture | Complete | Clean boundaries established |
| Admin API Extraction | In Progress | Repos created, migration ongoing |
| Infrastructure | In Progress | Bicep to Terraform migration |
| Polyglot/gRPC | Proposed | ADR-0013 defined, not implemented |
| Monitoring | Partial | Basic Application Insights |

### Problem Statement

Without a clear roadmap:
- Priorities conflict and create churn
- Cross-cutting concerns (like gRPC) get delayed indefinitely
- Teams work on different areas without coordination
- No clear success metrics for each phase

---

## Decision

We will adopt a **phased implementation roadmap** with clear priorities, deliverables, and success metrics for each phase.

### Phase Overview

| Phase | Focus Area | Priority | Status |
|-------|------------|----------|--------|
| Phase 1 | Infrastructure Foundation | High | In Progress |
| **Phase 1.5** | **Polyglot Integration (gRPC)** | **High** | **Planned** |
| Phase 2 | Pipeline Enhancement | High | Planned |
| Phase 3 | Monitoring & Observability | Medium | Planned |
| Phase 4 | Documentation & Knowledge Management | Medium | Planned |
| Phase 5 | Security & Compliance | High | Planned |
| Phase 6 | Performance & Scalability | Medium | Planned |
| Phase 7 | Developer Experience | Medium | Planned |
| Phase 8 | Advanced Features | Low | Planned |

### Key Decision: Prioritize Polyglot Integration Early (Phase 1.5)

**Rationale**:

1. **Cross-Service Dependencies**: Mystira.Chain (Python) is blocked until gRPC is implemented
2. **Performance Critical Path**: REST latency impacts user-facing features
3. **Foundation for Future Services**: All new services will use gRPC
4. **Streaming Requirements**: Blockchain transaction monitoring needs server streaming

**Polyglot Architecture**:

```
┌─────────────────────┐       ┌──────────────────────┐       ┌─────────────────┐
│  Mystira.App.Api    │──────▶│   Mystira.Chain      │──────▶│  Story Protocol │
│  (.NET 9)           │ gRPC  │   (Python)           │  SDK  │  Blockchain     │
└─────────────────────┘       └──────────────────────┘       └─────────────────┘
        │                              │
        │      ┌───────────────────────┘
        │      │
        ▼      ▼
    Shared .proto files
```

### Phase 1.5 Deliverables

| Deliverable | Description |
|-------------|-------------|
| Proto Definitions | `chain_service.proto`, `ip_assets.proto`, `royalties.proto` |
| .NET Client | `GrpcChainServiceAdapter` with retry policies |
| Python Server | `ChainServiceServicer` with health checks |
| Streaming | `WatchTransactions`, `BatchRegisterIpAssets` |
| Migration | Feature flags, gradual rollout, REST deprecation |

### Success Metrics

| Metric | Target |
|--------|--------|
| Latency (p50) | <15ms (3x improvement) |
| Latency (p99) | <40ms (4.5x improvement) |
| Throughput | 5,000+ req/s (4x improvement) |
| Payload Size | <500 bytes (5x smaller) |

---

## Consequences

### Positive Consequences ✅

1. **Clear Priorities**
   - Team alignment on what to work on next
   - Stakeholder visibility into progress
   - Reduced context switching

2. **Early Polyglot Integration**
   - Unblocks Mystira.Chain development
   - Performance improvements sooner
   - Foundation for future services

3. **Measurable Progress**
   - Success metrics for each phase
   - Clear deliverables to track
   - Regular evaluation points

4. **Risk Mitigation**
   - Phased approach allows course correction
   - Dependencies identified upfront
   - Parallel workstreams where possible

### Negative Consequences ❌

1. **Scope Creep Risk**
   - Each phase may expand beyond original scope
   - **Mitigation**: Strict deliverable definitions, phase gates

2. **Resource Constraints**
   - Multiple phases may compete for attention
   - **Mitigation**: Clear prioritization, phase dependencies

3. **External Dependencies**
   - Some phases depend on external factors (Azure features, team capacity)
   - **Mitigation**: Identify and track dependencies explicitly

---

## Implementation

The detailed roadmap is maintained in:

- **[Implementation Roadmap](../../planning/implementation-roadmap.md)** - Full phase details, deliverables, metrics

### Related Documents

| Document | Purpose |
|----------|---------|
| [ADR-0013](ADR-0013-grpc-for-csharp-python-integration.md) | gRPC technical specification |
| [ADR-0012](ADR-0012-infrastructure-as-code.md) | Infrastructure approach |
| [ADR-0011](ADR-0011-unified-workspace-orchestration.md) | Workspace structure |
| [Admin API Extraction](../../migration/admin-api-extraction-plan.md) | Migration plan |

---

## Alignment with Workspace

This roadmap aligns with `Mystira.workspace` documentation:

| Workspace Doc | Local Implementation |
|---------------|---------------------|
| ADR-0005 (Service Networking) | Phase 1.5 Polyglot Integration |
| docs/planning/implementation-roadmap.md | Adapted for Mystira.App context |
| docs/migration/ | Admin API extraction plan |
| docs/analysis/ | Component extraction analysis |

---

## Review Schedule

| Milestone | Review Date | Focus |
|-----------|-------------|-------|
| Phase 1 Complete | TBD | Infrastructure foundation |
| Phase 1.5 Complete | TBD | gRPC integration validated |
| Quarterly Review | Every 3 months | Overall roadmap progress |

---

## References

- [Mystira.workspace Repository](https://github.com/phoenixvc/Mystira.workspace)
- [Implementation Roadmap](../../planning/implementation-roadmap.md)
- [gRPC Official Documentation](https://grpc.io/docs/)
- [Azure Naming Conventions](https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
