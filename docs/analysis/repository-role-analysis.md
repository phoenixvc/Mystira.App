# Mystira.App Repository Role Analysis

**Date**: 2025-12-22
**Status**: Analysis Complete
**Related**: [ADR-0011](../architecture/adr/ADR-0011-unified-workspace-orchestration.md)

---

## Executive Summary

This document analyzes Mystira.App's role within the broader Mystira workspace ecosystem. Mystira.App serves as the **core platform repository**, containing the main .NET application with both public and admin APIs, and should remain a **cohesive submodule** within the workspace.

---

## Context: Mystira Ecosystem

### Repository Overview

| Repository | Description | Tech Stack | Deployment | Status |
|------------|-------------|------------|------------|--------|
| **Mystira.App** | Main platform - APIs, PWA | .NET 9, Blazor, Cosmos DB | Azure PaaS | Active |
| Mystira.Chain | Blockchain integration (Story Protocol) | Python, FastAPI | Kubernetes | Active |
| Mystira.Publisher | Publishing frontend | TypeScript/React | Kubernetes | Active |
| Mystira.StoryGenerator | Story generation engine | .NET | Kubernetes | Active |
| Mystira.Infra | Infrastructure as Code | Terraform, K8s | N/A | Proposed |
| Mystira.workspace | Multi-repo workspace | Scripts, Docs | N/A | Active |

### Coupling Types

| Coupling Type | Description | Example |
|---------------|-------------|---------|
| Code | Direct dependencies via packages | Domain models |
| Network | API calls between services | gRPC, REST |
| Data | Shared database/storage | Cosmos DB |
| Infrastructure | Shared Azure resources | Log Analytics |

---

## Mystira.App Analysis

### Current State

```
Mystira.App/
├── src/
│   ├── Mystira.App.Api/              # Public REST API
│   ├── Mystira.App.Admin.Api/        # Admin REST API + Razor UI
│   ├── Mystira.App.Domain/           # Domain models & entities
│   ├── Mystira.App.Application/      # CQRS handlers, specifications
│   ├── Mystira.App.Infrastructure.*/ # DB, Auth, Storage adapters
│   └── Mystira.App.Pwa/              # Blazor PWA
├── tests/                            # Unit & integration tests
├── infrastructure/                   # Azure Bicep (migrating to Infra)
├── docs/                             # Documentation
└── .github/workflows/                # CI/CD pipelines
```

### Key Characteristics

| Characteristic | Value | Notes |
|----------------|-------|-------|
| Technology | .NET 9 | Unified stack |
| Architecture | Hexagonal + CQRS | Clean architecture |
| Database | Azure Cosmos DB | Document store |
| Authentication | Entra External ID | OAuth 2.0 / OIDC |
| Deployment | Azure App Service | PaaS model |

### Analysis Criteria

| Criterion | Evaluation | Score |
|-----------|------------|-------|
| **Independence** | Can deploy independently | High |
| **Coupling** | Shares domain with internal projects | Medium |
| **Tech Uniformity** | All .NET 9 | High |
| **Release Cycle** | Unified release for all App components | High |
| **Team Ownership** | Single platform team | High |
| **Deployment Model** | Azure PaaS (different from K8s services) | High |

---

## Recommendation: Keep as Cohesive Submodule

### Rationale

1. **Cohesive Domain**: All projects share the domain model and .NET ecosystem
2. **Unified Deployment**: Azure PaaS deployment model (different from K8s services)
3. **Infrastructure Co-location**: Has its own infrastructure in `infrastructure/`
4. **Shared Libraries**: Domain and infrastructure libraries shared across projects
5. **Technology Consistency**: All .NET 9, benefits from unified build
6. **Single Team**: Platform team owns all App components

### What Should NOT Be Extracted

| Component | Reason to Keep |
|-----------|----------------|
| Mystira.App.Api | Core public-facing API |
| Mystira.App.Pwa | Tightly coupled with API |
| Mystira.App.Domain | Shared by all App components |
| Mystira.App.Application | CQRS handlers used everywhere |
| Mystira.App.Infrastructure.* | Adapters for App services |

### What MIGHT Be Extracted (Future)

| Component | Condition | Priority |
|-----------|-----------|----------|
| Admin API | If different team, security needs | Medium |
| Admin UI | If modernizing to SPA | Low |
| Infrastructure | When Mystira.Infra is ready | In Progress |

