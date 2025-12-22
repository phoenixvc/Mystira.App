# App Components Extraction Analysis

**Date**: 2025-12-22
**Status**: Analysis Complete - Recommendations Provided
**Related**: [ADR-0005](../architecture/adr/ADR-0005-separate-api-and-admin-api.md), [Migration Plan](../migration/admin-api-extraction-plan.md)

---

## Executive Summary

This document analyzes the separation of Admin API and Public API components within Mystira.App. After careful evaluation, we **recommend extracting the Admin API** into a separate repository to improve security isolation, enable independent releases, and support team scalability.

---

## Current State

### Application Structure

```
Mystira.App/
├── src/
│   ├── Mystira.App.Api/           # Public API
│   ├── Mystira.App.Admin.Api/     # Admin API + Razor UI
│   ├── Mystira.App.Domain/        # Shared domain
│   ├── Mystira.App.Application/   # Shared application logic (CQRS)
│   ├── Mystira.App.Infrastructure.*/  # Shared infrastructure
│   └── Mystira.App.Pwa/           # Public PWA
```

### Current Integration Points

| Component | Shared With | Coupling Type |
|-----------|-------------|---------------|
| Domain Models | Both APIs | Code (project reference) |
| CQRS Handlers | Both APIs | Code (project reference) |
| Repositories | Both APIs | Code (project reference) |
| Authentication | Both APIs | Configuration (Auth0) |
| Database | Both APIs | Data (Cosmos DB) |

---

## Key Concerns with Current Structure

### 1. Admin UI as Razor Pages in Admin API

**Current State**: Admin UI uses server-rendered Razor Pages embedded in `Mystira.App.Admin.Api`

**Problems**:

| Issue | Impact |
|-------|--------|
| Mixed Concerns | UI and API logic tightly coupled |
| Not Modern | Server-rendering less flexible than SPA |
| Deployment Coupling | Frontend and backend deploy together |
| Scaling Limitations | UI and API scale together |
| Technology Lock-in | Harder to migrate to modern frontend |

**If Separated**:

- Modern frontend (React/Vue/Blazor standalone)
- Independent deployment and scaling
- Better separation of concerns
- Frontend team autonomy

### 2. Different Security Boundaries

| Aspect | Public API | Admin API |
|--------|------------|-----------|
| Users | End users (external) | Staff/admins (internal) |
| Endpoints | Public-facing | High-security |
| Auth | Standard OAuth | Enhanced auth + MFA |
| Access | Open registration | Restricted |
| Availability | High (99.9% SLA) | Standard (99.5% SLA) |

**Implication**: Different security postures suggest separation improves security isolation.

### 3. Different Release Cycles

| Consideration | Public API | Admin API |
|---------------|------------|-----------|
| Iteration Speed | Stable, slower | Rapid, experimental |
| Risk Tolerance | Low | Higher (internal users) |
| Deployment Frequency | Weekly | Daily |
| Breaking Changes | Rare | More acceptable |

**If Separate**: Independent release cycles without cross-impact.

### 4. Shared Dependencies Analysis

**Reality Check**: Shared dependencies don't require same repository.

**Options for Shared Code**:

| Approach | Pros | Cons |
|----------|------|------|
| NuGet Packages | Versioned, explicit | Package management overhead |
| Git Submodules | Single source | Complexity for .NET |
| Shared Repository | Centralized | Additional repo to manage |

**Recommended**: NuGet packages for:
- `Mystira.App.Domain`
- `Mystira.App.Application`
- `Mystira.App.Infrastructure.*`
- `Mystira.App.Contracts`

**Verdict**: Shared dependencies can be managed via packages - not a blocker for extraction.

---

## Analysis Criteria

We evaluate extraction based on:

| Criterion | Weight | Description |
|-----------|--------|-------------|
| Security Isolation | High | Separate attack surface |
| Release Independence | High | Deploy without coordination |
| Team Scalability | Medium | Support team growth |
| Operational Simplicity | Medium | Reduce blast radius |
| Development Velocity | Medium | Faster iteration |
| Migration Effort | Low | One-time cost |

---

## Options Evaluated

### Option A: Keep Together (Status Quo)

**Description**: Maintain current monorepo structure.

**Pros**:
- No migration effort
- Single deployment pipeline
- Shared code via project references

**Cons**:
- Security boundaries not enforced
- Release cycles coupled
- Scaling requires full deployment
- UI/API tightly coupled

**Verdict**: Acceptable short-term, not recommended long-term.

### Option B: Extract Admin API Only

**Description**: Move `Mystira.App.Admin.Api` to `Mystira.Admin.Api` repository.

