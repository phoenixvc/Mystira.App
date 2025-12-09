# Mystira Infrastructure as Code

This directory contains Bicep templates for deploying the Mystira application infrastructure to Azure.

## Naming Convention

All resources follow the pattern: `[org]-[env]-[project]-[type]-[region]`

| Segment | Description | Values |
|---------|-------------|--------|
| `org` | Organisation code | `mys` (Mystira), `nl` (NeuralLiquid), `pvc` (Phoenix VC), `tws` (Twines & Straps) |
| `env` | Environment | `dev`, `staging`, `prod` |
| `project` | Project name | `mystira`, `mystira-story` |
| `type` | Resource type | `api`, `app`, `log`, `cosmos`, `storage`, `kv`, `acs`, `bot`, `swa`, etc. |
| `region` | Region code | `san` (South Africa North - **primary**), `eus2` (East US 2 - fallback), `euw` (West Europe), etc. |

### Resource Group Pattern
```
[org]-[env]-[project]-rg-[region]
```

### Examples (South Africa North)

| Resource | Name |
|----------|------|
| Dev Resource Group | `mys-dev-mystira-rg-san` |
| Dev API App Service | `mys-dev-mystira-api-san` |
| Dev Cosmos DB | `mys-dev-mystira-cosmos-san` |
| Dev Log Analytics | `mys-dev-mystira-log-san` |
| Dev Static Web App | `mys-dev-mystira-swa-eus2` (fallback region - see below) |
| Prod API App Service | `mys-prod-mystira-api-san` |
| Prod Storage Account | `mysprodmystirstsan` (no dashes for storage) |

## Regional Availability

**Primary Region: South Africa North (southafricanorth / san)**

Not all Azure services are available in South Africa North. The infrastructure uses fallback regions for services that aren't available locally.

### Service Availability Matrix

| Service | South Africa North | Fallback Region | Notes |
|---------|-------------------|-----------------|-------|
| **App Service** | ✅ Available | - | Primary compute |
| **Cosmos DB** | ✅ Available | - | NoSQL database |
| **Storage Account** | ✅ Available | - | Blob storage |
| **Key Vault** | ✅ Available | - | Secret management |
| **Log Analytics** | ✅ Available | - | Monitoring |
| **App Insights** | ✅ Available | - | APM |
| **Azure Bot** | ✅ Available (global) | - | Teams/Discord bots |
| **Communication Services** | ✅ Available (global) | - | Email/SMS/WhatsApp |
| **Static Web Apps** | ❌ NOT Available | **eastus2** | PWA hosting |

### Fallback Region Strategy

For services not available in South Africa North, we use **East US 2 (eastus2)** as the fallback region because:
1. It has full service availability
2. Good connectivity to Africa via Azure backbone
3. Lower latency than other fully-featured regions

Static Web Apps are CDN-accelerated globally, so the backend region has minimal impact on user experience.

### ACS Data Location

Azure Communication Services data is stored in **Europe** (closest supported data location to Africa). Supported data locations are:
- United States, Europe, UK, Australia, Japan, Canada, India, France

"Africa" is not yet a supported data location for ACS.

## Overview

The infrastructure uses a single `main.bicep` template with environment-specific parameter files:

```
infrastructure/
├── main.bicep              # Single orchestration template for all environments
├── modules/                # Shared Bicep modules
│   ├── app-service.bicep
│   ├── application-insights.bicep
│   ├── azure-bot.bicep        # Teams/Discord bot (global)
│   ├── communication-services.bicep  # ACS Email/SMS/WhatsApp (global)
│   ├── cosmos-db.bicep
│   ├── key-vault.bicep        # Stores secrets (JWT, Discord, Bot credentials)
│   ├── log-analytics.bicep
│   ├── static-web-app.bicep   # PWA hosting (fallback region)
│   └── storage.bicep
├── params.dev.json         # Development environment parameters (san)
├── params.staging.json     # Staging environment parameters (san)
└── params.prod.json        # Production environment parameters (san)
```

