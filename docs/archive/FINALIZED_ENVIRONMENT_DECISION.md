# FINALIZED DECISION: Environment Strategy

**Date**: 2025-12-08  
**Status**: ✅ APPROVED - Final Decision  
**Version**: 2.0 (Updated based on extended analysis)

---

## Executive Decision

### ✅ APPROVED STRATEGY: Hybrid Approach with SWA Preview Safety Net

**Environment Configuration**:
- **Dev**: Azure App Service (debugging & integration)
- **Staging**: Azure Static Web Apps (production parity)
- **Production**: Azure Static Web Apps (production)

**Parity Safety Net**:
- SWA Preview environments on every PR
- Automated smoke tests for SWA-specific behaviors
- Optional: Nightly "dev-swa" canary deployment

---

## Rationale for Hybrid Approach

### Why Dev on App Service is Acceptable

✅ **Developer Productivity**:
- Full server-side debugging (Kudu console, VS remote attach)
- Detailed HTTP logs and request inspection
- Custom middleware experimentation
- Filesystem access for advanced diagnostics
- WebJobs and background tasks for testing

✅ **Integration Testing**:
- Server-side experiments without impacting release gates
- Temporary endpoints for testing
- Flexible configuration changes
- Quick iteration cycles

✅ **Cost Control**:
- Can auto-stop outside work hours
- Scale down when not in use
- B1 plan sufficient (~R350/month justifiable for dev productivity)

### Why Staging/Prod on SWA is Critical

✅ **Production Parity** (Non-negotiable):
- Staging must mirror Production exactly
- Edge routing, CDN, auth must match
- Service worker and PWA behaviors validated
- No false confidence in pre-production validation

✅ **Release Confidence**:
- Staging is the final gate before Production
- Must catch SWA-specific issues
- Cannot have platform differences

---

## Risk Mitigation Strategy

### Risk 1: SWA-Specific Behaviors Not Validated in Dev

**Issue**: Edge routing, `staticwebapp.config.json`, platform auth, CDN caching not tested in Dev

**Mitigation**:
1. ✅ **PR SWA Preview Environments** (MANDATORY)
   - Every PR automatically deploys to SWA Preview
   - Developers test on Preview before merge
   - Catches SWA-specific issues early

2. ✅ **Automated Smoke Tests** (REQUIRED)
   ```yaml
   # In PR workflow
   - name: Run SWA Smoke Tests
     run: |
       # Test routing
       curl -f https://preview.azurestaticapps.net/ || exit 1
       curl -f https://preview.azurestaticapps.net/adventures || exit 1
       
       # Test headers
       curl -I https://preview.azurestaticapps.net/_framework/blazor.wasm | grep -i "cache-control"
       
       # Test auth endpoints (if applicable)
       curl https://preview.azurestaticapps.net/.auth/me
   ```

3. 🔄 **Optional: Nightly Dev-SWA Canary** (NICE TO HAVE)
   - Deploy `dev` branch to a separate SWA instance nightly
   - Run comprehensive smoke tests
   - Alert team if issues detected

### Risk 2: Auth and Header Differences

**Issue**: App Service Easy Auth ≠ SWA platform auth; middleware headers ≠ edge headers

**Mitigation**:
1. ✅ **Shared Auth Abstraction**
   - Keep auth logic in Application layer (already done)
   - Use `IEmailService`, `IJwtService` interfaces
   - Platform-specific implementations in Infrastructure layer

2. ✅ **Validate Headers in PR Tests**
   ```yaml
   - name: Validate SWA Headers
     run: |
       # Check security headers
       curl -I https://preview.azurestaticapps.net/ | grep -i "x-content-type-options"
       curl -I https://preview.azurestaticapps.net/ | grep -i "x-frame-options"
       
       # Check Blazor environment
       curl https://preview.azurestaticapps.net/ | grep "Blazor-Environment"
   ```

3. ✅ **Document Differences**
   - Create `docs/AUTH_DIFFERENCES.md`
   - List App Service vs SWA auth behaviors
   - Update during code reviews

### Risk 3: Caching and PWA Update Behavior

**Issue**: SWA CDN + service worker ≠ App Service single-tier caching

**Mitigation**:
1. ✅ **Test PWA Updates in PR Preview**
   ```javascript
   // Test script to validate service worker
   async function testServiceWorkerUpdate() {
     const registration = await navigator.serviceWorker.register('/service-worker.js');
     await registration.update();
     console.log('Service worker updated successfully');
   }
   ```

2. ✅ **Version Service Workers**
   - Already done via `version.json` with build number
   - Update on every deployment
   - Cache-Control: no-cache for `service-worker.js`

3. ✅ **Validate Cache Rules in PR**
   ```bash
   # Check cache headers on static assets
   curl -I https://preview.azurestaticapps.net/_framework/blazor.wasm | grep "immutable"
   curl -I https://preview.azurestaticapps.net/index.html | grep "no-cache"
   ```

### Risk 4: Operational Overhead

