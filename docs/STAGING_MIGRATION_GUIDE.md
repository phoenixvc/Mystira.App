# Staging Environment Migration Guide
## From Azure App Service to Azure Static Web Apps

**Date**: 2025-12-08  
**Status**: Ready for Implementation  
**Estimated Time**: 2 hours  

---

## Prerequisites

- [ ] Azure subscription access
- [ ] GitHub repository admin access
- [ ] Ability to create Azure resources
- [ ] Access to GitHub Secrets configuration

---

## Step-by-Step Migration

### Phase 1: Create Azure Static Web App Resource

#### Option A: Azure Portal (Easiest)

1. **Navigate to Azure Portal**
   - Go to https://portal.azure.com
   - Sign in with your Azure account

2. **Create Static Web App**
   ```
   - Click "Create a resource"
   - Search for "Static Web App"
   - Click "Create"
   ```

3. **Configure Basic Settings**
   ```
   Subscription: [Your subscription]
   Resource Group: [Same as other Mystira resources]
   Name: mystira-app-staging-swa
   Region: Same as production (for consistency)
   Plan type: Free (or Standard if needed)
   ```

4. **Deployment Configuration**
   ```
   Source: GitHub
   Organization: phoenixvc
   Repository: Mystira.App
   Branch: staging
   Build Presets: Blazor
   App location: ./src/Mystira.App.PWA
   Api location: (leave empty)
   Output location: wwwroot
   ```

5. **Review and Create**
   - Click "Review + Create"
   - Click "Create"
   - Wait for deployment to complete (1-2 minutes)

