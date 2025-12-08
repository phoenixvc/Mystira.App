# ADR-0009: Use App Service for Staging PWA Deployment

**Status**: ✅ Accepted

**Date**: 2025-12-08

**Deciders**: Architecture Team, DevOps Team

**Tags**: infrastructure, deployment, pwa, staging, azure, static-web-apps, app-service

---

## Context

The Mystira.App Blazor PWA is deployed differently across the three environments:

| Environment | Deployment Method | Resource Type |
|------------|------------------|---------------|
| **Development** | Azure Static Web Apps | Static Web App (`mango-water-04fdb1c03`) |
| **Staging** | Azure App Service | App Service (`mystira-app-staging-pwa`) |
| **Production** | Azure Static Web Apps | Static Web App (`blue-water-0eab7991e`) |

This inconsistency raised questions about whether the staging environment's use of App Service instead of Static Web Apps is the correct architectural decision.

### Current State

**Development & Production**: Use Azure Static Web Apps
- Deployment via GitHub Actions using `Azure/static-web-apps-deploy@v1`
- Deployment tokens stored as GitHub secrets
- Automatic CDN, SSL, and global distribution
- Free hosting for frontend assets
- Optimized for static content delivery

**Staging**: Uses Azure App Service
- Deployment via GitHub Actions using `azure/webapps-deploy@v2`
- Publish profile stored as GitHub secret
- Requires App Service Plan with compute resources
- Standard web hosting model
- Allows server-side processing capability

### Problems Being Addressed

1. **Environment Parity Concerns**
   - Should staging mirror production exactly?
   - Does infrastructure difference introduce risk?
   - Will deployment process differences cause issues?

2. **Cost Considerations**
   - Static Web Apps: Free tier available
   - App Service: Requires paid App Service Plan (~R350/month for B1)
   - Is the additional cost justified?

3. **Deployment Complexity**
   - Different deployment methods require different secrets
   - Different GitHub Actions workflows
   - Team needs to maintain expertise in both approaches

4. **Testing Validity**
   - Does testing on App Service validate Static Web App behavior?
   - Are we testing the right production configuration?

---

## Decision

We will **continue using Azure App Service for Staging PWA deployment** instead of migrating to Static Web Apps to match Dev and Production.

### Rationale

#### 1. **Flexibility for Pre-Production Testing** ⭐ Primary Reason

App Service provides server-side capabilities that may be valuable during staging validation:

- **API Fallback Testing**: Can test server-side fallback scenarios
- **Request Inspection**: Easier to debug HTTP issues with App Service logs
- **Middleware Testing**: Can add custom middleware for monitoring/debugging
- **Hot Reload**: Faster iteration during integration testing
- **Environment Variables**: More flexible configuration without rebuild

#### 2. **Cost Optimization Through Resource Sharing**

Staging App Service Plan is **shared across multiple services**:

```
App Service Plan: mystira-app-staging (B1)
├── mystira-app-staging-api      (API)
├── mystira-app-staging-admin-api (Admin API)
└── mystira-app-staging-pwa       (PWA)
```

**Cost Analysis**:
- **Current**: R350/month for shared plan (3 services)
- **If PWA moved to Static Web App**: R350/month for plan + R0 for SWA = R350/month
- **But**: Shared plan already budgeted for APIs; PWA adds no incremental cost
- **Marginal cost of current approach**: R0 (PWA shares existing plan)

#### 3. **Deployment Simplicity**

Using App Service keeps all staging deployments consistent:

```yaml
# All three services use same deployment pattern
- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v2
  with:
    app-name: ${{ env.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.PUBLISH_PROFILE }}
```

**Benefits**:
- Consistent secret management (publish profiles)
- Consistent deployment workflow
- Single deployment method to maintain
- Easier for team to understand and troubleshoot

#### 4. **Staging-Specific Requirements**

Staging has unique needs that benefit from App Service:

- **Integration Testing**: Can add health check endpoints
- **Performance Monitoring**: Direct access to App Service metrics
- **Debug Capabilities**: Can enable detailed logging temporarily
- **Quick Fixes**: Can modify files directly for urgent debugging (not recommended, but possible)
- **Authentication Testing**: Can test authentication flows with custom configuration

#### 5. **Risk Mitigation**

