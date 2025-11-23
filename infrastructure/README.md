# Mystira Infrastructure as Code

This directory contains Bicep templates for deploying the Mystira application infrastructure to Azure.

## Overview

The infrastructure is organized by environment:
- `dev/` - Development environment (West Europe)
- `prod/` - Production environment (West US)

## Prerequisites

### Azure Setup

1. **Azure Subscription**: Phoenix Azure Sponsorship (ID: `22f9eb18-6553-4b7d-9451-47d0195085fe`)
2. **Resource Group**: `dev-euw-rg-mystira` (Development) - must be created manually
3. **Service Principal**: For GitHub Actions authentication

### Required Secrets

The following secrets must be configured in GitHub repository settings:

#### Azure Authentication
- `AZURE_CLIENT_ID` - Service Principal client ID
- `AZURE_TENANT_ID` - Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID` - Already set as env var in workflow

#### Application Secrets
- `JWT_SECRET_KEY` - Secret key for JWT token generation
- `ACS_CONNECTION_STRING` - Azure Communication Services connection string (optional)

#### Azure Web App Publish Profiles (for API deployment)
- `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` - For main API (dev)
- `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` - For admin API (dev)
- `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` - For main API (prod)
- `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_ADMIN` - For admin API (prod)

## Dev Environment Resources

The development environment includes:

### Core Infrastructure
- **Log Analytics Workspace**: `dev-euw-log-mystira`
  - Centralized logging and monitoring
  - 30-day retention
  - 1GB daily cap

- **Application Insights**: `dev-euw-ai-mystira`
  - Application performance monitoring
  - Integrated with Log Analytics

### Communication Services
- **Azure Communication Services**: `dev-euw-acs-mystira`
  - Email and SMS capabilities
  - Global deployment

- **Email Communication Service**: `dev-euw-ecs-mystira`
  - Domain: `mystira.app`
  - Sender: `DoNotReply@mystira.app`

### Storage & Database
- **Storage Account**: `mystiraappdevstorage`
  - Container: `mystira-app-media`
  - Public blob access enabled
  - CORS configured for PWA origins

- **Cosmos DB**: `mystiraappdevcosmos`
  - Database: `MystiraAppDb`
  - Serverless mode
  - Containers:
    - UserProfiles
    - Accounts
    - Scenarios
    - GameSessions
    - ContentBundles
    - PendingSignups

### Application Hosting
- **Main API App Service**: `mystira-app-dev-api`
  - SKU: B1 (Basic)
  - Runtime: .NET 9.0 on Linux
  - Health check: `/health`
  - Integrated with App Insights

- **Admin API App Service**: `dev-euw-app-mystora-admin-api`
  - SKU: B1 (Basic)
  - Runtime: .NET 9.0 on Linux
  - Health check: `/health`
  - Integrated with App Insights

### Frontend
- **Static Web App**: `dev-euw-swa-mystira-app`
  - URL: `https://mango-water-04fdb1c03.3.azurestaticapps.net`
  - SKU: Standard
  - Connected to GitHub branch: `dev`
  - Workflow: `azure-static-web-apps-mango-water-04fdb1c03.yml`

## Deployment

### Automatic Deployment

Infrastructure is automatically deployed when:
1. Code is pushed to the `dev` branch with changes in `infrastructure/dev/`
2. A PR is merged to `dev` with infrastructure changes
3. Manually triggered via GitHub Actions workflow dispatch

### Manual Deployment

#### Using Azure CLI

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription 22f9eb18-6553-4b7d-9451-47d0195085fe

# Create resource group (if it doesn't exist)
az group create \
  --name dev-euw-rg-mystira \
  --location westeurope

# Deploy infrastructure
az deployment group create \
  --resource-group dev-euw-rg-mystira \
  --template-file infrastructure/dev/main.bicep \
  --parameters infrastructure/dev/main.parameters.json \
  --parameters jwtSecretKey="<your-secret>" \
  --parameters acsConnectionString="<your-acs-connection>"
```

#### Using GitHub Actions

1. Go to Actions → "Infrastructure Deployment - Dev Environment"
2. Click "Run workflow"
3. Select the `dev` branch
4. Click "Run workflow"

### Preview Changes (What-If)

Before deploying, you can preview what changes will be made:

```bash
az deployment group what-if \
  --resource-group dev-euw-rg-mystira \
  --template-file infrastructure/dev/main.bicep \
  --parameters infrastructure/dev/main.parameters.json \
  --parameters jwtSecretKey="<your-secret>" \
  --parameters acsConnectionString="<your-acs-connection>"
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

### Development Environment
- **Cosmos DB**: Serverless mode (pay per request)
- **App Services**: B1 tier (Basic, can be scaled down to F1 Free tier if needed)
- **Storage**: Standard LRS (Locally Redundant Storage)
- **Log Analytics**: 1GB daily cap
- **Application Insights**: Standard pricing

### Estimated Monthly Cost (Development)
- Cosmos DB: ~$5-20 (serverless, depends on usage)
- App Services (2x B1): ~$27.74
- Storage Account: ~$1-5
- Log Analytics + App Insights: ~$5-10
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