This approach provides:
- **One template, multiple environments** - Same infrastructure code, different configurations
- **Environment parity** - Dev, staging, and prod use identical resource structure
- **Simple CI/CD** - Pipelines choose the right parameter file per environment
- **Consistent naming** - All resources follow `[org]-[env]-[project]-[type]-[region]`

## Prerequisites

### Azure Setup

1. **Azure Subscription**: Phoenix Azure Sponsorship (ID: `22f9eb18-6553-4b7d-9451-47d0195085fe`)
2. **Resource Group**: `mys-dev-mystira-rg-euw` (Development) - must be created manually
3. **Service Principal**: For GitHub Actions authentication

### Required Secrets Setup

The infrastructure deployment pipeline requires several secrets to be configured in your GitHub repository. Follow these steps to set them up:

#### Step 1: Access GitHub Repository Settings

1. Navigate to your repository: `https://github.com/phoenixvc/Mystira.App`
2. Click on **Settings** (top navigation bar)
3. In the left sidebar, expand **Secrets and variables** 
4. Click on **Actions**
5. Click the **New repository secret** button

#### Step 2: Create Azure Service Principal

Before adding secrets, you need to create a Service Principal for GitHub Actions to authenticate with Azure:

```bash
# Login to Azure CLI
az login

# Set your subscription
az account set --subscription 22f9eb18-6553-4b7d-9451-47d0195085fe

# Create a Service Principal with Contributor role
az ad sp create-for-rbac \
  --name "github-actions-mystira-app" \
  --role Contributor \
  --scopes /subscriptions/22f9eb18-6553-4b7d-9451-47d0195085fe/resourceGroups/mys-dev-mystira-rg-euw \
  --sdk-auth

# The output will be a JSON object - save this entire output
```

The command will output JSON like this:
```json
{
  "clientId": "12345678-1234-1234-1234-123456789abc",
  "clientSecret": "your-client-secret-here",
  "subscriptionId": "22f9eb18-6553-4b7d-9451-47d0195085fe",
  "tenantId": "87654321-4321-4321-4321-abcdef123456",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  ...
}
```

#### Step 3: Add Required GitHub Secrets

Add each of the following secrets to your GitHub repository:

##### Azure Authentication Secrets

1. **`AZURE_CLIENT_ID`**
   - **Value**: The `clientId` from the Service Principal JSON output
   - **Example**: `12345678-1234-1234-1234-123456789abc`
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `AZURE_CLIENT_ID`
   - **Used by**: Infrastructure deployment workflow (`.github/workflows/infrastructure-deploy-dev.yml`)

2. **`AZURE_TENANT_ID`**
   - **Value**: The `tenantId` from the Service Principal JSON output
   - **Example**: `87654321-4321-4321-4321-abcdef123456`
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `AZURE_TENANT_ID`
   - **Used by**: Infrastructure deployment workflow

##### Application Secrets

3. **`JWT_SECRET_KEY`**
   - **Value**: A strong, randomly generated secret key for JWT token signing (at least 32 characters)
   - **How to generate**: 
     ```bash
     # Using OpenSSL
     openssl rand -base64 32
     
     # Using PowerShell
     [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
     ```
   - **Example**: `YourSecretKeyHere123456789012345678901234567890==`
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `JWT_SECRET_KEY`
   - **Used by**: Infrastructure deployment (passed to App Services)
   - **⚠️ Important**: Use different keys for dev and prod environments