The PWA is a **static client application** - deployment method doesn't affect functionality:

- Blazor WebAssembly runs entirely in the browser
- No server-side rendering or processing
- Same `wwwroot` output regardless of hosting
- API communication is identical (HTTP/HTTPS)
- Authentication flows are identical (JWT tokens)

**What we're NOT testing differently**:
- ✅ Application code (identical)
- ✅ API integration (identical)
- ✅ Authentication (identical)
- ✅ Browser behavior (identical)

**What IS different** (but acceptable for staging):
- ❌ CDN behavior (not critical for staging)
- ❌ Global distribution (staging is single region)
- ❌ Static Web App-specific features (we don't use them)

---

## Alternatives Considered

### Alternative 1: Migrate Staging to Static Web Apps ⭐ REJECTED

**Pros**:
- ✅ Perfect environment parity with Production
- ✅ Consistent deployment method across all environments
- ✅ Free tier available (no cost)
- ✅ Automatic CDN and global distribution

**Cons**:
- ❌ Requires creating new Static Web App resource
- ❌ Requires new deployment token secret
- ❌ Different deployment workflow from APIs
- ❌ Less flexibility for debugging/testing
- ❌ Cannot share resources with other services
- ❌ Migration effort with no functional benefit

**Why Rejected**: The deployment method doesn't affect PWA functionality. The additional complexity and migration effort isn't justified by the minimal benefits.

### Alternative 2: Migrate Dev/Prod to App Service ⭐ REJECTED

**Pros**:
- ✅ Consistent deployment across all environments
- ✅ Simplified secret management

**Cons**:
- ❌ Increased production costs (need dedicated App Service Plan)
- ❌ Reduced performance (no automatic CDN)
- ❌ More infrastructure to manage
- ❌ Worse user experience (slower load times)
- ❌ No global distribution

**Why Rejected**: Static Web Apps are optimal for production. Making production worse to match staging doesn't make sense.

### Alternative 3: Use Static Web Apps for All Environments ⭐ CONSIDERED

**Pros**:
- ✅ Perfect environment parity
- ✅ Free tier for all environments
- ✅ Optimal performance everywhere

**Cons**:
- ❌ Less flexibility in staging for debugging
- ❌ Cannot share resources with APIs
- ❌ Different deployment method from APIs (inconsistent)

**Why Not Chosen**: Current approach provides more value for staging without compromising production.

---

## Consequences

### Positive Consequences ✅

1. **Cost Efficiency**
   - PWA shares App Service Plan with APIs (zero marginal cost)
   - No need for additional Static Web App resources in staging
   - Optimal use of existing infrastructure

2. **Debugging Capabilities**
   - Can enable detailed logging in App Service
   - Can access streaming logs during testing
   - Can temporarily modify configuration for testing
   - Better visibility into HTTP request/response

3. **Consistent Staging Deployment**
   - All staging services use same deployment method
   - Simplified secret management (all use publish profiles)
   - Easier for team to understand
   - Reduced cognitive load

4. **Flexibility**
   - Can add server-side middleware if needed for testing
   - Can implement custom health checks
   - Can add temporary monitoring endpoints
   - More options for integration testing

5. **Production Safety**
   - Production uses optimal deployment method (Static Web Apps)
   - No compromise on production performance
   - CDN and global distribution where it matters

### Negative Consequences ❌

1. **Environment Parity Gap**
   - Staging deployment method differs from production
   - Could theoretically miss Static Web App-specific issues
   - **Mitigation**: PWA is static - hosting method doesn't affect functionality
   - **Mitigation**: All critical features tested (code, APIs, auth)

2. **Deployment Process Differences**
   - Team must understand two deployment methods
   - Different GitHub Actions workflows
   - Different secret types (publish profiles vs. deployment tokens)
   - **Mitigation**: Both methods are well-documented
   - **Mitigation**: Workflows are similar in structure

3. **Documentation Complexity**
   - Must explain why staging is different
   - Secret requirements differ by environment
   - **Mitigation**: This ADR provides clear explanation
   - **Mitigation**: Documentation updated to reflect differences

---

## Implementation Status

✅ **Already Implemented** - This ADR documents the existing decision.

### Current Configuration

**Development** (`dev` branch):
```yaml
# .github/workflows/azure-static-web-apps-dev-san-swa-mystira-app.yml
- name: Build And Deploy
  uses: Azure/static-web-apps-deploy@v1
  with:
    azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_SAN_MYSTIRA_APP }}
    repo_token: ${{ secrets.GITHUB_TOKEN }}
```

**Staging** (`staging` branch):
```yaml
# .github/workflows/mystira-app-pwa-cicd-staging.yml
- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v2
  with:
    app-name: mystira-app-staging-pwa
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_PWA }}
```

**Production** (`main` branch):
```yaml
# .github/workflows/azure-static-web-apps-blue-water-0eab7991e.yml
- name: Build And Deploy
  uses: Azure/static-web-apps-deploy@v1
  with:
    azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E }}
    repo_token: ${{ secrets.GITHUB_TOKEN }}
```

### Required Secrets by Environment

| Environment | PWA Deployment Secret | Type |
|------------|---------------------|------|
| Development | `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_SAN_MYSTIRA_APP` | Static Web App Token |
| Staging | `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_PWA` | App Service Publish Profile |
| Production | `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E` | Static Web App Token |

---

## Success Criteria

✅ **All criteria met**:

1. ✅ Staging PWA deploys successfully via App Service
2. ✅ Production PWA deploys successfully via Static Web Apps
3. ✅ Development PWA deploys successfully via Static Web Apps
4. ✅ All three environments function identically from user perspective
5. ✅ Cost optimization achieved (shared App Service Plan)
6. ✅ Team understands deployment differences
7. ✅ Documentation reflects current state

---

## Monitoring & Review

### Metrics to Track

1. **Deployment Success Rate**
   - Target: >99% successful deployments across all environments
   - Alert: If staging deployment failures increase

2. **Cost Tracking**
   - Monitor App Service Plan utilization
   - Ensure PWA doesn't cause resource contention
   - Target: <50% CPU utilization on shared plan

3. **Performance Comparison**
   - Compare staging vs. production load times
   - Ensure staging performance acceptable for testing
   - Alert: If staging becomes unusably slow

### Review Triggers

Review this decision if:
- Static Web Apps gain exclusive features we need
- App Service Plan becomes overloaded with staging services
- Team requests environment parity for specific reasons
- Cost structure changes significantly
- Performance issues emerge in staging

---

## Related Decisions

- **ADR-0008**: Separate Staging Environment Stack (established the need for staging resources)
- **ADR-0005**: Separate API and Admin API (other services sharing the App Service Plan)
- **ADR-0007**: Azure Front Door (staging has simpler routing needs)

---

## References

- [Azure Static Web Apps Documentation](https://learn.microsoft.com/azure/static-web-apps/)
- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [Blazor WebAssembly Hosting](https://learn.microsoft.com/aspnet/core/blazor/hosting-models)
- [GitHub Actions - Azure/static-web-apps-deploy](https://github.com/Azure/static-web-apps-deploy)
- [GitHub Actions - azure/webapps-deploy](https://github.com/Azure/webapps-deploy)

---

## Appendix: Technical Comparison

### Static Web Apps vs App Service for Blazor WASM

| Feature | Static Web Apps | App Service | Staging Need |
|---------|----------------|-------------|--------------|
| **Static File Hosting** | ✅ Optimized | ✅ Supported | ✅ Both work |
| **CDN** | ✅ Automatic | ❌ Separate resource | ❌ Not needed for staging |
| **Global Distribution** | ✅ Automatic | ❌ Manual setup | ❌ Not needed for staging |
| **Custom Domain** | ✅ Free SSL | ✅ Free SSL | ✅ Both work |
| **Deployment** | GitHub Action | Multiple methods | ✅ Both work |
| **Cost** | Free tier | Requires plan | ✅ Already have plan |
| **Server-side API** | Limited | ✅ Full support | ✅ More flexibility |
| **Logging** | Basic | ✅ Detailed | ✅ Better debugging |
| **Health Checks** | Basic | ✅ Custom | ✅ Better monitoring |

---

## Notes

- This decision does NOT affect application functionality
- Blazor WASM runs entirely client-side regardless of hosting
- The difference is infrastructure, not application behavior
- Focus on production optimization, staging flexibility
- Document differences clearly for team understanding

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