See [App Components Extraction Analysis](app-components-extraction.md) for Admin API extraction details.

---

## Integration Points

### With Mystira.Chain (Python/gRPC)

```
Mystira.App.Api  ──gRPC──>  Mystira.Chain
     │
     └── Story Protocol interactions
         - Asset registration
         - License management
         - Royalty processing
```

**Coupling**: Network-based (gRPC)
**Contracts**: Protocol Buffers (.proto files)
**Impact**: Low coupling, well-defined interface

### With Mystira.StoryGenerator

```
Mystira.App.Api  ──REST──>  Mystira.StoryGenerator
     │
     └── Story generation requests
         - Generate story content
         - Process narrative choices
```

**Coupling**: Network-based (REST)
**Contracts**: OpenAPI specification
**Impact**: Low coupling, versioned API

### With Mystira.Publisher

```
Mystira.Publisher  ──REST──>  Mystira.Chain
        │
        └── No direct integration with App
```

**Coupling**: None with Mystira.App
**Impact**: Independent service

---

## Benefits of Current Structure

### Advantages

1. **Single Build**: One solution, one build process
2. **Atomic Changes**: Domain changes apply everywhere
3. **Refactoring**: Easy cross-project refactoring
4. **Testing**: Integration tests in single repo
5. **CI/CD**: Single pipeline for all App components

### When Structure Works Well

- Same team owns all components
- Unified release cycle
- Shared domain model
- Same technology stack
- Integrated testing needs

---

## Deployment Architecture

### Current

```
Azure Resource Group: mystira-[env]-rg
│
├── App Service Plan
│   ├── mystira-[env]-api      ← Mystira.App.Api
│   └── mystira-[env]-admin    ← Mystira.App.Admin.Api
│
├── Static Web App
│   └── mystira-[env]-pwa      ← Mystira.App.Pwa
│
├── Cosmos DB Account
│   └── mystira-[env]-cosmos
│
└── Supporting Services
    ├── Key Vault
    ├── Application Insights
    └── Storage Account
```

### Future (with Mystira.Infra)

```
Mystira.Infra manages:
├── Shared Resources
│   ├── Container Registry
│   ├── Log Analytics
│   └── Virtual Network
│
└── App-Specific Resources
    └── Defined in Mystira.App/infrastructure/
        (Azure Bicep → Terraform migration)
```

---

## Workspace Integration

### In Mystira.workspace

```
~/mystira/
├── .workspace/              ← Mystira.workspace (orchestration)
├── Mystira.App/             ← This repository
├── Mystira.Chain/
├── Mystira.Infra/
├── Mystira.Publisher/
└── Mystira.StoryGenerator/
```

### VS Code Workspace Configuration

```jsonc
{
  "folders": [
    { "name": "Mystira.App", "path": "../Mystira.App" },
    { "name": "Mystira.Chain", "path": "../Mystira.Chain" },
    // ...
  ]
}
```

### Benefits of Workspace Approach

1. **Full Codebase Visibility**: AI assistants see all code
2. **Cross-Repo Search**: Find patterns across services
3. **Centralized Docs**: Single documentation source
4. **Optional Usage**: Not required for individual work

---

## Summary

| Aspect | Status | Notes |
|--------|--------|-------|
| Repository Role | Core Platform | Central to Mystira ecosystem |
| Extraction Status | Keep as Submodule | Well-organized monorepo |
| Internal Modularization | In Progress | CQRS complete, Admin extraction proposed |
| Workspace Integration | Active | Part of multi-repo workspace |
| Infrastructure | Migrating | Moving to Mystira.Infra |

### Key Takeaways

1. **Mystira.App is the core platform** - Main user-facing application
2. **Keep as cohesive repository** - Shared domain, unified release
3. **Internal modularization OK** - CQRS, hexagonal architecture
4. **External extraction selective** - Only Admin API if needed
5. **Infrastructure migrating** - Moving to dedicated Infra repo

---

## Related Documents

- [ADR-0011: Unified Workspace Orchestration](../architecture/adr/ADR-0011-unified-workspace-orchestration.md)
- [App Components Extraction](app-components-extraction.md)
- [Infrastructure Migration](../architecture/adr/migration-mystira-infra.md)
- [CQRS Migration Guide](../architecture/cqrs-migration-guide.md)

---

**Last Updated**: 2025-12-22
