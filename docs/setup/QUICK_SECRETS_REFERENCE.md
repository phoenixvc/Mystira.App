# Quick Secrets Reference

> **📘 Full Documentation**: For detailed setup instructions and security best practices, see [GitHub Secrets and Variables Guide](GITHUB_SECRETS_VARIABLES.md)

> ⚠️ **Staging Configuration Issue**: The staging workflows currently use a shared secret for all services, which is incorrect. See corrected requirements below.

## Secrets Required by Environment

### Development (dev branch)
| Secret Name | Purpose | Generate With |
|------------|---------|---------------|
| `AZURE_CREDENTIALS` | Azure CLI auth | `az ad sp create-for-rbac --sdk-auth` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription | Azure Portal |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` | API deployment | `az webapp deployment list-publishing-profiles --xml` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` | Admin API deployment | `az webapp deployment list-publishing-profiles --xml` |
| `JWT_SECRET_KEY` | Infrastructure setup | `openssl rand -base64 64` |
| `JWT_RSA_PRIVATE_KEY` | JWT signing | `openssl genrsa 2048` |
| `JWT_RSA_PUBLIC_KEY` | JWT verification | `openssl rsa -pubout` |
| `AZURE_ACS_CONNECTION_STRING` | Email service | `az communication list-key` |
| `AZURE_ACS_SENDER_EMAIL_DEV` | Sender email | From ACS portal |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_SAN_MYSTIRA_APP` | PWA deployment | `az staticwebapp secrets list` |

### Staging (staging branch)
> ⚠️ **Workflows need update**: Currently reference `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` for all services (incorrect)

| Secret Name | Purpose | Generate With |
|------------|---------|---------------|
| `AZURE_CREDENTIALS` | Azure CLI auth | `az ad sp create-for-rbac --sdk-auth` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription | Azure Portal |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_API` | API deployment (**workflow update needed**) | `az webapp deployment list-publishing-profiles --xml` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_ADMIN` | Admin API deployment (**workflow update needed**) | `az webapp deployment list-publishing-profiles --xml` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING_PWA` | PWA deployment (**workflow update needed**) | `az webapp deployment list-publishing-profiles --xml` |
| `JWT_SECRET_KEY` | Infrastructure setup | `openssl rand -base64 64` |
| `JWT_RSA_PRIVATE_KEY_STAGING` | JWT signing | `openssl genrsa 2048` |
| `JWT_RSA_PUBLIC_KEY_STAGING` | JWT verification | `openssl rsa -pubout` |

### Production (main branch)
| Secret Name | Purpose | Generate With |
|------------|---------|---------------|
| `AZURE_CREDENTIALS` | Azure CLI auth | `az ad sp create-for-rbac --sdk-auth` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription | Azure Portal |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` | API deployment | `az webapp deployment list-publishing-profiles --xml` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN` | Admin API deployment | `az webapp deployment list-publishing-profiles --xml` |
| `JWT_SECRET_KEY` | Infrastructure setup | `openssl rand -base64 64` |
| `JWT_RSA_PRIVATE_KEY_PROD` | JWT signing | `openssl genrsa 2048` |
| `JWT_RSA_PUBLIC_KEY_PROD` | JWT verification | `openssl rsa -pubout` |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_WATER_0EAB7991E` | PWA deployment | `az staticwebapp secrets list` |

### Optional/Shared
| Secret Name | Purpose | Used By |
|------------|---------|---------|
| `CODECOV_SECRET` | Test coverage reporting | All environments |
| `GITHUB_TOKEN` | PR comments, GitHub API | Auto-provided (no config needed) |

## Quick Setup Commands

### 1. Azure Service Principal
```bash
az ad sp create-for-rbac --name "mystira-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

### 2. JWT Keys (per environment)
```bash
# Generate RSA key pair
openssl genrsa -out private.pem 2048
openssl rsa -in private.pem -pubout -out public.pem

# Format for GitHub (single line with \n)
cat private.pem | sed 's/$/\\n/' | tr -d '\n'
cat public.pem | sed 's/$/\\n/' | tr -d '\n'
```

### 3. Publish Profiles
```bash
az webapp deployment list-publishing-profiles \
  --name {app-name} \
  --resource-group {resource-group} \
  --xml
```

### 4. Static Web App Tokens
```bash
az staticwebapp secrets list \
  --name {static-web-app-name} \
  --query "properties.apiKey" -o tsv
```

## Configuration Location

**GitHub Repository Settings:**
1. Go to your repository on GitHub
2. Navigate to **Settings > Secrets and variables > Actions**
3. Click **New repository secret**
4. Enter exact secret name (case-sensitive)
5. Paste the value
6. Click **Add secret**

## Key Differences Between Environments

| Aspect | Development | Staging | Production |
|--------|------------|---------|-----------|
| **Branch** | `dev` | `staging` | `main` |
| **Azure Region** | South Africa North / West Europe | TBD | West US |
| **Email Service** | ACS via GitHub secrets | App Service settings | App Service settings |
| **JWT Keys** | Separate RSA pair | Separate RSA pair | Separate RSA pair |
| **Publish Profiles** | 2 separate (API/Admin) | ⚠️ **Should be 3 separate** (API/Admin/PWA)<br>Currently using 1 shared (incorrect) | 2 separate (API/Admin) |
| **Static Web App** | mango-water-04fdb1c03 | N/A (uses App Service for PWA) | blue-water-0eab7991e |

## Validation

After adding secrets, workflows will validate them automatically:
- ✅ Infrastructure deployment workflows check for `AZURE_CREDENTIALS` and `JWT_SECRET_KEY`
- ✅ API deployment workflows check for publish profiles and JWT keys
- ❌ Missing secrets cause workflows to fail with clear error messages

## Troubleshooting

**Workflow fails with "Secret not set":**
- Verify secret name matches exactly (case-sensitive)
- Check that secret value is not empty
- Ensure you're using the correct environment (dev/staging/prod)

**JWT authentication fails:**
- Ensure RSA keys are properly formatted (use provided script)
- Verify private/public key pair matches
- Check that keys are environment-specific

**Azure deployment fails:**
- Verify `AZURE_CREDENTIALS` JSON format is correct
- Ensure service principal has contributor role
- Check subscription ID matches

## Related Documentation

- 📖 [Complete GitHub Secrets Guide](GITHUB_SECRETS_VARIABLES.md) - Full documentation with troubleshooting
- 📖 [Secrets Management Guide](SECRETS_MANAGEMENT.md) - Local development and Azure Key Vault
- 📖 [Email Setup Guide](EMAIL_SETUP.md) - Azure Communication Services configuration

---

**Last Updated**: 2025-12-08  
**For Support**: Check workflow logs for specific error messages