**Issue**: Two deployment mechanisms, dual secrets, different failure modes

**Mitigation**:
1. ✅ **Parameterized Workflows** (IMPLEMENT)
   ```yaml
   # .github/workflows/deploy-pwa.yml (reusable)
   name: Deploy PWA (Reusable)
   
   on:
     workflow_call:
       inputs:
         environment:
           required: true
           type: string
         platform:
           required: true
           type: string  # 'swa' or 'appservice'
       secrets:
         DEPLOY_TOKEN:
           required: true
   
   jobs:
     deploy-swa:
       if: inputs.platform == 'swa'
       # ... SWA deployment steps
     
     deploy-appservice:
       if: inputs.platform == 'appservice'
       # ... App Service deployment steps
   ```

2. ✅ **GitHub Environments with Scoped Secrets**
   - `dev` environment: `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`
   - `staging` environment: `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING`
   - `production` environment: `AZURE_STATIC_WEB_APPS_API_TOKEN_PROD`

3. ✅ **Centralized Deployment Docs**
   - Update `docs/DEPLOYMENT.md` with both workflows
   - Include troubleshooting for each platform
   - Maintain runbooks for common issues

---

## Implementation Plan

### Phase 1: Immediate (Keep Current Setup) ✅

**Keep**:
- ✅ Dev: App Service (provides debugging value)
- ✅ Prod: SWA (already working well)

**Change**:
- ✅ Staging: Migrate from App Service to SWA (REQUIRED for production parity)

**Rationale**: Staging must match Prod; Dev can differ if properly mitigated

### Phase 2: Add PR Preview Safety Net (CRITICAL) 🚨

**Required Actions**:

1. **Enable SWA PR Previews**
   ```yaml
   # In all SWA workflows
   on:
     pull_request:
       types: [opened, synchronize, reopened, closed]
   ```

2. **Add Automated Smoke Tests**
   ```yaml
   # .github/workflows/swa-preview-tests.yml
   name: SWA Preview Smoke Tests
   
   on:
     pull_request:
       types: [opened, synchronize, reopened]
   
   jobs:
     smoke-tests:
       runs-on: ubuntu-latest
       steps:
         - name: Wait for Preview Deployment
           run: sleep 60  # Wait for SWA preview to be ready
         
         - name: Test Routing
           run: |
             curl -f ${{ env.PREVIEW_URL }}/ || exit 1
             curl -f ${{ env.PREVIEW_URL }}/adventures || exit 1
         
         - name: Test Static Assets
           run: |
             curl -f ${{ env.PREVIEW_URL }}/_framework/blazor.wasm || exit 1
         
         - name: Test Headers
           run: |
             curl -I ${{ env.PREVIEW_URL }}/ | grep "Blazor-Environment"
             curl -I ${{ env.PREVIEW_URL }}/_framework/blazor.wasm | grep "immutable"
         
         - name: Test PWA Manifest
           run: |
             curl -f ${{ env.PREVIEW_URL }}/manifest.json || exit 1
   ```

3. **Update PR Template**
   ```markdown
   ## Testing Checklist
   - [ ] Tested on Dev (App Service)
   - [ ] Tested on SWA Preview (check preview URL in PR comments)
   - [ ] Verified routing works on SWA
   - [ ] Verified auth (if applicable)
   - [ ] Checked PWA service worker updates
   ```

### Phase 3: Optional Enhancements (NICE TO HAVE) 🔄

1. **Nightly Dev-SWA Canary**
   ```yaml
   # .github/workflows/dev-swa-canary.yml
   name: Dev SWA Canary
   
   on:
     schedule:
       - cron: '0 2 * * *'  # 2 AM daily
     workflow_dispatch:
   
   jobs:
     deploy-and-test:
       # Deploy dev branch to separate SWA
       # Run comprehensive smoke tests
       # Notify team if failures
   ```

2. **SWA Parity Validation**
   - Periodic comparison of Dev vs Staging behavior
   - Document any intentional differences
   - Alert on unexpected divergence

---

## Updated Environment Matrix

| Environment | Platform | Purpose | Branch | Parity Level | Cost |
|-------------|----------|---------|--------|--------------|------|
| **Dev** | App Service | Developer debugging & integration | `dev` | Different (intentional) | ~R350/mo |
| **PR Preview** | SWA | SWA behavior validation | PR branches | **Matches Prod** | Free (included) |
| **Staging** | SWA | Release candidate validation | `staging` | **Matches Prod** | Free |
| **Production** | SWA | Live users | `main` | Baseline | Standard plan |

**Key Insight**: PR Previews act as the parity safety net, ensuring SWA-specific behaviors are validated even though Dev uses App Service.

---

## Configuration Alignment Checklist

### Environment Variables (App Service vs SWA)

✅ **Ensure these match across platforms**:
```json
{
  "AzureCommunicationServices:ConnectionString": "[same across all]",
  "AzureCommunicationServices:SenderEmail": "[same across all]",
  "JwtSettings:Issuer": "[environment-specific]",
  "JwtSettings:Audience": "[environment-specific]",
  "CorsSettings:AllowedOrigins": "[include Dev App Service + SWA URLs]"
}
```