4. **`ACS_CONNECTION_STRING`** (Optional)
   - **Value**: Azure Communication Services connection string for sending emails
   - **How to get**:
     ```bash
     # List your ACS resources
     az communication list --resource-group mys-dev-mystira-rg-euw

     # Get the connection string
     az communication list-key \
       --name mys-dev-mystira-acs-euw \
       --resource-group mys-dev-mystira-rg-euw \
       --query primaryConnectionString \
       --output tsv
     ```
   - **Example**: `endpoint=https://mys-dev-mystira-acs-euw.communication.azure.com/;accesskey=your-access-key-here`
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `ACS_CONNECTION_STRING`
   - **Used by**: Infrastructure deployment (passed to App Services for email functionality)
   - **Note**: If not provided, the app will log emails instead of sending them

##### Azure Web App Publish Profiles (for API CI/CD)

These secrets are used by the API deployment workflows to publish to Azure App Services:

5. **`AZURE_WEBAPP_PUBLISH_PROFILE_DEV`**
   - **Value**: Publish profile XML for the main API (dev environment)
   - **How to get**:
     ```bash
     # Via Azure CLI
     az webapp deployment list-publishing-profiles \
       --name mys-dev-mystira-api-euw \
       --resource-group mys-dev-mystira-rg-euw \
       --xml
     ```
     Or via Azure Portal:
     1. Go to Azure Portal → App Services
     2. Select `mystira-app-dev-api`
     3. Click **Get publish profile** button (top toolbar)
     4. Copy the entire XML content
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`
   - **Used by**: `.github/workflows/mystira-app-api-cicd-dev.yml`

6. **`AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`**
   - **Value**: Publish profile XML for the admin API (dev environment)
   - **How to get**: Same as above, but for `mys-dev-mystira-adminapi-euw`
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN`
   - **Used by**: `.github/workflows/mystira-app-admin-api-cicd-dev.yml`

7. **`AZURE_WEBAPP_PUBLISH_PROFILE_PROD`**
   - **Value**: Publish profile XML for the main API (production environment)
   - **How to get**: Same as above, but for `mys-prod-mystira-api-euw`
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`
   - **Used by**: `.github/workflows/mystira-app-api-cicd-prod.yml`

8. **`AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`**
   - **Value**: Publish profile XML for the admin API (production environment)
   - **How to get**: Same as above, but for `mys-prod-mystira-adminapi-euw`
   - **Location**: GitHub → Settings → Secrets and variables → Actions → New repository secret
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN`
   - **Used by**: `.github/workflows/mystira-app-admin-api-cicd-prod.yml`

#### Step 4: Verify Secrets Configuration

After adding all secrets, verify them in GitHub:

1. Go to your repository → Settings → Secrets and variables → Actions
2. You should see all 8 secrets listed (or at least the 4 required for infrastructure deployment)
3. The secret values are hidden, but you can see when they were last updated

#### Summary Table

| Secret Name | Required | Used By | Where to Get Value |
|-------------|----------|---------|-------------------|
| `AZURE_CLIENT_ID` | ✅ Yes | Infrastructure deployment | Service Principal JSON → `clientId` |
| `AZURE_TENANT_ID` | ✅ Yes | Infrastructure deployment | Service Principal JSON → `tenantId` |
| `JWT_SECRET_KEY` | ✅ Yes | Infrastructure deployment | Generate with `openssl rand -base64 32` |
| `ACS_CONNECTION_STRING` | ⚠️ Optional | Infrastructure deployment | Azure Communication Services → Connection string |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` | ✅ Yes | API deployment (dev) | Azure Portal → App Service → Get publish profile |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` | ✅ Yes | Admin API deployment (dev) | Azure Portal → App Service → Get publish profile |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` | ✅ Yes | API deployment (prod) | Azure Portal → App Service → Get publish profile |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN` | ✅ Yes | Admin API deployment (prod) | Azure Portal → App Service → Get publish profile |

#### Troubleshooting

**Secret not found error**:
- Ensure secret name matches exactly (case-sensitive)
- Verify you added the secret to the correct repository
- Check if you have admin access to the repository

