# GitHub Secrets Configuration Guide

This guide provides step-by-step instructions for configuring GitHub secrets required for the Mystira.App infrastructure and API deployments.

## Quick Reference

Navigate to: `https://github.com/phoenixvc/Mystira.App/settings/secrets/actions`

Or: Repository → Settings → Secrets and variables → Actions

## Prerequisites

- Admin access to the GitHub repository
- Azure CLI installed and configured
- Access to Azure subscription: `22f9eb18-6553-4b7d-9451-47d0195085fe`

## Setup Checklist

### ☑️ Step 1: Create Azure Service Principal

This Service Principal allows GitHub Actions to deploy infrastructure to Azure.

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription 22f9eb18-6553-4b7d-9451-47d0195085fe

# Create Service Principal with Contributor role
az ad sp create-for-rbac \
  --name "github-actions-mystira-app" \
  --role Contributor \
  --scopes /subscriptions/22f9eb18-6553-4b7d-9451-47d0195085fe/resourceGroups/dev-euw-rg-mystira \
  --sdk-auth
```

**Save the entire JSON output** - you'll need values from it for the next steps.

### ☑️ Step 2: Add Azure Authentication Secrets

#### `AZURE_CLIENT_ID`

1. Go to: https://github.com/phoenixvc/Mystira.App/settings/secrets/actions
2. Click: **New repository secret**
3. Name: `AZURE_CLIENT_ID`
4. Value: Copy `clientId` from Service Principal JSON output
5. Click: **Add secret**

#### `AZURE_TENANT_ID`

1. Click: **New repository secret**
2. Name: `AZURE_TENANT_ID`
3. Value: Copy `tenantId` from Service Principal JSON output
4. Click: **Add secret**

### ☑️ Step 3: Add Application Secrets

#### `JWT_SECRET_KEY`

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

⚠️ **Important**: Use different keys for dev and prod environments in production.

#### `ACS_CONNECTION_STRING` (Optional)

Only needed if you want to send emails via Azure Communication Services.

Get the connection string:

```bash
az communication list-key \
  --name dev-euw-acs-mystira \
  --resource-group dev-euw-rg-mystira \
  --query primaryConnectionString \
  --output tsv
```

Add to GitHub:
1. Click: **New repository secret**
2. Name: `ACS_CONNECTION_STRING`
3. Value: Paste the connection string
4. Click: **Add secret**

If you skip this, emails will be logged to console instead of sent.

### ☑️ Step 4: Add Azure Web App Publish Profiles

These are needed for the API deployment workflows to work.

#### `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`

**Option A: Via Azure CLI**
```bash
az webapp deployment list-publishing-profiles \
  --name mystira-app-dev-api \
  --resource-group dev-euw-rg-mystira \
  --xml
