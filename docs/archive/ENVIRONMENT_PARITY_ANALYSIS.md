# Environment Parity Analysis & Recommendations

## Executive Summary

**Current State**: Mystira.App has an **environment parity issue** - Staging uses Azure App Service while Dev and Prod use Azure Static Web Apps (SWA). This creates false confidence in pre-production validation.

**Risk**: Changes validated on App Service Staging may behave differently on SWA Production due to fundamental hosting differences (edge routing, CDN behavior, auth mechanisms, caching, etc.).

**Recommendation**: Migrate Staging to Azure Static Web Apps to achieve production parity, OR implement a dual-track approach with separate Integration Sandbox.

---

## Current Environment Architecture

### PWA/Frontend Environments

| Environment | Hosting Platform | Deployment Method | Branch | Status |
|-------------|-----------------|-------------------|--------|---------|
| **Dev** | Azure Static Web Apps | `Azure/static-web-apps-deploy@v1` | `dev` | ✅ SWA |
| **Staging** | Azure App Service | `azure/webapps-deploy@v2` | `staging` | ⚠️ App Service |
| **Production** | Azure Static Web Apps | `Azure/static-web-apps-deploy@v1` | `main` | ✅ SWA |

### API Environments

| Environment | Platform | Status |
|-------------|----------|--------|
| **Dev** | Azure App Service | Consistent |
| **Staging** | Azure App Service | Consistent |
| **Production** | Azure App Service | Consistent |

### Key Observation

✅ **APIs**: Consistent across all environments (all use App Service)  
⚠️ **PWA/Frontend**: **INCONSISTENT** - Staging differs from Dev/Prod

---

## Critical Differences Between SWA and App Service

### 1. Hosting Model
- **SWA**: Global CDN edge network, static asset hosting, automatic global distribution
- **App Service**: Single-region server, no built-in global CDN (requires Azure Front Door)

### 2. Routing & Configuration
- **SWA**: Uses `staticwebapp.config.json` with edge-level routing, headers, redirects
- **App Service**: Uses `web.config`, middleware, no edge routing

### 3. Caching Behavior
- **SWA**: Multi-level CDN caching, edge-based cache control headers
- **App Service**: Single-tier caching, server-side only

### 4. Authentication
- **SWA**: Built-in platform auth with `/.auth/` endpoints, role-based claims
- **App Service**: Easy Auth or custom middleware (different implementation)

### 5. PWA Service Worker Behavior
- **SWA**: Edge CDN affects offline caching, version updates, static asset delivery
- **App Service**: Direct server delivery, different cache invalidation patterns

### 6. Deployment & CI/CD
- **SWA**: GitHub integration, PR preview environments, automatic rollbacks
- **App Service**: Publish profiles, deployment slots, manual rollback process

### 7. CORS & Headers
- **SWA**: Edge-level CORS handling via `staticwebapp.config.json`
- **App Service**: Server middleware, different header injection points

---

## Risks of Current Architecture

### ❌ **Environment Parity Issues**

1. **False Confidence**: Staging passes may not predict Prod behavior
   - Edge routing rules in Prod not tested in Staging
   - CDN caching behavior differs
   - Service worker cache strategies behave differently

2. **Auth & Security**: Different auth flows could hide issues
   - SWA platform auth vs App Service middleware
   - CORS handling differences
   - Security header injection points differ

3. **Performance**: Cannot validate Prod performance characteristics
   - CDN edge caching not tested
   - Global distribution latency not measured
   - Cache hit ratios differ

4. **PWA Offline Behavior**: Service worker validation incomplete
   - Cache-Control headers handled differently
   - Update mechanisms vary
   - Version rollout strategies differ

### ⚠️ **Operational Overhead**

1. **Dual Deployment Pipelines**: Maintenance burden
   - Two different workflow files
   - Two sets of secrets (SWA tokens vs publish profiles)
   - Different failure modes to troubleshoot

2. **Configuration Drift**: 
   - `staticwebapp.config.json` only tested in Dev/Prod
   - App Service `web.config` only used in Staging
   - Route changes must be validated twice

3. **Cost Inefficiency**:
   - App Service B1 plan: ~R350/month for limited value
   - SWA Free tier available for Staging

---

## Recommended Solutions

### 🎯 **Option 1: Standardize on SWA (RECOMMENDED)**

**Goal**: Achieve production parity by migrating Staging to SWA

#### Implementation Steps

1. **Create Staging SWA Resource**
   ```bash
   # In Azure Portal or via Bicep
   - Resource: Static Web App (Free or Standard tier)
   - Region: Same as Prod for consistency
   - Branch: staging
   ```