### CORS Configuration

✅ **Update API appsettings to include all URLs**:
```json
{
  "CorsSettings": {
    "AllowedOrigins": "http://localhost:7000,https://localhost:7000,https://dev-app-service.azurewebsites.net,https://dev-swa.azurestaticapps.net,https://staging.azurestaticapps.net,https://mystira.app"
  }
}
```

### staticwebapp.config.json

✅ **Keep source-controlled and validated in PR**:
- Single source of truth in repo
- Generated dynamically per environment in workflow
- Validated in PR Preview smoke tests

---

## Success Metrics

### Immediate (Email Service Fix)
- ✅ Both APIs build successfully
- ✅ All tests pass
- ✅ Consistent service registration

### Phase 1 (Staging Migration to SWA)
- 📋 Staging migrated to SWA
- 📋 Production parity achieved
- 📋 Cost savings: ~R350/month (Staging App Service eliminated)

### Phase 2 (PR Preview Safety Net)
- 📋 PR Previews enabled for all SWA environments
- 📋 Automated smoke tests catching SWA-specific issues
- 📋 Zero production incidents due to Dev/Staging parity issues

### Ongoing
- 📋 Developer productivity maintained (Dev App Service)
- 📋 Release confidence high (Staging = Prod)
- 📋 Operational overhead manageable (parameterized workflows)

---

## Decision Matrix (Final)

| Factor | Dev App Service | Dev SWA | Hybrid (Approved) |
|--------|----------------|---------|-------------------|
| Developer Productivity | ✅ Excellent | ⚠️ Limited | ✅ Excellent (App Service) |
| Production Parity | ❌ Poor | ✅ Excellent | ✅ Good (PR Previews) |
| Debugging Tools | ✅ Full access | ⚠️ Limited | ✅ Full access (Dev) |
| Release Confidence | ❌ Low | ✅ High | ✅ High (Staging=Prod) |
| Operational Complexity | ⚠️ Medium | ✅ Low | ⚠️ Medium (mitigated) |
| Cost | ~R350/mo | Free | ~R350/mo (Dev only) |
| SWA Validation | ❌ None | ✅ Always | ✅ PR Previews |

**Winner**: **Hybrid Approach** - Best balance of developer productivity and release confidence

---

## Action Items (Priority Order)

### 🚨 CRITICAL (Do First)
1. ✅ Merge email service consistency fix (this PR)
2. 📋 Migrate Staging from App Service to SWA (follow STAGING_MIGRATION_GUIDE.md)
3. 📋 Enable PR SWA Previews on all PRs
4. 📋 Add automated smoke tests for PR Previews

### ⚠️ HIGH PRIORITY (Do Next)
5. 📋 Create reusable deployment workflow
6. 📋 Update PR template with SWA testing checklist
7. 📋 Document auth differences (App Service vs SWA)
8. 📋 Align CORS configuration across all environments

### 🔄 NICE TO HAVE (Future)
9. 📋 Implement nightly Dev-SWA canary
10. 📋 Add SWA parity validation dashboard
11. 📋 Consider auto-stop for Dev App Service (cost optimization)

---

## Cost Analysis (Final)

| Component | Current | After Phase 1 | After All |
|-----------|---------|---------------|-----------|
| Dev App Service | R0 (SWA) | R0 (SWA) | **+R350/mo** (App Service) |
| Staging App Service | R350/mo | **R0** (SWA Free) | R0 |
| Staging SWA | R0 | R0 | R0 |
| Prod SWA | Standard | Standard | Standard |
| **Net Change** | — | **-R350/mo** | **±R0** |

**Insight**: Hybrid approach is cost-neutral (save on Staging, spend on Dev) while maximizing both developer productivity and release confidence.

---

## References

- [Original Parity Analysis](docs/ENVIRONMENT_PARITY_ANALYSIS.md)
- [Staging Migration Guide](docs/STAGING_MIGRATION_GUIDE.md)
- [Azure Static Web Apps - Preview Environments](https://learn.microsoft.com/en-us/azure/static-web-apps/review-publish-pull-requests)
- [GitHub Actions - Reusable Workflows](https://docs.github.com/en/actions/using-workflows/reusing-workflows)

---

## Approval & Sign-off

**Decision**: ✅ **APPROVED - Hybrid Approach**

**Conditions**:
- ✅ Staging MUST be migrated to SWA (production parity)
- ✅ PR Previews MUST be enabled (parity safety net)
- ✅ Automated smoke tests MUST be implemented

**Timeline**:
- Week 1: Staging migration (2 hours)
- Week 2: PR Preview setup (4 hours)
- Week 3: Smoke tests implementation (4 hours)
- Week 4: Validation and monitoring

**Document Version**: 2.0 (Final)  
**Status**: ✅ Approved for Implementation  
**Date**: 2025-12-08
