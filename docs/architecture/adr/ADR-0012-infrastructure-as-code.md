# ADR-0012: Infrastructure as Code Strategy and Migration

**Status**: 💭 Proposed

**Date**: 2025-12-10

**Deciders**: Development Team

**Tags**: infrastructure, iac, azure, bicep, devops, migration

**Supersedes**: None (documents existing infrastructure and migration path)

---

## Approvals

| Role | Name | Date | Status |
|------|------|------|--------|
| Tech Lead | | | ⏳ Pending |
| DevOps | | | ⏳ Pending |

---

## Context

### Current State (Accurate)

Infrastructure already exists in **Mystira.App** repository with comprehensive Azure Bicep modules:

```
Mystira.App/
├── infrastructure/
│   ├── main.bicep                    # Main orchestration (746 lines)
│   ├── params.dev.json               # Dev environment parameters
│   ├── params.staging.json           # Staging parameters
│   ├── params.prod.json              # Production parameters
│   └── modules/
│       ├── app-service.bicep         # App Service (API, Admin API)
│       ├── application-insights.bicep
│       ├── azure-bot.bicep           # Teams/WhatsApp/Discord bot
│       ├── communication-services.bicep
│       ├── cosmos-db.bicep
│       ├── dns-zone.bicep
│       ├── key-vault.bicep
│       ├── log-analytics.bicep
│       ├── static-web-app.bicep
│       └── storage.bicep
└── .github/workflows/
    ├── infrastructure-deploy-dev.yml
    ├── infrastructure-deploy-staging.yml
    ├── infrastructure-deploy-prod.yml
    ├── mystira-app-api-cicd-dev.yml
    ├── mystira-app-api-cicd-staging.yml
    ├── mystira-app-api-cicd-prod.yml
    ├── mystira-app-admin-api-cicd-*.yml
    └── mystira-app-pwa-cicd-*.yml
```

### Existing Infrastructure Features

The current `main.bicep` supports:

| Resource | Module | Features |
|----------|--------|----------|
| **App Services** | `app-service.bicep` | API + Admin API on shared plan, custom domains, managed certs |
| **Static Web App** | `static-web-app.bicep` | PWA hosting, fallback region (eastus2) for SA North limitations |
| **Cosmos DB** | `cosmos-db.bicep` | Serverless or provisioned, database + containers |
| **Key Vault** | `key-vault.bicep` | JWT keys, Discord/Bot tokens, WhatsApp config |
| **Communication Services** | `communication-services.bicep` | Email, WhatsApp integration |
| **Azure Bot** | `azure-bot.bicep` | Teams + WebChat channels |
| **Monitoring** | `log-analytics.bicep`, `application-insights.bicep` | Full observability |
| **DNS** | `dns-zone.bicep` | Custom domain records |
| **Storage** | `storage.bicep` | Blob storage |

### Naming Convention

All resources follow: `[org]-[env]-[project]-[type]-[region]`
- Example: `mys-dev-mystira-api-san` (API in South Africa North)

### CI/CD Status

**Why deployments aren't fully automated:**

| Workflow | Status | Missing Secrets |
|----------|--------|-----------------|
| Infrastructure Deploy | ⚠️ Requires secrets | `AZURE_CREDENTIALS`, `JWT_RSA_PRIVATE_KEY`, `JWT_RSA_PUBLIC_KEY` |
| API CI/CD | ⚠️ Requires secrets | `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`, `AZURE_CREDENTIALS` |
| Admin API CI/CD | ⚠️ Requires secrets | Same as API |
| PWA CI/CD | ✅ Working | - |
| SWA Preview Tests | ❌ Failing | Smoke test issues |

The workflows are correctly configured but **require GitHub secrets to be set**:

1. **`AZURE_CREDENTIALS`** - Service principal JSON for Azure login
2. **`AZURE_SUBSCRIPTION_ID`** - Azure subscription ID
3. **`JWT_RSA_PRIVATE_KEY`** / **`JWT_RSA_PUBLIC_KEY`** - RS256 signing keys
4. **`AZURE_WEBAPP_PUBLISH_PROFILE_DEV`** - Publish profile for App Service

---

## Problems Identified

1. **Infrastructure Coupled to App Repo**
   - IaC mixed with application code
   - Changes to infrastructure affect app repo CI/CD
   - Hard to manage infrastructure for other repos (Chain, StoryGenerator)

2. **Missing Network Security**
   - No VNet isolation
   - No private endpoints for Cosmos DB/Key Vault
   - Services communicate over public internet

3. **No Global Load Balancing**
   - No Azure Front Door
   - No WAF protection
   - No DDoS protection

4. **Multi-Repo Infrastructure Needs**
   - Mystira.Chain needs Container Apps (not in current IaC)
   - Mystira.StoryGenerator may need separate compute
   - Shared resources (networking, monitoring) need centralization

---

## Decision

### Phase 1: Enable Current CI/CD (Immediate)

Before extracting infrastructure, enable current deployments:

1. Configure required GitHub secrets
2. Verify infrastructure deploys successfully
3. Document the secret setup process

### Phase 2: Add Missing Security (Short-term)

Enhance current infrastructure with:

1. VNet module with subnets
2. Private endpoints for data stores
3. Azure Front Door with WAF
4. Network Security Groups

### Phase 3: Extract to Mystira.Infra (Medium-term)

Create dedicated `Mystira.Infra` repository:

1. Move/copy `infrastructure/` directory
2. Add multi-repo support (Chain, StoryGenerator)
3. Add VNet and Front Door modules
4. Keep Mystira.App infrastructure in sync during transition

---

## Repository Structure (Future: Mystira.Infra)

```
Mystira.Infra/
├── modules/                    # Shared Bicep modules
│   ├── compute/
│   │   ├── app-service.bicep  # From Mystira.App
│   │   ├── container-app.bicep # NEW: For Mystira.Chain
│   │   └── static-web-app.bicep
│   ├── data/
│   │   ├── cosmos-db.bicep
│   │   ├── key-vault.bicep
│   │   └── storage.bicep
│   ├── networking/            # NEW
│   │   ├── vnet.bicep
│   │   ├── private-endpoints.bicep
│   │   └── nsg.bicep
│   ├── security/              # NEW
│   │   └── front-door.bicep
│   ├── communication/
│   │   ├── communication-services.bicep
│   │   └── azure-bot.bicep
│   └── monitoring/
│       ├── log-analytics.bicep
│       └── application-insights.bicep
├── apps/                       # Per-application orchestration
│   ├── mystira-app/
│   │   ├── main.bicep         # Mystira.App specific
│   │   └── params.*.json
│   ├── mystira-chain/         # NEW
│   │   ├── main.bicep
│   │   └── params.*.json
│   └── mystira-storygenerator/
│       ├── main.bicep
│       └── params.*.json
├── shared/                     # Cross-cutting infrastructure
│   ├── networking.bicep       # VNet, subnets, peering
│   ├── front-door.bicep       # Global load balancer
│   └── dns.bicep              # DNS zones
├── scripts/
│   ├── deploy.sh
│   ├── deploy.ps1
│   └── migrate-from-app.sh    # Migration helper
└── .github/workflows/
    ├── validate.yml
    └── deploy.yml
```

---

## Target Network Architecture

```
                              Internet
                                 │
                    ┌────────────▼────────────┐
                    │    Azure Front Door     │
                    │  ┌─────────────────────┐│
                    │  │ WAF │ SSL │ Routing ││
                    │  └─────────────────────┘│
                    └───────────┬─────────────┘
        ┌───────────────────────┼───────────────────────┐
        │                       │                       │
        ▼                       ▼                       ▼
   mystira.app          api.mystira.app        admin.mystira.app
        │                       │                       │
┌───────▼───────────────────────▼───────────────────────▼───────┐
│                    Azure Virtual Network                       │
│                       10.0.0.0/16                              │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │ App Subnet (10.0.1.0/24)                                 │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐ │  │
│  │  │ SWA      │  │ API App  │  │Admin App │  │Container │ │  │
│  │  │ (PWA)    │  │ Service  │  │ Service  │  │App Chain │ │  │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘ │  │
│  └─────────────────────────────────────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │ Private Endpoints Subnet (10.0.2.0/24)                   │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐               │  │
│  │  │ Cosmos   │  │ Key      │  │ Storage  │               │  │
│  │  │ DB (PE)  │  │ Vault(PE)│  │ (PE)     │               │  │
│  │  └──────────┘  └──────────┘  └──────────┘               │  │
│  └─────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────┘
```

---

## Migration Plan

### Step 1: Document Current State ✅
- [x] Audit existing infrastructure
- [x] Document CI/CD workflows
- [x] Identify missing secrets

### Step 2: Enable Deployments
- [ ] Set up GitHub secrets (see Migration Guide)
- [ ] Test infrastructure deploy workflow
- [ ] Test API/Admin API deployment

### Step 3: Add Security Modules (In Mystira.App first)
- [ ] Add VNet module to `infrastructure/modules/`
- [ ] Add private endpoints
- [ ] Test in dev environment

### Step 4: Create Mystira.Infra Repo
- [ ] Create repository
- [ ] Copy modules from Mystira.App
- [ ] Add Container Apps module for Mystira.Chain
- [ ] Add Front Door module

### Step 5: Migrate References
- [ ] Update Mystira.App workflows to reference Mystira.Infra
- [ ] Add Mystira.Chain infrastructure
- [ ] Deprecate Mystira.App/infrastructure (keep for reference)

---

## Consequences

### Positive ✅

1. **Separation of Concerns** - Infrastructure managed independently
2. **Multi-Repo Support** - Single place for all app infrastructure
3. **Security Improvements** - VNet, private endpoints, WAF
4. **Cleaner Application Repos** - No infrastructure code mixed in

### Negative ❌

1. **Migration Effort** - Time to extract and test
2. **Coordination** - Changes span multiple repos
3. **Initial Complexity** - More repos to manage

---

## Related Decisions

- **ADR-0010**: Story Protocol SDK Integration (needs Container Apps)
- **ADR-0011**: Unified Workspace Repository (Mystira.Infra in workspace)

---

## References

- [Current Infrastructure](../../../infrastructure/main.bicep)
- [Azure Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Migration Guide](./MIGRATION-MYSTIRA-INFRA.md)

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