**Authentication failed**:
- Verify Service Principal has Contributor role on the subscription/resource group
- Ensure `AZURE_CLIENT_ID` and `AZURE_TENANT_ID` are correct
- Check if Service Principal has been deleted or credentials expired

**Deployment fails with "secret not set"**:
- Check workflow YAML references the correct secret name
- Ensure secret is not empty (re-add if needed)

## Dev Environment Resources

The development environment includes the following resources (using the naming convention `[org]-[env]-[project]-[type]-[region]`):

### Core Infrastructure
- **Log Analytics Workspace**: `mys-dev-mystira-log-euw`
  - Centralized logging and monitoring
  - 30-day retention
  - 1GB daily cap

- **Application Insights**: `mys-dev-mystira-appins-euw`
  - Application performance monitoring
  - Integrated with Log Analytics

### Communication Services
- **Azure Communication Services**: `mys-dev-mystira-acs-euw`
  - Email and SMS capabilities
  - Global deployment

- **Email Communication Service**: `mys-dev-mystira-email-euw`
  - Domain: `mystira.app`
  - Sender: `DoNotReply@mystira.app`

### Storage & Database
- **Storage Account**: `mysdevmystirasteruw`
  - Container: `mystira-app-media`
  - SKU: Standard_LRS (Locally Redundant - dev optimized)
  - Public blob access enabled
  - CORS configured for PWA origins

- **Cosmos DB**: `mys-dev-mystira-cosmos-euw`
  - Database: `MystiraAppDb`
  - Serverless mode (pay-per-request - dev optimized)
  - Containers:
    - UserProfiles
    - Accounts
    - Scenarios
    - GameSessions
    - ContentBundles
    - PendingSignups

### Application Hosting
- **App Service Plan**: `mys-dev-mystira-plan-euw` (shared by both APIs)
  - SKU: F1 (Free - dev optimized)

- **Main API App Service**: `mys-dev-mystira-api-euw`
  - Runtime: .NET 9.0 on Linux
  - Health check: `/health`
  - Integrated with App Insights

- **Admin API App Service**: `mys-dev-mystira-adminapi-euw`
  - Runtime: .NET 9.0 on Linux
  - Health check: `/health`
  - Integrated with App Insights

### Frontend
- **Static Web App**: `mys-dev-mystira-swa-euw`
  - URL: `https://mango-water-04fdb1c03.3.azurestaticapps.net`
  - SKU: Standard
  - Connected to GitHub branch: `dev`

## Deployment

### Manual Deployment via GitHub Actions (Recommended)

Infrastructure deployment is **manual only** to prevent accidental changes. Use the GitHub Actions workflow dispatch:

1. Go to **Actions** → **"Infrastructure Deployment - Dev Environment"**
2. Click **"Run workflow"**
3. Select the action to perform:
   - **validate**: Validates Bicep templates without deploying
   - **preview**: Previews infrastructure changes (what-if analysis)
   - **deploy**: Validates and deploys infrastructure
   - **destroy**: Deletes all infrastructure (requires confirmation)
4. Click **"Run workflow"**

**Note**: 
- Destroy action requires checking the "Confirm destruction" checkbox
- Preview action automatically runs on pull requests that modify infrastructure files
- Preview action generates a what-if analysis comment on the PR

### Deployment via CosmosConsole Tool

You can also trigger infrastructure deployments directly from the CosmosConsole tool:

```bash
# Navigate to tools directory
cd tools/Mystira.App.CosmosConsole

# Prerequisites: Install and authenticate with GitHub CLI
gh auth login

# Validate templates
dotnet run -- infrastructure validate

# Preview changes
dotnet run -- infrastructure preview

# Deploy infrastructure
dotnet run -- infrastructure deploy
```

**Benefits of using CosmosConsole for deployment:**
- ✅ Trigger workflows from command line
- ✅ No need to navigate to GitHub Actions UI
- ✅ Integrated with other management operations
- ✅ View workflow status and progress commands

