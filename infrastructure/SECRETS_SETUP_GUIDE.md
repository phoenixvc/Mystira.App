# GitHub Secrets Configuration Guide

This guide provides step-by-step instructions for configuring GitHub secrets required for the Mystira.App infrastructure and API deployments.

## Quick Reference

Navigate to: `https://github.com/phoenixvc/Mystira.App/settings/secrets/actions`

Or: Repository → Settings → Secrets and variables → Actions

## Complete Secrets Overview

### Required Secrets by Workflow

| Secret | Used By | Required? |
|--------|---------|-----------|
| `AZURE_CREDENTIALS` | Infrastructure Deploy (all envs) | ✅ Required |
| `AZURE_SUBSCRIPTION_ID` | Infrastructure Deploy (all envs) | ✅ Required |
| `JWT_SECRET_KEY` | Infrastructure Deploy (all envs) | ✅ Required |
| `ACS_CONNECTION_STRING` | Infrastructure Deploy | ⚠️ Optional |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` | API CI/CD - Dev | ✅ Required |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` | Admin API CI/CD - Dev | ✅ Required |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` | API CI/CD - Prod | ✅ Required |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN` | Admin API CI/CD - Prod | ✅ Required |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` | API CI/CD - Staging | ✅ Required |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN` | Admin API CI/CD - Staging | ✅ Required |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_MANGO_WATER_04FDB1C03` | PWA CI/CD - Dev | ✅ Required |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E` | PWA CI/CD - Prod | ✅ Required |

### Workflow Permissions

The infrastructure deploy workflows also require these **repository settings** for PR commenting:

1. Go to: Repository → Settings → Actions → General
2. Under "Workflow permissions", select: **Read and write permissions**
3. Check: **Allow GitHub Actions to create and approve pull requests**

Without this, the "Resource not accessible by integration" error occurs when the workflow tries to comment on PRs.

## Prerequisites

- Admin access to the GitHub repository
- Azure CLI installed and configured
- Access to Azure subscription: `22f9eb18-6553-4b7d-9451-47d0195085fe`

---

## Section 1: Azure Infrastructure Secrets

### `AZURE_CREDENTIALS` (Required)

This JSON object allows GitHub Actions to authenticate with Azure.

**Step 1: Create Service Principal**

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription 22f9eb18-6553-4b7d-9451-47d0195085fe

# Create Service Principal with Contributor role (scoped to all resource groups)
az ad sp create-for-rbac \
  --name "github-actions-mystira-app" \
  --role Contributor \
  --scopes /subscriptions/22f9eb18-6553-4b7d-9451-47d0195085fe \
  --sdk-auth
```

**Step 2: Copy the ENTIRE JSON output** (looks like this):
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "22f9eb18-6553-4b7d-9451-47d0195085fe",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  ...
}
```

**Step 3: Add to GitHub**
1. Go to: https://github.com/phoenixvc/Mystira.App/settings/secrets/actions
2. Click: **New repository secret**
3. Name: `AZURE_CREDENTIALS`
4. Value: Paste the **entire JSON output**
5. Click: **Add secret**

### `AZURE_SUBSCRIPTION_ID` (Required)

1. Click: **New repository secret**
2. Name: `AZURE_SUBSCRIPTION_ID`
3. Value: `22f9eb18-6553-4b7d-9451-47d0195085fe`
4. Click: **Add secret**

### `JWT_SECRET_KEY` (Required)

Generate a secure key:

```bash
# Using OpenSSL (Linux/Mac)
openssl rand -base64 32

# Using PowerShell (Windows)
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

Add to GitHub:
1. Click: **New repository secret**
2. Name: `JWT_SECRET_KEY`
3. Value: Paste the generated key
4. Click: **Add secret**

⚠️ **Important**: Use different keys for dev, staging, and prod environments in production.

### `ACS_CONNECTION_STRING` (Optional)

Only needed if you want to send emails via Azure Communication Services. If not configured, emails will be logged to console instead.

```bash
az communication list-key \
  --name dev-euw-acs-mystira \
  --resource-group dev-euw-rg-mystira \
  --query primaryConnectionString \
  --output tsv
```

---

## Section 2: Static Web Apps (PWA) Secrets

These are required for the Blazor PWA deployment.

### `AZURE_STATIC_WEB_APPS_API_TOKEN_MANGO_WATER_04FDB1C03` (Dev)

**Get from Azure Portal:**
1. Go to: https://portal.azure.com
2. Navigate to: Static Web Apps → `mango-water-04fdb1c03` (or your dev SWA name)
3. Click: **Manage deployment token** (in the toolbar or Overview)
4. Copy the deployment token

**Add to GitHub:**
1. Name: `AZURE_STATIC_WEB_APPS_API_TOKEN_MANGO_WATER_04FDB1C03`
2. Value: Paste the deployment token

### `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E` (Prod)

Repeat the above steps for the production SWA:
- SWA name: `blue-water-0eab7991e` (or your prod SWA name)
- Secret name: `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E`

**Note:** If your Static Web Apps have different names, update the workflow files to match:
- `.github/workflows/azure-static-web-apps-mango-water-04fdb1c03.yml`
- `.github/workflows/azure-static-web-apps-blue-water-0eab7991e.yml`

---

## Section 3: App Service Publish Profiles

These are needed for the API deployment workflows.

### How to Get a Publish Profile

**Option A: Via Azure CLI**
```bash
az webapp deployment list-publishing-profiles \
  --name <app-service-name> \
  --resource-group <resource-group> \
  --xml