6. **Get Deployment Token**
   - Open the newly created Static Web App resource
   - Go to "Overview" → "Manage deployment token"
   - Copy the token (you'll need this for GitHub)
   - **IMPORTANT**: Store this securely - treat it like a password

#### Option B: Azure CLI (Advanced)

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="rg-mystira-app"
LOCATION="southafricanorth"  # or your preferred region
SWA_NAME="mystira-app-staging-swa"

# Create Static Web App
az staticwebapp create \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --branch staging \
  --app-location "./src/Mystira.App.PWA" \
  --output-location "wwwroot" \
  --token $GITHUB_TOKEN \
  --sku Free

# Get deployment token
az staticwebapp secrets list \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv
```

---

### Phase 2: Configure GitHub Secrets

1. **Navigate to GitHub Repository**
   - Go to https://github.com/phoenixvc/Mystira.App
   - Click "Settings" → "Secrets and variables" → "Actions"

2. **Add New Secret**
   ```
   Name: AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING
   Value: [Paste the deployment token from Phase 1]
   ```

3. **Verify Existing Secrets**
   - Ensure these exist (needed for workflow):
     - `GITHUB_TOKEN` (automatically provided by GitHub)

---

### Phase 3: Deploy New Workflow

**Note**: The workflow file has already been created at:
`.github/workflows/azure-static-web-apps-staging.yml`

1. **Verify Workflow File**
   ```bash
   # From repository root
   cat .github/workflows/azure-static-web-apps-staging.yml
   ```

2. **Test Deployment**
   ```bash
   # Push to staging branch to trigger workflow
   git checkout staging
   git pull origin staging
   
   # Make a small change to trigger deployment (if needed)
   echo "# Test deployment" >> README.md
   git add README.md
   git commit -m "Test: Trigger Staging SWA deployment"
   git push origin staging
   ```

3. **Monitor Workflow**
   - Go to GitHub → Actions tab
   - Find "PWA CI/CD - Staging Environment (SWA)" workflow
   - Click on the running workflow
   - Monitor build and deployment steps
   - Wait for completion (5-10 minutes)

4. **Verify Deployment**
   - Go to Azure Portal → Your Static Web App resource
   - Note the URL (e.g., `https://[random-words].[random-number].azurestaticapps.net`)
   - Open URL in browser
   - Verify app loads correctly
   - Check browser console for "Blazor Environment: Staging"

---

### Phase 4: Validation Testing

Run through this checklist to ensure everything works:

#### Basic Functionality
- [ ] App loads without errors
- [ ] Navigation works (all routes accessible)
- [ ] Static assets load (CSS, JS, images)
- [ ] Blazor environment detected as "Staging" (check browser console)

#### API Connectivity
- [ ] API calls succeed (check Network tab)
- [ ] CORS headers present
- [ ] Authentication works (if applicable)

#### PWA Features
- [ ] Service worker registers successfully
- [ ] Offline mode works (disconnect network, reload)
- [ ] App install prompt appears (on mobile/supported browsers)
- [ ] Version.json accessible at `/version.json`

#### SWA-Specific Features
- [ ] `/_framework/*` files served with correct cache headers
- [ ] WASM files have correct MIME type (`application/wasm`)
- [ ] 404 pages redirect to index.html (SPA fallback)

#### Performance
- [ ] Initial load time < 3 seconds
- [ ] Static assets cached (check Network tab - 304 responses on reload)
- [ ] CDN edge caching working (check response headers for `X-Azure-Ref`)

---

### Phase 5: Decommission Old App Service

**⚠️ CRITICAL: Only proceed after validation in Phase 4 is complete!**

1. **Disable Old Workflow**
   ```bash
   # Rename old workflow to prevent accidental runs
   git mv .github/workflows/mystira-app-pwa-cicd-staging.yml \
          .github/workflows/mystira-app-pwa-cicd-staging.yml.disabled
   
   git add .github/workflows/
   git commit -m "Disable old App Service staging workflow"
   git push origin staging
   ```

2. **Remove Old GitHub Secret**
   - Go to GitHub → Settings → Secrets and variables → Actions
   - Find `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`
   - Click "Remove"
   - Confirm removal

3. **Delete Azure App Service Resource**
   
   **Option A: Azure Portal**
   ```
   - Go to Azure Portal
   - Find resource: mystira-app-staging-pwa
   - Click "Delete"
   - Type resource name to confirm
   - Click "Delete"
   ```
   
   **Option B: Azure CLI**
   ```bash
   RESOURCE_GROUP="rg-mystira-app"
   APP_SERVICE_NAME="mystira-app-staging-pwa"
   
   # Delete App Service
   az webapp delete \
     --name $APP_SERVICE_NAME \
     --resource-group $RESOURCE_GROUP
   
   # Delete App Service Plan (if not used by other apps)
   APP_SERVICE_PLAN="asp-mystira-app-staging"
   az appservice plan delete \
     --name $APP_SERVICE_PLAN \
     --resource-group $RESOURCE_GROUP
   ```

4. **Verify Deletion**
   - Check Azure Portal - resource should be gone
   - Check Azure costs - no more App Service charges (~R350/mo savings)

---

### Phase 6: Update Documentation

1. **Update Architecture Docs**
   ```bash
   # Update any architecture diagrams
   # Update deployment documentation
   # Update troubleshooting guides
   ```

2. **Update Team**
   - Notify team of new Staging URL
   - Update any bookmarks or saved links
   - Update CI/CD documentation
   - Update runbooks

3. **Commit Documentation Changes**
   ```bash
   git add docs/
   git commit -m "docs: Update for Staging SWA migration"
   git push origin staging
   ```

---

## Rollback Plan

If issues arise, you can quickly rollback:

### Quick Rollback (Keep Both Running)

1. **Re-enable Old Workflow**
   ```bash
   git mv .github/workflows/mystira-app-pwa-cicd-staging.yml.disabled \
          .github/workflows/mystira-app-pwa-cicd-staging.yml
   git push origin staging
   ```

2. **Old App Service should still be running** (if not deleted yet)

3. **Investigate issues on SWA** while old App Service handles traffic

### Full Rollback (If SWA Deleted)

1. Re-create App Service following original setup
2. Restore publish profile secret
3. Re-enable App Service workflow
4. Delete SWA resource

---

## Troubleshooting

### Issue: Workflow Fails with "Invalid token"

**Solution**:
1. Go to Azure Portal → Static Web App → Manage deployment token
2. Copy the token again
3. Update GitHub Secret `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING`
4. Re-run workflow

### Issue: App loads but API calls fail (CORS errors)

**Solution**:
1. Check API CORS configuration includes SWA URL
2. Update API appsettings to include SWA staging URL in `CorsSettings:AllowedOrigins`
3. Redeploy API

### Issue: Service Worker not registering

**Solution**:
1. Check `staticwebapp.config.json` has correct Service-Worker-Allowed header
2. Verify service-worker.js is served from root
3. Clear browser cache and reload

### Issue: Blazor environment not detected correctly

**Solution**:
1. Check `staticwebapp.config.json` has `"Blazor-Environment": "Staging"` in globalHeaders
2. Verify workflow generates the config file correctly
3. Check browser Network tab for response headers on index.html

### Issue: Static assets not cached correctly

**Solution**:
1. Check `staticwebapp.config.json` routes section
2. Verify Cache-Control headers in response (Network tab)
3. CDN may take 5-10 minutes to propagate changes

---

## Post-Migration Monitoring

### Week 1: Intensive Monitoring

- [ ] Monitor deployment success rate (should be 100%)
- [ ] Check error rates in Application Insights
- [ ] Validate performance metrics (load times, API latency)
- [ ] Review user feedback for any issues

### Week 2: Performance Validation

- [ ] Compare Staging vs Prod behavior
- [ ] Validate CDN cache hit ratios
- [ ] Check for any edge routing issues
- [ ] Verify PWA update mechanisms

### Ongoing: Monthly Review

- [ ] Review SWA bandwidth usage (Free tier limit: 100GB/month)
- [ ] Check for any deployment failures
- [ ] Validate cost savings (~R350/mo)
- [ ] Ensure production parity maintained

---

## Success Criteria

✅ **Migration is successful when:**

1. Staging SWA deploys automatically on `staging` branch push
2. App loads and functions identically to production
3. All validation tests pass
4. No CORS or routing issues
5. Service worker and PWA features work correctly
6. Old App Service is decommissioned
7. Team is aware of new Staging URL
8. Documentation updated

---

## Cost Impact

**Before Migration**:
- App Service B1: ~R350/month
- Total: ~R350/month

**After Migration**:
- Azure Static Web Apps (Free tier): R0/month (100GB bandwidth)
- Total: R0/month

**Savings**: ~R350/month (~R4,200/year)

---

## Contacts & Support

**Azure Support**:
- Portal: https://portal.azure.com → Support tickets
- Docs: https://learn.microsoft.com/en-us/azure/static-web-apps/

**GitHub Support**:
- Actions: https://github.com/phoenixvc/Mystira.App/actions
- Issues: https://github.com/phoenixvc/Mystira.App/issues

**Team Contacts**:
- [Add relevant team contact information]

---

**Document Version**: 1.0  
**Last Updated**: 2025-12-08  
**Author**: Copilot  
**Status**: Ready for Implementation
