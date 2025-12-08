# GitHub Secrets and Variables Configuration

## Overview

This document provides a comprehensive guide to configuring GitHub Secrets and Variables for the Mystira Application Suite across all three environments: **Development**, **Staging**, and **Production**.

> ⚠️ **IMPORTANT - Staging Configuration Issue Detected**
> 
> The staging workflows currently have a configuration error where all three services (API, Admin API, and PWA) attempt to use the same publish profile secret (`AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`). However, each Azure App Service requires its own unique publish profile.
> 
> **Impact**: Staging deployments will fail or deploy to the wrong service.
> 
> **Fix Required**: Update staging workflows to use separate secrets:
> - `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_API`
> - `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN`  
> - `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_PWA`
>
> See the [Staging Environment](#staging-environment) section for details.

## Environments

The Mystira application uses three distinct environments, each with its own set of Azure resources and configuration:

| Environment | Branch | Purpose | Azure Resource Prefix |
|------------|--------|---------|---------------------|
| **Development** | `dev` | Active development and testing | `dev-san-*` (South Africa North) / `dev-euw-*` (West Europe) |
| **Staging** | `staging` | Pre-production validation | `staging-*` |
| **Production** | `main` | Live production environment | `prod-wus-*` (West US) |

## Required GitHub Secrets

GitHub Secrets are encrypted environment variables that store sensitive information. Configure these in your repository under **Settings > Secrets and variables > Actions**.

### 1. Azure Credentials & Subscription

#### `AZURE_CREDENTIALS` (All Environments)
- **Type**: JSON object
- **Purpose**: Service principal credentials for Azure CLI authentication
- **Used by**: Infrastructure deployment workflows, app settings configuration
- **Required for**: All environments

**How to generate:**
```bash
az ad sp create-for-rbac \
  --name "mystira-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth
```

**Expected format:**
```json
{
  "clientId": "00000000-0000-0000-0000-000000000000",
  "clientSecret": "your-client-secret",
  "subscriptionId": "00000000-0000-0000-0000-000000000000",
  "tenantId": "00000000-0000-0000-0000-000000000000"
}
```

#### `AZURE_SUBSCRIPTION_ID` (All Environments)
- **Type**: String (GUID)
- **Purpose**: Azure subscription identifier
- **Used by**: Infrastructure deployment workflows
- **Example**: `22f9eb18-6553-4b7d-9451-47d0195085fe`

### 2. Azure Web App Publish Profiles

These secrets contain the publish profile XML for deploying to Azure App Services.

#### Development Environment
- **`AZURE_WEBAPP_PUBLISH_PROFILE_DEV`** - For main API (`dev-san-app-mystira-api`)
- **`AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`** - For admin API (`dev-san-app-mystira-admin-api`)

#### Staging Environment
> ⚠️ **Configuration Issue Detected**: The current staging workflows all reference the same secret (`AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`) but deploy to three different App Services. Each App Service requires its own unique publish profile. The workflows should be updated to use separate secrets.

**Current (Incorrect) Configuration:**
- **`AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`** - Referenced by API, Admin API, and PWA workflows (shared - **this doesn't work**)

**Required (Correct) Configuration:**
- **`AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_API`** - For main API (`mystira-app-staging-api`)
- **`AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN`** - For admin API (`mystira-app-staging-admin-api`)
- **`AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_PWA`** - For PWA (`mystira-app-staging-pwa`)

**To Fix:** Update the staging workflow files to use the separate secrets above instead of the shared `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`.

#### Production Environment
- **`AZURE_WEBAPP_PUBLISH_PROFILE_PROD`** - For main API (`prod-wus-app-mystira-api`)
- **`AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`** - For admin API (`prod-wus-app-mystira-api-admin`)

**How to obtain:**
```bash
# Download publish profile from Azure portal or use CLI
az webapp deployment list-publishing-profiles \
  --name {app-name} \
  --resource-group {resource-group} \
  --xml
```

### 3. JWT Authentication Keys

#### `JWT_SECRET_KEY` (Infrastructure Deployment)
- **Type**: String (base64-encoded)
- **Purpose**: Used during infrastructure deployment for initial setup
- **Used by**: Infrastructure deployment workflows (all environments)

**Generate:**
```bash
openssl rand -base64 64
```

#### JWT RSA Key Pairs (Per Environment)

**Development:**
- **`JWT_RSA_PRIVATE_KEY`** - RSA private key for JWT signing (dev)
- **`JWT_RSA_PUBLIC_KEY`** - RSA public key for JWT verification (dev)

**Staging:**
- **`JWT_RSA_PRIVATE_KEY_STAGING`** - RSA private key for JWT signing (staging)
- **`JWT_RSA_PUBLIC_KEY_STAGING`** - RSA public key for JWT verification (staging)

**Production:**
- **`JWT_RSA_PRIVATE_KEY_PROD`** - RSA private key for JWT signing (production)
- **`JWT_RSA_PUBLIC_KEY_PROD`** - RSA public key for JWT verification (production)

**Generate RSA key pair:**
```bash
# Generate private key
openssl genrsa -out private_key.pem 2048

# Extract public key
openssl rsa -in private_key.pem -pubout -out public_key.pem

# Format for GitHub secret (single line, escaped newlines)
cat private_key.pem | sed 's/$/\\n/' | tr -d '\n'
cat public_key.pem | sed 's/$/\\n/' | tr -d '\n'
```

### 4. Azure Communication Services (Email)

#### `AZURE_ACS_CONNECTION_STRING` (Development Only)
- **Type**: Connection string
- **Purpose**: Azure Communication Services connection for email sending
- **Used by**: Dev API workflow only
- **Format**: `endpoint=https://...;accesskey=...`

**How to obtain:**
```bash
az communication list-key \
  --name {acs-name} \
  --resource-group {resource-group} \
  --query primaryConnectionString -o tsv
```

#### `AZURE_ACS_SENDER_EMAIL_DEV` (Development Only)
- **Type**: Email address
- **Purpose**: Sender email for Azure Communication Services
- **Example**: `DoNotReply@yourdomain.azurecomm.net`

**Note:** Staging and Production use different email configuration methods (typically configured directly in Azure App Service settings).

### 5. Azure Static Web Apps Deployment Tokens

#### `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E` (Production PWA)
- **Type**: Deployment token
- **Purpose**: Deploys Blazor PWA to production Static Web App
- **Static Web App**: `blue-water-0eab7991e` (mystira.app)

#### `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_SAN_MYSTIRA_APP` (Development PWA)
- **Type**: Deployment token
- **Purpose**: Deploys Blazor PWA to development Static Web App
- **Static Web App**: `mango-water-04fdb1c03`

**How to obtain:**
```bash
az staticwebapp secrets list \
  --name {static-web-app-name} \
  --query "properties.apiKey" -o tsv
```

### 6. Code Coverage & Testing

#### `CODECOV_SECRET`
- **Type**: API token
- **Purpose**: Uploads test coverage reports to Codecov
- **Used by**: `ci-tests-codecov.yml` workflow
- **Obtain from**: https://codecov.io (Codecov dashboard)

### 7. Automatic Secrets (No Configuration Required)

#### `GITHUB_TOKEN`
- **Type**: Automatic token
- **Purpose**: GitHub API authentication for workflow actions (PR comments, etc.)
- **Scope**: Automatically provided by GitHub Actions
- **No manual configuration needed**

## GitHub Variables

GitHub Variables are non-sensitive configuration values. Currently, **no custom variables** are used in the workflows. All configuration uses:
- Environment-specific hardcoded values in workflow files
- Secrets for sensitive data
- Environment variables derived from branch names

## Secrets Configuration Matrix

### Complete Checklist by Environment

#### Development Environment Secrets
- [x] `AZURE_CREDENTIALS`
- [x] `AZURE_SUBSCRIPTION_ID`
- [x] `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`
- [x] `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`
- [x] `JWT_SECRET_KEY`
- [x] `JWT_RSA_PRIVATE_KEY`
- [x] `JWT_RSA_PUBLIC_KEY`
- [x] `AZURE_ACS_CONNECTION_STRING`
- [x] `AZURE_ACS_SENDER_EMAIL_DEV`
- [x] `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_SAN_MYSTIRA_APP`

#### Staging Environment Secrets
> ⚠️ **Note**: Current workflows use a shared secret which is incorrect. The checklist below shows the **correct** configuration needed.

- [x] `AZURE_CREDENTIALS`
- [x] `AZURE_SUBSCRIPTION_ID`
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_API` (API deployment - **workflow needs update**)
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN` (Admin API deployment - **workflow needs update**)
- [ ] `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_PWA` (PWA deployment - **workflow needs update**)
- [x] `JWT_SECRET_KEY`
- [x] `JWT_RSA_PRIVATE_KEY_STAGING`
- [x] `JWT_RSA_PUBLIC_KEY_STAGING`

#### Production Environment Secrets
- [x] `AZURE_CREDENTIALS`
- [x] `AZURE_SUBSCRIPTION_ID`
- [x] `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`
- [x] `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`
- [x] `JWT_SECRET_KEY`
- [x] `JWT_RSA_PRIVATE_KEY_PROD`
- [x] `JWT_RSA_PUBLIC_KEY_PROD`
- [x] `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E`

#### Optional/Shared Secrets
- [x] `CODECOV_SECRET` (for test coverage reporting)

## Workflow-to-Secret Mapping

### Infrastructure Deployment Workflows

| Workflow | Environment | Required Secrets |
|----------|------------|-----------------|
| `infrastructure-deploy-dev.yml` | Development | `AZURE_CREDENTIALS`, `AZURE_SUBSCRIPTION_ID`, `JWT_SECRET_KEY` |
| `infrastructure-deploy-staging.yml` | Staging | `AZURE_CREDENTIALS`, `AZURE_SUBSCRIPTION_ID`, `JWT_SECRET_KEY` |
| `infrastructure-deploy-prod.yml` | Production | `AZURE_CREDENTIALS`, `AZURE_SUBSCRIPTION_ID`, `JWT_SECRET_KEY` |

### API Deployment Workflows

| Workflow | Environment | Required Secrets |
|----------|------------|-----------------|
| `mystira-app-api-cicd-dev.yml` | Development | `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`, `AZURE_CREDENTIALS`, `JWT_RSA_PRIVATE_KEY`, `JWT_RSA_PUBLIC_KEY`, `AZURE_ACS_CONNECTION_STRING`, `AZURE_ACS_SENDER_EMAIL_DEV` |
| `mystira-app-api-cicd-staging.yml` | Staging | ⚠️ **Currently**: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` (incorrect)<br>**Should be**: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_API`, `AZURE_CREDENTIALS`, `JWT_RSA_PRIVATE_KEY_STAGING`, `JWT_RSA_PUBLIC_KEY_STAGING` |
| `mystira-app-api-cicd-prod.yml` | Production | `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`, `AZURE_CREDENTIALS`, `JWT_RSA_PRIVATE_KEY_PROD`, `JWT_RSA_PUBLIC_KEY_PROD` |

### Admin API Deployment Workflows

| Workflow | Environment | Required Secrets |
|----------|------------|-----------------|
| `mystira-app-admin-api-cicd-dev.yml` | Development | `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` |
| `mystira-app-admin-api-cicd-staging.yml` | Staging | ⚠️ **Currently**: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` (incorrect)<br>**Should be**: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN` |
| `mystira-app-admin-api-cicd-prod.yml` | Production | `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN` |

### PWA Deployment Workflows

| Workflow | Environment | Required Secrets |
|----------|------------|-----------------|
| `azure-static-web-apps-dev-san-swa-mystira-app.yml` | Development | `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_SAN_MYSTIRA_APP` |
| `azure-static-web-apps-blue-water-0eab7991e.yml` | Production | `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E` |
| `mystira-app-pwa-cicd-staging.yml` | Staging | ⚠️ **Currently**: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` (incorrect)<br>**Should be**: `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_PWA` |

### Testing Workflows

| Workflow | Purpose | Required Secrets |
|----------|---------|-----------------|
| `ci-tests-codecov.yml` | Test coverage reporting | `CODECOV_SECRET` |

## Setup Instructions

### Step 1: Configure Azure Service Principal

```bash
# Create service principal with contributor role
SUBSCRIPTION_ID="your-subscription-id"
RESOURCE_GROUP="your-resource-group"

az ad sp create-for-rbac \
  --name "mystira-github-actions" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth > azure-credentials.json

# Copy the output to AZURE_CREDENTIALS secret
cat azure-credentials.json
```

### Step 2: Generate JWT Keys

```bash
# Infrastructure deployment key (shared across environments)
openssl rand -base64 64

# Environment-specific RSA key pairs
for env in dev staging prod; do
  echo "Generating keys for $env..."
  openssl genrsa -out jwt_private_${env}.pem 2048
  openssl rsa -in jwt_private_${env}.pem -pubout -out jwt_public_${env}.pem
  
  echo "Private key for JWT_RSA_PRIVATE_KEY_${env^^}:"
  cat jwt_private_${env}.pem | sed 's/$/\\n/' | tr -d '\n'
  echo ""
  
  echo "Public key for JWT_RSA_PUBLIC_KEY_${env^^}:"
  cat jwt_public_${env}.pem | sed 's/$/\\n/' | tr -d '\n'
  echo ""
done
```

### Step 3: Download Publish Profiles

```bash
# Development environment
az webapp deployment list-publishing-profiles \
  --name dev-san-app-mystira-api \
  --resource-group dev-euw-rg-mystira-app \
  --xml > dev-api-publish-profile.xml

az webapp deployment list-publishing-profiles \
  --name dev-san-app-mystira-admin-api \
  --resource-group dev-euw-rg-mystira-app \
  --xml > dev-admin-api-publish-profile.xml

# Staging environment
az webapp deployment list-publishing-profiles \
  --name mystira-app-staging-api \
  --resource-group staging-rg-mystira-app \
  --xml > staging-publish-profile.xml

# Production environment
az webapp deployment list-publishing-profiles \
  --name prod-wus-app-mystira-api \
  --resource-group prod-rg-mystira-app \
  --xml > prod-api-publish-profile.xml

az webapp deployment list-publishing-profiles \
  --name prod-wus-app-mystira-api-admin \
  --resource-group prod-rg-mystira-app \
  --xml > prod-admin-api-publish-profile.xml
```

### Step 4: Get Static Web App Tokens

```bash
# Development PWA
az staticwebapp secrets list \
  --name mango-water-04fdb1c03 \
  --query "properties.apiKey" -o tsv

# Production PWA
az staticwebapp secrets list \
  --name blue-water-0eab7991e \
  --query "properties.apiKey" -o tsv
```

### Step 5: Configure Azure Communication Services (Dev Only)

```bash
# Get ACS connection string
az communication list-key \
  --name {your-acs-resource-name} \
  --resource-group dev-euw-rg-mystira-app \
  --query primaryConnectionString -o tsv

# Get verified sender email from ACS Email Communication Service
```

### Step 6: Add Secrets to GitHub

1. Navigate to your repository on GitHub
2. Go to **Settings > Secrets and variables > Actions**
3. Click **New repository secret**
4. Add each secret with the exact name from the tables above
5. Paste the corresponding value
6. Click **Add secret**

## Security Best Practices

1. **Rotate secrets regularly** (every 90 days recommended)
2. **Use separate key pairs per environment** (already configured)
3. **Never commit secrets to version control**
4. **Limit service principal permissions** to specific resource groups
5. **Enable secret scanning** in GitHub repository settings
6. **Audit secret access** via Azure AD logs
7. **Use Azure Key Vault** for production secrets (runtime configuration)
8. **Document secret rotation procedures**

## Troubleshooting

### Missing Secret Validation

Workflows include secret validation steps that will fail with clear error messages if required secrets are missing:

```
ERROR: Secret AZURE_WEBAPP_PUBLISH_PROFILE_DEV is not set.
```

### Secret Format Issues

**JWT Keys:**
- Must be properly escaped with `\n` for newlines in RSA keys
- Use the provided generation scripts to ensure correct format

**Publish Profiles:**
- Must be valid XML
- Include the entire output from `az webapp deployment list-publishing-profiles`

**Azure Credentials:**
- Must be valid JSON in the exact format from `az ad sp create-for-rbac --sdk-auth`

### Workflow Failures

If workflows fail due to secrets:
1. Check the workflow run logs for specific secret validation errors
2. Verify the secret name matches exactly (case-sensitive)
3. Ensure the secret value is not empty
4. Re-generate and update the secret if corrupted

## Migration Notes

If migrating from an older configuration:

1. **JWT Migration**: Older deployments may use symmetric keys (`JWT_SECRET_KEY`). New deployments use asymmetric RSA keys for better security.
2. **Environment-specific keys**: Ensure each environment has its own RSA key pair.
3. **ACS Configuration**: Only Development environment requires ACS secrets in GitHub. Staging/Production configure ACS directly in Azure App Service settings.

## Related Documentation

- [Secrets Management Guide](SECRETS_MANAGEMENT.md) - General secrets management practices
- [Email Setup Guide](EMAIL_SETUP.md) - Azure Communication Services configuration
- [Deploy Now Script Documentation](../../DEPLOY-NOW.md) - Infrastructure deployment
- [Repository README](../../README.md) - Main project documentation

## Support

For issues with GitHub secrets configuration:
1. Verify secret names match exactly (case-sensitive)
2. Check workflow logs for specific validation errors
3. Consult the troubleshooting section above
4. Contact DevOps team for Azure-related issues

---

**Last Updated**: 2025-12-08  
**Version**: 1.0  
**Maintainer**: DevOps Team
