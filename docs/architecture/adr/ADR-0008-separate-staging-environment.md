# ADR-0008: Implement Separate Staging Environment Stack

**Status**: Proposed

**Date**: 2025-12-07

**Deciders**: Architecture Team

**Tags**: infrastructure, environments, staging, devops, azure, testing

---

## Context

The Mystira.App application currently lacks a proper staging environment that mirrors production. This has led to environment-specific issues being discovered only in production, including the recent "connection refused" error where the PWA was attempting to connect to `localhost:5260` instead of the production API.

### Current Environment Setup

```
┌─────────────────────────────────────────────────────────────────┐
│                    CURRENT STATE                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Development (Local)          Production                         │
│  ┌──────────────────┐        ┌──────────────────┐               │
│  │ localhost:5260   │        │ mystira-api-prod │               │
│  │ localhost:5261   │   →    │ mystira-admin-   │               │
│  │ localhost:5000   │  ???   │     api-prod     │               │
│  └──────────────────┘        │ mystira-pwa-prod │               │
│                              └──────────────────┘               │
│                                                                   │
│  Gap: No staging environment to catch environment-specific       │
│       configuration issues before production                     │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Problems with Current Approach

1. **No Pre-Production Validation**
   - Changes go directly from development to production
   - Environment-specific bugs discovered by users
   - No opportunity to test production-like configurations

2. **Configuration Drift**
   - Development uses localhost URLs
   - Production uses different settings
   - No intermediate validation step

3. **Integration Testing Gaps**
   - Cannot test API integrations before production
   - Third-party service integrations untested
   - Authentication flows not validated

4. **No Safe Experimentation**
   - New features cannot be tested safely
   - Database migrations tested only in production
   - Performance testing affects real users

5. **Incident Response Limitations**
   - Cannot reproduce production issues safely
   - No environment to test hotfixes
   - Rollback strategies untested

6. **Compliance Concerns**
   - No separation of test/production data
   - Changes not validated before affecting users
   - Audit trail gaps

### Considered Alternatives

1. **Use Production with Feature Flags**
   - ✅ Lower infrastructure cost
   - ✅ No environment sync issues
   - ❌ Production data at risk during testing
   - ❌ Cannot test infrastructure changes
   - ❌ Performance testing affects users

2. **Development Environment Only**
   - ✅ No additional cost
   - ✅ Fast iteration
   - ❌ Localhost != cloud environment
   - ❌ Missing Azure-specific configurations
   - ❌ No integration testing

3. **Staging Slots on Same Resources**
   - ✅ Lower cost than separate resources
   - ✅ Easy deployment swapping
   - ❌ Shared database creates risks
   - ❌ Resource contention
   - ❌ Cannot test database changes safely

4. **Full Separate Staging Stack** ⭐ **CHOSEN**
   - ✅ True production mirror
   - ✅ Complete isolation
   - ✅ Safe testing environment
   - ✅ Can test infrastructure changes
   - ✅ Database migration validation
   - ❌ Additional infrastructure cost
   - ❌ Environment sync maintenance

---

## Decision

We will implement a **complete separate staging environment** that mirrors production infrastructure. This provides a safe pre-production validation environment that catches environment-specific issues before they affect users.

### Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    TARGET STATE                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Development          Staging                 Production         │
│  ┌────────────┐      ┌────────────────┐      ┌────────────────┐ │
│  │ localhost  │  →   │ mystira-*-stag │  →   │ mystira-*-prod │ │
│  │            │      │                │      │                │ │
│  │ Unit Tests │      │ Integration    │      │ Production     │ │
│  │ Local Dev  │      │ UAT            │      │ Users          │ │
│  │            │      │ Performance    │      │                │ │
│  └────────────┘      └────────────────┘      └────────────────┘ │
│                                                                   │
│  Promotion Flow: PR Merge → Staging Deploy → Validation →        │
│                  Production Deploy                               │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Staging Environment Components

| Production Resource | Staging Equivalent | Notes |
|--------------------|-------------------|-------|
| mystira-pwa-prod | mystira-pwa-stag | Static Web App |
| mystira-api-prod | mystira-api-stag | App Service |
| mystira-admin-api-prod | mystira-admin-api-stag | App Service |
| mystira-cosmos-prod | mystira-cosmos-stag | Cosmos DB (Serverless) |
| mystirastorageprod | mystirastoragestag | Storage Account |
| mystira-fd-prod | mystira-fd-stag | Front Door |
| mystira-kv-prod | mystira-kv-stag | Key Vault |
| mystira-appinsights-prod | mystira-appinsights-stag | App Insights |

### Resource Naming Convention

```
Pattern: mystira-{service}-{environment}