See `tools/Mystira.App.CosmosConsole/README.md` for detailed deployment documentation.

### Manual Deployment via Azure CLI

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription 22f9eb18-6553-4b7d-9451-47d0195085fe

# Create resource group (if it doesn't exist)
# Pattern: [org]-[env]-[project]-rg-[region]

# Dev
az group create --name mys-dev-mystira-rg-euw --location westeurope

# Staging
az group create --name mys-staging-mystira-rg-euw --location westeurope

# Prod
az group create --name mys-prod-mystira-rg-euw --location westeurope

# Deploy infrastructure (choose the appropriate environment)

# Dev deployment
az deployment group create \
  --resource-group mys-dev-mystira-rg-euw \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/params.dev.json \
  --parameters jwtRsaPrivateKey="<your-private-key>" \
  --parameters jwtRsaPublicKey="<your-public-key>"

# Staging deployment
az deployment group create \
  --resource-group mys-staging-mystira-rg-euw \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/params.staging.json \
  --parameters jwtRsaPrivateKey="<your-private-key>" \
  --parameters jwtRsaPublicKey="<your-public-key>"

# Prod deployment
az deployment group create \
  --resource-group mys-prod-mystira-rg-euw \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/params.prod.json \
  --parameters jwtRsaPrivateKey="<your-private-key>" \
  --parameters jwtRsaPublicKey="<your-public-key>"
```

### Data Migration via CosmosConsole Tool

For data migration between environments, use the CosmosConsole tool located in `/tools/Mystira.App.CosmosConsole`:

```bash
# Navigate to tools directory
cd tools/Mystira.App.CosmosConsole

# Set connection strings
export SOURCE_COSMOS_CONNECTION="AccountEndpoint=https://old-cosmos..."
export DEST_COSMOS_CONNECTION="AccountEndpoint=https://new-cosmos..."
export SOURCE_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;..."
export DEST_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;..."

# Run migration
dotnet run -- migrate all
```

See `tools/Mystira.App.CosmosConsole/README.md` for detailed migration documentation.

### Preview Changes (What-If)

Before deploying, you can preview what changes will be made:

```bash
# Dev environment preview
az deployment group what-if \
  --resource-group mys-dev-mystira-rg-euw \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/params.dev.json \
  --parameters jwtRsaPrivateKey="<your-private-key>" \
  --parameters jwtRsaPublicKey="<your-public-key>"

# Staging environment preview
az deployment group what-if \
  --resource-group mys-staging-mystira-rg-euw \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/params.staging.json \
  --parameters jwtRsaPrivateKey="<your-private-key>" \
  --parameters jwtRsaPublicKey="<your-public-key>"

# Prod environment preview
az deployment group what-if \
  --resource-group mys-prod-mystira-rg-euw \
  --template-file infrastructure/main.bicep \
  --parameters @infrastructure/params.prod.json \
  --parameters jwtRsaPrivateKey="<your-private-key>" \
  --parameters jwtRsaPublicKey="<your-public-key>"
```

## Configuration

### Environment Variables

The following environment variables are configured in App Services via Bicep:

- `ASPNETCORE_ENVIRONMENT`: Set to "Development" for dev environment
- `ConnectionStrings__CosmosDb`: Cosmos DB connection string
- `ConnectionStrings__AzureStorage`: Storage account connection string
- `JwtSettings__SecretKey`: JWT signing key
- `JwtSettings__Issuer`: "MystiraAPI"
- `JwtSettings__Audience`: "MystiraPWA"
- `CorsSettings__AllowedOrigins`: Comma-separated list of allowed origins
- `Azure__BlobStorage__ContainerName`: "mystira-app-media"
- `Azure__CosmosDb__DatabaseName`: "MystiraAppDb"
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: App Insights connection string

### CORS Configuration

CORS is configured for the following origins:
- `http://localhost:7000` - Local development
- `https://localhost:7000` - Local development (HTTPS)
- `https://mystira.app` - Production custom domain
- `https://mango-water-04fdb1c03.3.azurestaticapps.net` - Dev PWA
- `https://blue-water-0eab7991e.3.azurestaticapps.net` - Prod PWA