**Pros**:
- Security isolation
- Independent releases
- Cleaner codebase
- Same effort as Option C Phase 1

**Cons**:
- Admin UI still coupled with API
- Doesn't modernize frontend

**Verdict**: Good intermediate step.

### Option C: Extract Admin API + Modern UI (Recommended)

**Description**:
1. Move Admin API to `Mystira.Admin.Api`
2. Create `Mystira.Admin.UI` with modern SPA

**Pros**:
- Full separation of concerns
- Modern frontend architecture
- Independent scaling
- Team autonomy
- Future-proof

**Cons**:
- Higher initial effort
- More repositories to manage
- Package management setup

**Verdict**: **Recommended** - Best long-term architecture.

---

## Recommendation

### Primary Recommendation: **Extract Admin API + Create Modern UI**

**Rationale**:

1. **Better Architecture**: Separates concerns, improves security isolation
2. **Modern Best Practices**: Aligns with service-oriented architecture
3. **Future-Proof**: Better positioned for growth, scaling, and team expansion
4. **Admin UI Opportunity**: Enables modern frontend architecture

### When to Extract

Extract Admin API if any of these apply:

- [x] Different security requirements for admin vs public
- [x] Desire for independent release cycles
- [ ] Different teams will own Admin vs Public
- [x] Plans to modernize admin UI
- [x] Long-term maintainability priority

### When to Keep Together

Keep together if ALL of these apply:

- [ ] Same small team (< 5 developers) owns both
- [ ] Release cycles tightly coupled forever
- [ ] No plans for growth or change
- [ ] Current structure works well
- [ ] Package management too complex

---

## Migration Strategy

### Phase 1: Setup Shared Packages

```
Mystira.App.Domain → NuGet package
Mystira.App.Application → NuGet package
Mystira.App.Infrastructure.* → NuGet packages
Mystira.App.Contracts → NuGet package
```

### Phase 2: Extract Admin API

1. Create `Mystira.Admin.Api` repository
2. Copy Admin API code
3. Replace project references with NuGet packages
4. Set up CI/CD
5. Deploy independently

### Phase 3: Extract Admin UI

1. Create `Mystira.Admin.UI` repository
2. Build modern frontend (React/Vue/Blazor)
3. Connect to Admin API via REST/gRPC
4. Deploy as static site

See [Admin API Extraction Plan](../migration/admin-api-extraction-plan.md) for detailed steps.

---

## Impact Assessment

### Positive Impacts

| Impact | Magnitude | Beneficiary |
|--------|-----------|-------------|
| Security isolation | High | Platform |
| Release velocity | High | Admin team |
| Code clarity | Medium | All developers |
| Scaling flexibility | Medium | Operations |
| Team autonomy | Medium | Both teams |

### Negative Impacts

| Impact | Magnitude | Mitigation |
|--------|-----------|------------|
| Initial effort | Medium | Phased approach |
| Package management | Low | Automation |
| More repositories | Low | Clear ownership |
| Coordination overhead | Low | Versioned contracts |

---

## Decision Framework

### Extract If:

- [ ] Different team ownership (current or planned)
- [ ] Different release cycle needs
- [ ] Security isolation requirements
- [ ] Plans to modernize admin UI
- [ ] Growth/scaling considerations

### Keep Together If:

- [ ] Very small team (1-3 developers)
- [ ] No security differentiation needed
- [ ] Same release cadence forever
- [ ] No frontend modernization planned

---

## Next Steps

1. **Review with Team**: Discuss recommendation
2. **Evaluate Package Management**: Set up internal NuGet feed
3. **Assess Team Structure**: Determine ownership model
4. **Plan Migration**: Create detailed timeline
5. **Execute Phase 1**: Setup shared packages

---

## Conclusion

The initial analysis correctly identified the trade-offs, but underestimated the benefits of separation. While shared dependencies make extraction more work, modern software architecture practices favor separation of services with different security boundaries, release cycles, and concerns.

**Final Recommendation**: **Extract Admin API** and create modern Admin UI.

This positions Mystira.App for:
- Better security posture
- Faster admin feature iteration
- Cleaner codebase
- Team scalability
- Modern frontend capabilities

---

## Related Documents

- [ADR-0005: Separate API and Admin API](../architecture/adr/ADR-0005-separate-api-and-admin-api.md)
- [Admin API Extraction Plan](../migration/admin-api-extraction-plan.md)
- [CQRS Migration Guide](../architecture/cqrs-migration-guide.md)
- [Implementation Roadmap](../planning/implementation-roadmap.md)

---

**Last Updated**: 2025-12-22