Examples:
- mystira-api-stag      (Staging API)
- mystira-api-prod      (Production API)
- mystira-cosmos-stag   (Staging Database)
- mystira-cosmos-prod   (Production Database)
```

### Configuration Strategy

Each environment will have dedicated configuration:

**Staging (`appsettings.Staging.json`)**:
```json
{
  "Environment": "Staging",
  "Api": {
    "BaseUrl": "https://mystira-fd-stag.azurefd.net/api"
  },
  "CosmosDb": {
    "DatabaseId": "mystira-stag"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**Production (`appsettings.Production.json`)**:
```json
{
  "Environment": "Production",
  "Api": {
    "BaseUrl": "https://mystira.app/api"
  },
  "CosmosDb": {
    "DatabaseId": "mystira-prod"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

### Deployment Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                    CI/CD PIPELINE                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐     │
│  │  Build   │ → │  Test    │ → │ Staging  │ → │Production│     │
│  │          │   │          │   │ Deploy   │   │ Deploy   │     │
│  └──────────┘   └──────────┘   └──────────┘   └──────────┘     │
│       │              │              │              │             │
│       ▼              ▼              ▼              ▼             │
│  - Compile      - Unit Tests   - Auto Deploy  - Manual Gate    │
│  - Lint         - Integration  - Health Check - Approval Req   │
│  - Build PWA    - Security     - Smoke Tests  - Auto Deploy    │
│  - Package      - SonarQube    - Performance  - Health Check   │
│                                - UAT Ready    - Monitoring     │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Environment Promotion Gates

| Gate | Staging | Production |
|------|---------|------------|
| Automated Tests | Required | Required |
| Health Check | Required | Required |
| Smoke Tests | Required | Required |
| Performance Test | Optional | Required |
| Manual Approval | Not Required | Required |
| Soak Time | 1 hour minimum | 24 hour rollback window |

---

## Consequences

### Positive Consequences ✅

1. **Pre-Production Validation**
   - Catch environment-specific bugs before production
   - Test Azure-specific configurations safely
   - Validate authentication flows end-to-end
   - Verify API integrations work correctly

2. **Safe Testing Environment**
   - Test database migrations without risk
   - Performance testing without affecting users
   - Security testing in isolated environment
   - Load testing with production-like data

3. **Improved Development Workflow**
   - Developers can test in cloud environment
   - QA has dedicated testing environment
   - Product team can preview features
   - Stakeholders can validate before release

4. **Better Incident Response**
   - Reproduce production issues safely
   - Test hotfixes before deployment
   - Validate rollback procedures
   - Debug without production access

5. **Compliance Benefits**
   - Clear separation of test/production data
   - Audit trail for deployments
   - Change validation before user impact
   - Environment access controls

6. **Configuration Confidence**
   - Environment variables tested before production
   - Connection strings validated
   - Feature flags tested in staging
   - No more localhost in production issues

### Negative Consequences ❌

1. **Additional Infrastructure Cost**
   - ~R1,400/month additional (estimated)
   - Mitigated by: Use smaller SKUs for staging
   - Mitigated by: Cosmos DB serverless (pay per use)

2. **Environment Sync Maintenance**
   - Must keep staging updated with production structure
   - Infrastructure changes must be applied to both
   - Mitigated by: Infrastructure as Code (Bicep)
   - Mitigated by: Automated environment provisioning

3. **Data Management Complexity**
   - Need to maintain test data in staging
   - Cannot use production data (PII concerns)
   - Mitigated by: Data seeding scripts
   - Mitigated by: Anonymized data copies (if needed)

4. **Deployment Pipeline Complexity**
   - Additional deployment stage
   - More CI/CD configuration
   - Mitigated by: GitHub Actions workflows
   - Mitigated by: Reusable workflow templates

---

## Implementation Plan

### Phase 1: Infrastructure Setup (Week 1-2)

- [ ] Create Bicep templates for staging environment
- [ ] Set up staging resource group in Azure
- [ ] Deploy staging App Service Plan (B1 tier)
- [ ] Deploy staging Cosmos DB (Serverless)
- [ ] Deploy staging Storage Account
- [ ] Configure staging Key Vault
- [ ] Set up staging Application Insights

### Phase 2: Configuration (Week 2-3)

- [ ] Create `appsettings.Staging.json` for all projects
- [ ] Configure staging environment variables
- [ ] Set up staging Key Vault references
- [ ] Configure staging CORS policies
- [ ] Set up staging DNS (mystira-stag.azurefd.net)

### Phase 3: CI/CD Updates (Week 3-4)

- [ ] Update GitHub Actions for staging deployment
- [ ] Add staging deployment stage to pipeline
- [ ] Configure staging health checks
- [ ] Add smoke test automation
- [ ] Set up staging monitoring alerts

### Phase 4: Validation (Week 4)

- [ ] Deploy current code to staging
- [ ] Run full test suite against staging
- [ ] Validate all integrations
- [ ] Document staging access procedures
- [ ] Train team on staging environment

---

## Cost Analysis

### Staging Environment Monthly Costs (Estimated)

| Resource | SKU | Monthly Cost (ZAR) |
|----------|-----|-------------------|
| App Service Plan | B1 (shared with both APIs) | R350 |
| Cosmos DB | Serverless (minimal usage) | R200 |
| Storage Account | Standard LRS | R50 |
| Application Insights | Pay-per-use | R100 |
| Front Door | Standard | R400 |
| Key Vault | Standard | R30 |
| Static Web App | Free tier | R0 |
| **Total Staging** | | **R1,130** |

### Cost Optimization Strategies

1. **Use Smaller SKUs**
   - Staging doesn't need production capacity
   - B1 App Service sufficient for testing
   - Can scale up temporarily for load tests

2. **Serverless Where Possible**
   - Cosmos DB Serverless (pay per request)
   - Minimal usage during non-testing periods
   - No idle capacity costs

3. **Auto-Shutdown (Optional)**
   - Consider stopping staging during off-hours
   - Restart before business hours
   - Savings: ~30% of compute costs

4. **Shared Resources Where Safe**
   - Share App Service Plan between APIs
   - Share Log Analytics workspace
   - Static Web App free tier

### Total Environment Costs

| Environment | Monthly Cost (ZAR) |
|-------------|-------------------|
| Production (current) | R1,800 |
| Production (with Front Door) | R2,690 |
| Staging | R1,130 |
| **Total** | **R3,820** |

**Increase from current**: +R2,020/month (+112%)

---

## Environment Comparison Matrix

| Aspect | Development | Staging | Production |
|--------|-------------|---------|------------|
| **Purpose** | Local development | Pre-production validation | Live users |
| **Data** | Mock/seed data | Test data | Real user data |
| **Access** | Developers | Team + stakeholders | Public |
| **Deployment** | Manual/local | Automatic on PR merge | Manual approval |
| **Monitoring** | Local logs | Full monitoring | Full monitoring + alerts |
| **Performance** | Local machine | Cloud (smaller SKUs) | Cloud (production SKUs) |
| **Availability** | On-demand | Always on | 99.9% SLA |
| **URL** | localhost:* | *-stag.azurefd.net | mystira.app |

---

## Configuration Files Structure

```
src/
├── Mystira.App.Api/
│   ├── appsettings.json           # Base settings
│   ├── appsettings.Development.json  # Local dev
│   ├── appsettings.Staging.json      # Staging
│   └── appsettings.Production.json   # Production
│
├── Mystira.App.Admin.Api/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.Staging.json
│   └── appsettings.Production.json
│
└── Mystira.App.PWA/
    └── wwwroot/
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Staging.json
        └── appsettings.Production.json
```

---

## Success Metrics

### Environment Parity
- [ ] All production services mirrored in staging
- [ ] Configuration structure identical
- [ ] Deployment process identical
- [ ] Monitoring identical

### Deployment Quality
- [ ] 100% of deployments go through staging first
- [ ] Zero environment-specific bugs in production
- [ ] < 5 minute staging deployment time
- [ ] Automated rollback capability tested

### Team Adoption
- [ ] All team members have staging access
- [ ] Staging used for all feature validation
- [ ] QA testing performed in staging
- [ ] Stakeholder demos use staging

---

## Related Decisions

- **ADR-0005**: Separate API and Admin API (both deployed to staging)
- **ADR-0007**: Azure Front Door (staging will have its own Front Door)

---

## References

- [Azure Environments Best Practices](https://learn.microsoft.com/azure/architecture/framework/devops/release-engineering-environments)
- [GitHub Actions Environments](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
- [12-Factor App - Dev/Prod Parity](https://12factor.net/dev-prod-parity)
- [Azure DevOps Release Pipelines](https://learn.microsoft.com/azure/devops/pipelines/release/)

---

## Notes

- Start with minimal staging resources; scale up as needed
- Consider data seeding strategy for consistent test data
- Document staging access and credentials in team wiki
- Set up staging alerts but with lower sensitivity than production
- Staging database should never contain real user data

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