```

**Option B: Via Azure Portal**
1. Go to: https://portal.azure.com
2. Navigate to: App Services → `mystira-app-dev-api`
3. Click: **Get publish profile** (top toolbar)
4. Save the downloaded file and copy its content

**Add to GitHub:**
1. Click: **New repository secret**
2. Name: `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`
3. Value: Paste the entire XML content
4. Click: **Add secret**

#### `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`

Repeat the above steps for the Admin API:
- App Service name: `dev-euw-app-mystora-admin-api`
- Secret name: `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`

#### `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`

Repeat the above steps for the Production API:
- App Service name: `prod-wus-app-mystira-api`
- Secret name: `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`

#### `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`

Repeat the above steps for the Production Admin API:
- App Service name: `prod-wus-app-mystira-api-admin`
- Secret name: `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`

## Verification

After adding all secrets, verify your setup:

1. Go to: https://github.com/phoenixvc/Mystira.App/settings/secrets/actions
2. You should see **8 secrets** listed:
   - ✅ `AZURE_CLIENT_ID`
   - ✅ `AZURE_TENANT_ID`
   - ✅ `JWT_SECRET_KEY`
   - ✅ `ACS_CONNECTION_STRING` (optional)
   - ✅ `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`
   - ✅ `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`
   - ✅ `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`
   - ✅ `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`

3. Test by triggering a workflow:
   - Go to: Actions tab
   - Select: "Infrastructure Deployment - Dev Environment"
   - Click: "Run workflow"
   - Select: `dev` branch
   - Click: "Run workflow"

## Secrets Summary

| Secret | Required? | Purpose | How to Get |
|--------|-----------|---------|------------|
| `AZURE_CLIENT_ID` | ✅ Required | Azure authentication for infrastructure deployment | Service Principal JSON → `clientId` |
| `AZURE_TENANT_ID` | ✅ Required | Azure authentication for infrastructure deployment | Service Principal JSON → `tenantId` |
| `JWT_SECRET_KEY` | ✅ Required | API authentication token signing | Generate: `openssl rand -base64 32` |
| `ACS_CONNECTION_STRING` | ⚠️ Optional | Email sending capability | Azure Portal → Communication Services → Keys |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` | ✅ Required | Deploy main API to dev | Azure Portal → App Service → Get publish profile |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` | ✅ Required | Deploy admin API to dev | Azure Portal → App Service → Get publish profile |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` | ✅ Required | Deploy main API to prod | Azure Portal → App Service → Get publish profile |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN` | ✅ Required | Deploy admin API to prod | Azure Portal → App Service → Get publish profile |

## Troubleshooting

### "Secret not found" error in workflow

**Solution**: Check that the secret name in the workflow YAML exactly matches the secret name in GitHub (case-sensitive).

### Authentication failed during deployment

**Solution**: 
1. Verify Service Principal has Contributor role:
   ```bash
   az role assignment list --assignee <AZURE_CLIENT_ID> --all
   ```
2. If missing, add the role:
   ```bash
   az role assignment create \
     --assignee <AZURE_CLIENT_ID> \
     --role Contributor \
     --scope /subscriptions/22f9eb18-6553-4b7d-9451-47d0195085fe/resourceGroups/dev-euw-rg-mystira
   ```

### Publish profile expired or invalid

**Solution**: Download a new publish profile from Azure Portal and update the GitHub secret.

### How to update a secret

1. Go to: https://github.com/phoenixvc/Mystira.App/settings/secrets/actions
2. Click on the secret name
3. Click: **Update secret**
4. Enter the new value
5. Click: **Update secret**

### How to rotate secrets

For security best practices, rotate secrets periodically:

1. **JWT_SECRET_KEY**: Generate new key, update GitHub secret, redeploy APIs
2. **Service Principal**: Create new SP, update `AZURE_CLIENT_ID` and `AZURE_TENANT_ID`, delete old SP
3. **Publish Profiles**: Download new profiles from Azure Portal, update GitHub secrets

## Security Best Practices

1. ✅ Never commit secrets to the repository
2. ✅ Use different JWT keys for dev and prod
3. ✅ Rotate secrets every 90 days
4. ✅ Use Azure Key Vault for production secrets
5. ✅ Limit Service Principal permissions to minimum required
6. ✅ Monitor secret access in GitHub audit logs
7. ✅ Use separate Service Principals for dev and prod

## Need Help?

- Check workflow run logs: https://github.com/phoenixvc/Mystira.App/actions
- Review infrastructure documentation: `infrastructure/README.md`
- Azure CLI documentation: https://docs.microsoft.com/cli/azure/
- GitHub Actions secrets: https://docs.github.com/en/actions/security-guides/encrypted-secrets

## Quick Commands Reference

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription 22f9eb18-6553-4b7d-9451-47d0195085fe

# Create Service Principal
az ad sp create-for-rbac --name "github-actions-mystira-app" --role Contributor --scopes /subscriptions/22f9eb18-6553-4b7d-9451-47d0195085fe/resourceGroups/dev-euw-rg-mystira --sdk-auth

# Generate JWT secret
openssl rand -base64 32

# Get ACS connection string
az communication list-key --name dev-euw-acs-mystira --resource-group dev-euw-rg-mystira --query primaryConnectionString --output tsv

# Get App Service publish profile
az webapp deployment list-publishing-profiles --name mystira-app-dev-api --resource-group dev-euw-rg-mystira --xml
```
