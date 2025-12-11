# Environment Strategy

**Last Updated**: 2025-12-10
**Status**: All-SWA Standardization

---

## Current Environment Configuration

| Environment | Platform | Status | Workflow |
|-------------|----------|--------|----------|
| **Dev** | Azure Static Web Apps | ✅ Active | `azure-static-web-apps-dev-san-swa-mystira-app.yml` |
| **Staging** | Azure Static Web Apps | ✅ Active | `azure-static-web-apps-staging.yml` |
| **Production** | Azure Static Web Apps | ✅ Active | `azure-static-web-apps-blue-water-0eab7991e.yml` |

---

## Strategy: All-SWA Standardization

All three environments use Azure Static Web Apps for maximum production parity.

### Benefits

1. **Maximum Production Parity**
   - All environments identical
   - Dev and Staging accurately predict Prod behavior
   - No platform-specific differences

2. **Operational Simplicity**
   - Single deployment method (Azure SWA)
   - One set of workflows and patterns
   - Unified troubleshooting approach

3. **Cost Efficiency**
   - Dev: Free (SWA Free tier)
   - Staging: Free (SWA Free tier)
   - Production: Standard plan
   - **Total PWA hosting: ~R350/month**

4. **No Hybrid Complexity**
   - No need to maintain two different platform types
   - No environment-specific behaviors
   - Simpler for team to understand

---

## Environment URLs

| Environment | PWA URL | API URL |
|-------------|---------|---------|
| **Dev** | `https://dev-san-swa-mystira-app.azurestaticapps.net` | `https://dev-san-app-mystira-api.azurewebsites.net` |
| **Staging** | `https://staging-euw-swa-mystira-app.azurestaticapps.net` | `https://staging-euw-app-mystira-api.azurewebsites.net` |
| **Production** | `https://mystira.app` | `https://prod-wus-app-mystira-api.azurewebsites.net` |

---

## Deployment Workflows

### PWA (Static Web Apps)
- **Trigger**: Push to respective branch
- **Dev**: `main` branch → Dev SWA
- **Staging**: `staging` branch → Staging SWA
- **Production**: Release tag → Prod SWA

### API (App Services)
- **Trigger**: Push or manual dispatch
- **Dev**: `main` branch → Dev App Service
- **Staging**: `staging` branch → Staging App Service
- **Production**: Release tag → Prod App Service

---

## Branch Strategy

| Branch | Environment | Auto-Deploy |
|--------|-------------|-------------|
| `main` | Dev | Yes |
| `staging` | Staging | Yes |
| `release/*` | Production | Manual approval |

---

## Configuration by Environment

### Environment Variables

| Variable | Dev | Staging | Prod |
|----------|-----|---------|------|
| `ASPNETCORE_ENVIRONMENT` | Development | Staging | Production |
| `CosmosDb` | Dev DB | Staging DB | Prod DB |
| `BlobStorage` | Dev Storage | Staging Storage | Prod Storage |

### Feature Flags

| Feature | Dev | Staging | Prod |
|---------|-----|---------|------|
| Swagger UI | ✅ Enabled | ✅ Enabled | ❌ Disabled |
| Debug Logging | ✅ Enabled | ⚠️ Limited | ❌ Disabled |
| Rate Limiting | ⚠️ Relaxed | ✅ Enabled | ✅ Enabled |

---

## Monitoring

- **Application Insights**: All environments connected
- **Health Checks**: `GET /health` endpoint on all APIs
- **SWA Diagnostics**: Azure Portal → Static Web Apps → Diagnostics

---

## Related Documentation

- [Azure Naming Conventions](azure-naming-conventions.md)
- [CI/CD Architecture](devops/ci-cd-architecture.md)
- [Branching Strategy](devops/branching-strategy.md)

---

**Maintained By**: DevOps Team