2. **Create New GitHub Workflow**
   - Copy `azure-static-web-apps-blue-water-0eab7991e.yml` (Prod)
   - Rename to `azure-static-web-apps-staging.yml`
   - Update:
     - Branch trigger: `staging`
     - Environment name: "Staging"
     - SWA token secret name
     - Blazor-Environment header: "Staging"

3. **Generate `staticwebapp.config.json` for Staging**
   ```json
   {
     "globalHeaders": {
       "Blazor-Environment": "Staging"
     },
     "navigationFallback": { /* same as Prod */ },
     "routes": [ /* same as Prod */ ]
   }
   ```

4. **Update GitHub Secrets**
   - Add: `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING`
   - Source: Azure Portal → Static Web App → Deployment tokens

5. **Decommission App Service**
   - Delete `mystira-app-staging-pwa` App Service
   - Remove `mystira-app-pwa-cicd-staging.yml` workflow
   - Remove `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` secret

#### Benefits
✅ Production parity - Staging mirrors Prod exactly  
✅ Simplified operations - One deployment method  
✅ Cost savings - SWA Free tier vs App Service ~R350/mo  
✅ Better testing - Edge routing, CDN, auth validated  
✅ PR preview environments - SWA feature unavailable in App Service  

#### Timeline
- Setup: 1-2 hours
- Testing: 1 day
- Cutover: Immediate (delete old workflow)

---

### 🔧 **Option 2: Dual-Track (Advanced Use Case)**

**Use if**: Need server-side debugging capabilities not available in SWA

#### Architecture

1. **Staging (Production-Parity)**: Azure Static Web Apps
   - Purpose: Release candidate validation
   - Branch: `staging`
   - Mirrors Prod exactly
   - User-facing pre-production environment

2. **Integration Sandbox**: Azure App Service (keep existing)
   - Purpose: Server-side experiments, debugging, middleware testing
   - Branch: `integration` or `sandbox`
   - Internal-only (IP allowlist or AAD auth)
   - Retained for advanced diagnostics

#### Implementation
- Rename current `staging` → `integration-sandbox`
- Create new SWA Staging as in Option 1
- Update workflows to deploy `integration` branch to App Service
- Add IP restrictions to App Service for internal access only

#### Benefits
✅ Production parity via SWA Staging  
✅ Retain server-side debugging capabilities  
✅ Clear separation of concerns  

#### Costs
- SWA Staging: Free tier
- App Service Sandbox: ~R350/mo (justified if actively used)

---

### ❌ **Option 3: Keep Current (NOT RECOMMENDED)**

If keeping App Service Staging:

1. **Add Parity Checks**
   ```yaml
   - name: Deploy to Throwaway SWA for Parity Testing
     # Deploy staging build to temporary SWA
     # Run smoke tests
     # Validate routing, headers, caching
   ```

2. **Document Known Differences**
   - Create checklist of SWA-specific behaviors
   - Manual verification before Prod deploy

3. **Cost**: Ongoing ~R350/mo + operational burden

**Why Not Recommended**: Complexity without benefit; half-measures don't solve parity issue

---

## Implementation Plan (Option 1 - Recommended)