```

**Option B: Via Azure Portal**
1. Go to: https://portal.azure.com
2. Navigate to: App Services → `<app-service-name>`
3. Click: **Get publish profile** (top toolbar)
4. Save and copy the downloaded XML content

### Required Publish Profile Secrets

| Secret Name | App Service Name | Resource Group |
|-------------|------------------|----------------|
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` | `mystira-app-dev-api` | `dev-euw-rg-mystira` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` | `dev-euw-app-mystora-admin-api` | `dev-euw-rg-mystira` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` | `mystira-app-staging-api` | `staging-euw-rg-mystira` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN` | `staging-euw-app-mystira-admin-api` | `staging-euw-rg-mystira` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` | `prod-wus-app-mystira-api` | `prod-wus-rg-mystira` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN` | `prod-wus-app-mystira-api-admin` | `prod-wus-rg-mystira` |

**Quick Commands:**
```bash
# Dev API
az webapp deployment list-publishing-profiles --name mystira-app-dev-api --resource-group dev-euw-rg-mystira --xml

# Dev Admin API
az webapp deployment list-publishing-profiles --name dev-euw-app-mystora-admin-api --resource-group dev-euw-rg-mystira --xml

# Prod API
az webapp deployment list-publishing-profiles --name prod-wus-app-mystira-api --resource-group prod-wus-rg-mystira --xml

# Prod Admin API
az webapp deployment list-publishing-profiles --name prod-wus-app-mystira-api-admin --resource-group prod-wus-rg-mystira --xml
```

---

## Verification Checklist

After adding all secrets, you should have these **14 secrets** configured:

### Azure Authentication (3)
- [ ] `AZURE_CREDENTIALS`
- [ ] `AZURE_SUBSCRIPTION_ID`
- [ ] `JWT_SECRET_KEY`

### Static Web Apps (2)
- [ ] `AZURE_STATIC_WEB_APPS_API_TOKEN_MANGO_WATER_04FDB1C03`
- [ ] `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E`

### Publish Profiles - Dev (2)
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`

### Publish Profiles - Staging (2)
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN`

### Publish Profiles - Prod (2)
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`

### Optional (1)
- [ ] `ACS_CONNECTION_STRING`

### Workflow Permissions
- [ ] Repository Settings → Actions → General → "Read and write permissions" enabled
- [ ] "Allow GitHub Actions to create and approve pull requests" checked

---

## Common Errors and Fixes

### "Resource not accessible by integration" (403)

**Cause:** GitHub Actions doesn't have permission to comment on PRs.

**Fix:**
1. Go to: Repository → Settings → Actions → General
2. Under "Workflow permissions", select: **Read and write permissions**
3. Check: **Allow GitHub Actions to create and approve pull requests**
4. Save

### "No matching Static Web App was found or the api key was invalid"

**Cause:** Static Web Apps API token is missing, expired, or doesn't match the SWA resource.

**Fix:**
1. Verify the SWA resource exists in Azure Portal
2. Get a fresh deployment token from the SWA resource
3. Update the GitHub secret with the new token
4. Ensure the secret name in the workflow matches exactly

### "Secret not found" error in workflow

**Cause:** Secret name mismatch (case-sensitive).

**Fix:** Verify the secret name in the workflow YAML exactly matches the secret name in GitHub.

### Authentication failed during deployment

**Fix:**
1. Verify Service Principal has Contributor role:
   ```bash
   az role assignment list --assignee <CLIENT_ID_FROM_CREDENTIALS> --all
   ```
2. If missing, add the role:
   ```bash
   az role assignment create \
     --assignee <CLIENT_ID> \
     --role Contributor \
     --scope /subscriptions/22f9eb18-6553-4b7d-9451-47d0195085fe
   ```

### Publish profile expired or invalid

**Fix:** Download a new publish profile from Azure Portal and update the GitHub secret.

---

## Security Best Practices

1. ✅ Never commit secrets to the repository
2. ✅ Use different JWT keys for dev, staging, and prod
3. ✅ Rotate secrets every 90 days
4. ✅ Use Azure Key Vault for production secrets (app-level)
5. ✅ Limit Service Principal permissions to minimum required
6. ✅ Monitor secret access in GitHub audit logs
7. ✅ Use separate Service Principals for dev and prod (recommended)

---

## Quick Commands Reference

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription 22f9eb18-6553-4b7d-9451-47d0195085fe

# Create Service Principal (AZURE_CREDENTIALS)
az ad sp create-for-rbac \
  --name "github-actions-mystira-app" \
  --role Contributor \
  --scopes /subscriptions/22f9eb18-6553-4b7d-9451-47d0195085fe \
  --sdk-auth

# Generate JWT secret
openssl rand -base64 32

# Get ACS connection string
az communication list-key \
  --name dev-euw-acs-mystira \
  --resource-group dev-euw-rg-mystira \
  --query primaryConnectionString \
  --output tsv

# Get App Service publish profile
az webapp deployment list-publishing-profiles \
  --name mystira-app-dev-api \
  --resource-group dev-euw-rg-mystira \
  --xml
```

---

## Need Help?

- Check workflow run logs: https://github.com/phoenixvc/Mystira.App/actions
- Review infrastructure documentation: `infrastructure/README.md`
- Azure CLI documentation: https://docs.microsoft.com/cli/azure/
- GitHub Actions secrets: https://docs.github.com/en/actions/security-guides/encrypted-secrets
- Static Web Apps deployment tokens: https://docs.microsoft.com/en-us/azure/static-web-apps/deployment-token-management
