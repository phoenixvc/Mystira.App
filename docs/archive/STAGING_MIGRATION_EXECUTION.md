# Staging Migration Execution Checklist

**Date**: 2025-12-08  
**Status**: 🚀 READY TO EXECUTE  
**Automated Steps**: ✅ COMPLETED  
**Manual Steps**: ⏳ PENDING  

---

## Automated Steps (✅ COMPLETED)

### 1. Disabled Old App Service Workflow
- ✅ Renamed `mystira-app-pwa-cicd-staging.yml` → `mystira-app-pwa-cicd-staging.yml.disabled`
- ✅ This prevents accidental deployments to App Service
- ✅ New SWA workflow (`azure-static-web-apps-staging.yml`) is ready to use

---

## Manual Steps Required (Execute in Order)

### Step 1: Create Azure Static Web App Resource (15 min)

**Option A: Azure Portal** (Recommended for first-time)

1. Navigate to https://portal.azure.com
2. Click "Create a resource" → Search "Static Web App" → Click "Create"
3. Configure:
   ```
   Subscription: [Your Azure subscription]
   Resource Group: [Same as other Mystira resources]
   Name: mystira-app-staging-swa
   Region: South Africa North (same as prod)
   Plan type: Free
   ```
4. Deployment details:
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
5. Click "Review + Create" → "Create"
6. Wait 1-2 minutes for deployment

**Option B: Azure CLI** (Faster for experienced users)

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="rg-mystira-app"  # Use your resource group name
LOCATION="southafricanorth"
SWA_NAME="mystira-app-staging-swa"

# Create Static Web App
az staticwebapp create \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --branch staging \
  --app-location "./src/Mystira.App.PWA" \
  --output-location "wwwroot" \
  --sku Free

# Get deployment token (save this for Step 2)
az staticwebapp secrets list \
  --name $SWA_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv
```

---

### Step 2: Configure GitHub Secret (5 min)

1. **Get Deployment Token** (if using Portal):
   - In Azure Portal, open the Static Web App resource
   - Go to "Overview" → Click "Manage deployment token"
   - Copy the token
   - ⚠️ **IMPORTANT**: Treat this like a password - don't share it

2. **Add GitHub Secret**:
   - Go to https://github.com/phoenixvc/Mystira.App/settings/secrets/actions
   - Click "New repository secret"
   - Name: `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING`
   - Value: [Paste the deployment token from step 1]
   - Click "Add secret"

---

### Step 3: Test Deployment (30 min)

1. **Trigger Workflow**:
   ```bash
   # Option A: Push to staging branch
   git checkout staging
   git pull origin staging
   git merge copilot/investigate-email-service-differences  # Merge this PR
   git push origin staging
   
   # Option B: Manual workflow dispatch
   # Go to: https://github.com/phoenixvc/Mystira.App/actions/workflows/azure-static-web-apps-staging.yml
   # Click "Run workflow" → Select "staging" branch → "Run workflow"
   ```

2. **Monitor Deployment**:
   - Go to: https://github.com/phoenixvc/Mystira.App/actions
   - Find "PWA CI/CD - Staging Environment (SWA)" workflow
   - Click on the running workflow
   - Wait for completion (5-10 minutes)

3. **Get Staging URL**:
   - In Azure Portal, open the Static Web App resource
   - Copy the URL (e.g., `https://[random-words].[random-number].azurestaticapps.net`)

4. **Validate Deployment**:
   ```bash
   # Replace with your actual staging URL
   STAGING_URL="https://your-staging-swa.azurestaticapps.net"
   
   # Test home page
   curl -I $STAGING_URL/
   
   # Test Blazor environment header
   curl -I $STAGING_URL/ | grep -i "blazor-environment"
   
   # Test routing
   curl -I $STAGING_URL/adventures
   ```

---

### Step 4: Manual Validation (30 min)

Open the Staging URL in your browser and verify:

**Basic Functionality**:
- [ ] Home page loads without errors
- [ ] Navigation works (click through main routes)
- [ ] Static assets load (images, CSS, JS)
- [ ] Blazor environment shows "Staging" in browser console

**SWA-Specific Features**:
- [ ] Deep links work (try `/adventures`, `/profile` directly)
- [ ] Browser back/forward buttons work
- [ ] Service worker registers (check Application tab in DevTools)
- [ ] PWA manifest loads (`/manifest.json`)

**API Integration**:
- [ ] API calls succeed (check Network tab)
- [ ] CORS headers present
- [ ] Authentication works (if applicable)

**Performance**:
- [ ] Initial load time < 3 seconds
- [ ] Static assets cached (check Network tab for 304 responses on reload)
- [ ] Check response headers for `X-Azure-Ref` (CDN indicator)

---

### Step 5: Decommission Old App Service (15 min)

⚠️ **ONLY proceed after Step 4 validation is complete!**

**Option A: Azure Portal**
1. Go to Azure Portal
2. Find resource: `mystira-app-staging-pwa` (App Service)
3. Click "Delete"
4. Type resource name to confirm
5. Click "Delete"

**Option B: Azure CLI**
```bash
RESOURCE_GROUP="rg-mystira-app"
APP_SERVICE_NAME="mystira-app-staging-pwa"

# Delete App Service
az webapp delete \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP

# Optional: Delete App Service Plan if not used by other apps
APP_SERVICE_PLAN="asp-mystira-app-staging"
az appservice plan delete \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP
```