### Phase 1: Create Staging SWA (30 min)
- [ ] Create Azure Static Web App resource via Portal/Bicep
- [ ] Copy deployment token to GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING`

### Phase 2: Create Workflow (30 min)
- [ ] Copy `azure-static-web-apps-blue-water-0eab7991e.yml` → `azure-static-web-apps-staging.yml`
- [ ] Update branch trigger to `staging`
- [ ] Update environment name to "Staging"
- [ ] Update Blazor-Environment header to "Staging"
- [ ] Update SWA token secret reference

### Phase 3: Test Deployment (1 hour)
- [ ] Push to `staging` branch
- [ ] Verify workflow runs successfully
- [ ] Test routing with `staticwebapp.config.json`
- [ ] Verify Blazor environment detection
- [ ] Test PWA service worker updates
- [ ] Validate API connectivity

### Phase 4: Decommission App Service (15 min)
- [ ] Delete `mystira-app-pwa-cicd-staging.yml` workflow
- [ ] Remove `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` secret
- [ ] Delete `mystira-app-staging-pwa` App Service resource

### Phase 5: Documentation (15 min)
- [ ] Update deployment docs
- [ ] Update architecture diagrams
- [ ] Document SWA-specific behaviors

---

## Validation Checklist (Post-Migration)

### SWA Staging Deployment
- [ ] Workflow triggers on `staging` branch push
- [ ] Build completes successfully
- [ ] Deployment to SWA succeeds
- [ ] Custom domain configured (if applicable)

### Functional Validation
- [ ] Index page loads correctly
- [ ] Blazor environment detected as "Staging"
- [ ] Routing works (`/adventures`, `/profile`, etc.)
- [ ] API calls succeed (CORS configured)
- [ ] Authentication flows work
- [ ] Service worker registers correctly
- [ ] PWA offline mode functions

### SWA-Specific Features
- [ ] `staticwebapp.config.json` routes applied
- [ ] Cache-Control headers set correctly
- [ ] CORS headers present
- [ ] WASM files served with correct MIME types
- [ ] CDN edge caching working

### Performance
- [ ] Initial load time acceptable
- [ ] Static assets cached at edge
- [ ] API latency similar to Prod
- [ ] PWA update mechanism smooth

---

## Cost Comparison

| Scenario | Monthly Cost (ZAR) | Notes |
|----------|-------------------|-------|
| Current (App Service Staging) | ~R350 | B1 tier |
| Option 1 (SWA Free Tier) | R0 | 100GB bandwidth/mo free |
| Option 1 (SWA Standard) | ~R0-R200 | Pay for excess bandwidth only |
| Option 2 (Dual Track) | ~R350 | Keep App Service for sandbox |

**Recommendation**: Start with SWA Free tier; upgrade to Standard only if bandwidth exceeded.

---

## Decision Matrix

| Factor | Current (App Service) | Option 1 (SWA Only) | Option 2 (Dual Track) |
|--------|----------------------|---------------------|---------------------|
| Production Parity | ❌ Poor | ✅ Excellent | ✅ Excellent |
| Operational Complexity | ⚠️ Medium | ✅ Low | ⚠️ High |
| Cost | ⚠️ ~R350/mo | ✅ Free | ⚠️ ~R350/mo |
| Debugging Capabilities | ✅ Full server access | ⚠️ SWA logs only | ✅ Sandbox available |
| CI/CD Maintenance | ⚠️ Dual pipelines | ✅ Single pipeline | ❌ Dual pipelines |
| PWA Validation | ❌ Incomplete | ✅ Complete | ✅ Complete |

**Winner**: **Option 1 (SWA Only)** - Best balance of parity, simplicity, and cost

---

## Next Steps

1. **Immediate**: Review and approve this document
2. **Week 1**: Implement Option 1 (SWA Staging migration)
3. **Week 2**: Validate and monitor Staging SWA
4. **Week 3**: Decommission App Service Staging
5. **Ongoing**: Monitor for any SWA-specific issues

---

## References

- [Azure Static Web Apps Documentation](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [SWA Configuration Reference](https://learn.microsoft.com/en-us/azure/static-web-apps/configuration)
- [SWA vs App Service Comparison](https://learn.microsoft.com/en-us/azure/static-web-apps/overview)
- [Blazor WASM on SWA Best Practices](https://learn.microsoft.com/en-us/azure/static-web-apps/deploy-blazor)

---

## Appendix: Sample Staging SWA Workflow

```yaml
name: PWA CI/CD - Staging Environment (SWA)

on:
  push:
    branches: [staging]
  workflow_dispatch:

concurrency:
  group: swa-staging-${{ github.ref }}
  cancel-in-progress: false

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    name: Build and Deploy to Staging SWA
    steps:
      - uses: actions/checkout@v3
        with:
          submodules: true
          lfs: false

      - name: Generate staticwebapp.config.json for Staging
        run: |
          cat << 'EOF' > src/Mystira.App.PWA/wwwroot/staticwebapp.config.json
          {
            "globalHeaders": {
              "Blazor-Environment": "Staging"
            },
            "navigationFallback": {
              "rewrite": "/index.html"
            }
          }
          EOF

      - name: Update version.json
        run: |
          VERSION="1.0.${{ github.run_number }}"
          BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
          COMMIT_SHA="${{ github.sha }}"
          cat > src/Mystira.App.PWA/wwwroot/version.json << EOF
          {
            "version": "${VERSION}",
            "buildDate": "${BUILD_DATE}",
            "commitSha": "${COMMIT_SHA:0:7}"
          }
          EOF

      - name: Build And Deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "./src/Mystira.App.PWA"
          output_location: "wwwroot"
          skip_api_build: true
          skip_app_build: false
          production_branch: "staging"
```

---

**Document Version**: 1.0  
**Date**: 2025-12-08  
**Author**: Copilot (based on Junie's analysis)  
**Status**: Proposal - Pending Approval