## Monitoring & Diagnostics

### Application Insights

All App Services are instrumented with Application Insights for:
- Request tracking
- Dependency tracking
- Exception tracking
- Custom events and metrics

### Diagnostic Logs

The following logs are collected:
- HTTP logs (7-day retention)
- Console logs (7-day retention)
- Application logs (7-day retention)
- Metrics (7-day retention)

### Health Checks

All App Services have health check endpoints configured at `/health`.

## Cost Optimization

### Development Environment (Optimized for Low Cost)
- **Cosmos DB**: Serverless mode (pay per request - $0.25/million RUs)
- **App Services**: F1 tier (Free tier - $0/month)
  - ⚠️ Note: Free tier has limitations (60 CPU minutes/day, 1GB RAM, 1GB storage)
  - Can upgrade to B1 ($13.14/month) if more resources needed
- **Storage**: Standard_LRS (Locally Redundant Storage - cheapest option)
- **Log Analytics**: 1GB daily cap (first 5GB/month free)
- **Application Insights**: Included with Log Analytics (first 5GB/month free)
- **Communication Services**: Pay per use (emails/SMS)

### Estimated Monthly Cost (Development - Optimized)
- Cosmos DB: ~$1-5 (serverless, low dev usage)
- App Services (2x F1): **$0** (Free tier)
- Storage Account: ~$1-3 (LRS, low usage)
- Log Analytics + App Insights: **$0-5** (within free tier)
- Communication Services: ~$0-2 (pay per use)

**Total: ~$2-15/month** (vs ~$40-70 with B1 App Services)

### Upgrading for Production
When moving to production, consider:
- **Cosmos DB**: Switch to provisioned throughput for predictable performance
- **App Services**: Upgrade to P1v3 or higher for production workloads
- **Storage**: Change to GRS (Geo-Redundant) for high availability
- **Log Analytics**: Remove daily cap, increase retention
- Communication Services: Pay per use

**Total: ~$40-70/month**

## Troubleshooting

### Deployment Failures

1. **Invalid credentials**: Ensure service principal has Contributor role on subscription
2. **Resource name conflicts**: Resource names must be globally unique (especially storage accounts)
3. **Quota limits**: Check subscription quotas for the region

### Application Issues

1. **API not accessible**: Check App Service is running and health endpoint returns 200
2. **Database connection**: Verify Cosmos DB connection string in App Service configuration
3. **CORS errors**: Ensure PWA URL is in the allowed origins list

## Security

### Secrets Management

- All secrets are stored in GitHub repository secrets
- Connection strings are passed as secure parameters
- No secrets are committed to the repository
- App Service uses managed identity where possible

### Network Security

- All services use HTTPS only
- Minimum TLS version: 1.2
- FTPS disabled on App Services
- Public access to blobs (required for media files)

## Maintenance

### Regular Tasks

1. **Update dependencies**: Keep Bicep modules and API versions current
2. **Review costs**: Monitor Azure Cost Management
3. **Review logs**: Check Application Insights for errors and performance
4. **Update secrets**: Rotate JWT keys and connection strings periodically
5. **Backup data**: Cosmos DB has automatic backups, but test restore procedures

### Scaling

To scale the application:

1. **App Services**: Change SKU in Bicep parameters (e.g., B1 → S1 → P1v3)
2. **Cosmos DB**: Switch from serverless to provisioned throughput if needed
3. **Storage**: Change replication (LRS → GRS) for higher availability

## Support

For issues or questions:
1. Check Application Insights logs
2. Review GitHub Actions workflow runs
3. Consult Azure Portal for resource status
4. Contact: support@mystira.app