---

### Step 6: Clean Up GitHub Secret (5 min)

1. Go to: https://github.com/phoenixvc/Mystira.App/settings/secrets/actions
2. Find: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`
3. Click "Remove"
4. Confirm removal

---

### Step 7: Final Verification (10 min)

1. **Verify Workflow File**:
   ```bash
   # Ensure old workflow is disabled
   ls .github/workflows/ | grep "mystira-app-pwa-cicd-staging"
   # Should show: mystira-app-pwa-cicd-staging.yml.disabled
   ```

2. **Verify GitHub Secrets**:
   - Go to: https://github.com/phoenixvc/Mystira.App/settings/secrets/actions
   - Should have: `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING` ✅
   - Should NOT have: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` ❌

3. **Verify Azure Resources**:
   - Should have: `mystira-app-staging-swa` (Static Web App) ✅
   - Should NOT have: `mystira-app-staging-pwa` (App Service) ❌

4. **Test End-to-End**:
   - Make a small change to `src/Mystira.App.PWA/`
   - Push to `staging` branch
   - Verify workflow runs automatically
   - Verify changes appear on Staging URL

---

## Troubleshooting

### Issue: Workflow fails with "Invalid token"
**Solution**:
1. Go to Azure Portal → Static Web App → Manage deployment token
2. Copy the token again
3. Update GitHub Secret `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING`
4. Re-run workflow

### Issue: App loads but API calls fail (CORS errors)
**Solution**:
1. Get Staging SWA URL
2. Update API `appsettings.Staging.json` or environment variable:
   ```json
   {
     "CorsSettings": {
       "AllowedOrigins": "...,https://your-staging-swa.azurestaticapps.net"
     }
   }
   ```
3. Redeploy API

### Issue: Service worker not registering
**Solution**:
1. Check `staticwebapp.config.json` has `Service-Worker-Allowed` header
2. Verify service-worker.js is at root
3. Clear browser cache and reload

### Issue: Blazor environment not "Staging"
**Solution**:
1. Check workflow generates `staticwebapp.config.json` with `"Blazor-Environment": "Staging"`
2. Check Network tab for response headers on `index.html`
3. Re-run workflow if needed

---

## Rollback Plan

If issues arise, you can quickly rollback:

1. **Re-enable Old Workflow**:
   ```bash
   cd .github/workflows/
   mv mystira-app-pwa-cicd-staging.yml.disabled mystira-app-pwa-cicd-staging.yml
   git add .
   git commit -m "Rollback: Re-enable App Service staging workflow"
   git push origin staging
   ```

2. **Old App Service should still be running** (if not deleted in Step 5)

3. **Investigate SWA issues** while old App Service handles traffic

---

## Post-Migration Checklist

After successful migration:

- [ ] Update team on new Staging URL
- [ ] Update any bookmarks or documentation
- [ ] Monitor Staging for 1 week
- [ ] Validate cost savings (~R350/month)
- [ ] Update architecture diagrams
- [ ] Schedule quarterly environment review

---

## Success Criteria

✅ Migration is successful when:
- [ ] Staging SWA deploys automatically on `staging` branch push
- [ ] App loads and functions identically to before
- [ ] All validation tests pass
- [ ] No CORS or routing issues
- [ ] Service worker and PWA features work
- [ ] Old App Service is decommissioned
- [ ] Old GitHub secret removed
- [ ] Team aware of new Staging URL

---

## Estimated Timeline

| Phase | Time | Cumulative |
|-------|------|------------|
| Step 1: Create SWA | 15 min | 15 min |
| Step 2: Configure Secret | 5 min | 20 min |
| Step 3: Test Deployment | 30 min | 50 min |
| Step 4: Validation | 30 min | 1h 20m |
| Step 5: Decommission | 15 min | 1h 35m |
| Step 6: Clean Up | 5 min | 1h 40m |
| Step 7: Final Verification | 10 min | 1h 50m |
| **Total** | **1h 50m** | |

---

## Cost Impact

**Before Migration**:
- Staging App Service B1: ~R350/month

**After Migration**:
- Staging SWA (Free tier): R0/month

**Savings**: ~R350/month (~R4,200/year)

---

## Support & Resources

**Documentation**:
- [Full Migration Guide](STAGING_MIGRATION_GUIDE.md)
- [Finalized Decision](FINALIZED_ENVIRONMENT_DECISION.md)
- [Executive Summary](EXECUTIVE_SUMMARY.md)

**Azure Support**:
- Portal: https://portal.azure.com
- SWA Docs: https://learn.microsoft.com/en-us/azure/static-web-apps/

**GitHub**:
- Actions: https://github.com/phoenixvc/Mystira.App/actions
- Secrets: https://github.com/phoenixvc/Mystira.App/settings/secrets/actions

---

**Execution Status**: ⏳ Ready - Execute Steps 1-7 in order  
**Automated Prep**: ✅ Complete (old workflow disabled)  
**Next Action**: Create Azure SWA resource (Step 1)  
**Estimated Total Time**: ~2 hours  
**Expected Outcome**: Production parity + R350/mo savings
