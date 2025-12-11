# CI/CD Architecture

This document describes our continuous integration and continuous deployment (CI/CD) architecture.

## Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CI/CD Pipeline                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐           │
│  │  Build   │────▶│   Test   │────▶│ Validate │────▶│  Deploy  │           │
│  └──────────┘     └──────────┘     └──────────┘     └──────────┘           │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Feature Branch ──▶ dev ──▶ main ──▶ Tag v*.*.* ──▶ Production             │
│       │              │        │           │                                 │
│       ▼              ▼        ▼           ▼                                 │
│    PR Checks    Dev Env   Staging    Production                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Pipeline Stages

### 1. Build Stage

**Triggers**: All pushes and pull requests

- Restore dependencies (`dotnet restore`)
- Build solution (`dotnet build`)
- Compile TypeScript (if applicable)
- Generate artifacts

### 2. Test Stage

**Triggers**: All pushes and pull requests

- Run unit tests (`dotnet test`)
- Run integration tests
- Generate code coverage reports
- Lint and style checks

### 3. Infrastructure Validation

**Triggers**: Changes to `infrastructure/**` files

- Validate Bicep templates (`az deployment group validate`)
- Run what-if analysis (`az deployment group what-if`)
- Comment results on PR

### 4. Deployment Stage

**Triggers**: Based on branch/tag (see below)

- Deploy infrastructure (if changed)
- Deploy application code
- Run smoke tests
- Update deployment status

## Environment Deployments

### Development Environment

| Trigger | Action |
|---------|--------|
| Push to `dev` | Auto-deploy |
| PR to `dev` | Preview only |
| Manual workflow dispatch | Deploy on-demand |

**Configuration**:
- Resource Group: `mys-dev-mystira-rg-san`
- Parameters: `infrastructure/params.dev.json`
- SKU: Free/Basic tiers

### Staging Environment

| Trigger | Action |
|---------|--------|
| Push to `staging` | Auto-deploy |
| PR to `staging` | Preview only |
| Manual workflow dispatch | Deploy on-demand |

**Configuration**:
- Resource Group: `mys-staging-mystira-rg-san`
- Parameters: `infrastructure/params.staging.json`
- SKU: Standard tiers

### Production Environment

| Trigger | Action |
|---------|--------|
| Tag `v*.*.*` | Auto-deploy (with approval gate) |
| Manual workflow dispatch | Deploy on-demand (with approval gate) |

**Configuration**:
- Resource Group: `mys-prod-mystira-rg-san`
- Parameters: `infrastructure/params.prod.json`
- SKU: Premium tiers
- Manual approval required

## Workflow Files

### Application Workflows

| Workflow | File | Purpose | Manual Deploy |
|----------|------|---------|---------------|
| API CI/CD - Dev | `.github/workflows/mystira-app-api-cicd-dev.yml` | Build, test, deploy API to dev | ✅ |
| API CI/CD - Staging | `.github/workflows/mystira-app-api-cicd-staging.yml` | Build, test, deploy API to staging | ✅ |
| API CI/CD - Prod | `.github/workflows/mystira-app-api-cicd-prod.yml` | Build, test, deploy API to production | ✅ |
| Admin API CI/CD - Dev | `.github/workflows/mystira-app-admin-api-cicd-dev.yml` | Build, test, deploy Admin API to dev | ✅ |
| Admin API CI/CD - Staging | `.github/workflows/mystira-app-admin-api-cicd-staging.yml` | Build, test, deploy Admin API to staging | ✅ |
| Admin API CI/CD - Prod | `.github/workflows/mystira-app-admin-api-cicd-prod.yml` | Build, test, deploy Admin API to production | ✅ |
| PWA CI/CD - Dev | `.github/workflows/mystira-app-pwa-cicd-dev.yml` | Build, test, deploy PWA to dev | - |
| PWA CI/CD - Staging | `.github/workflows/mystira-app-pwa-cicd-staging.yml` | Build, test, deploy PWA to staging | - |
| PWA CI/CD - Prod | `.github/workflows/mystira-app-pwa-cicd-prod.yml` | Build, test, deploy PWA to production | - |

### Infrastructure Workflows

| Workflow | File | Purpose |
|----------|------|---------|
| Infra Dev | `.github/workflows/infrastructure-deploy-dev.yml` | Dev infrastructure |
| Infra Staging | `.github/workflows/infrastructure-deploy-staging.yml` | Staging infrastructure |
| Infra Prod | `.github/workflows/infrastructure-deploy-prod.yml` | Production infrastructure |

## Manual Deployment

All API and Admin API workflows support manual deployment invocation via GitHub's `workflow_dispatch` event.

### How to Trigger Manual Deployment

1. **Navigate to Actions Tab**
   - Go to the GitHub repository
   - Click on the **Actions** tab

2. **Select Workflow**
   - Choose the desired workflow from the left sidebar:
     - "API CI/CD - Dev Environment"
     - "API CI/CD - Staging Environment"
     - "API CI/CD - Production Environment"
     - "Admin API CI/CD - Dev Environment"
     - "Admin API CI/CD - Staging Environment"
     - "Admin API CI/CD - Production Environment"

3. **Run Workflow**
   - Click the **"Run workflow"** dropdown button (top right)
   - Select the target branch that corresponds to your desired environment:
     - `dev` branch → deploys to **Development** environment
     - `staging` branch → deploys to **Staging** environment
     - `main` branch → deploys to **Production** environment
   - Click **"Run workflow"** to start the deployment

4. **Monitor Progress**
   - The workflow will appear in the workflow runs list
   - Click on the run to see real-time logs
   - Deployment will go through: Build → Test → Deploy stages

### When to Use Manual Deployment

- **Hotfix Deployment**: Deploy critical fixes without waiting for merge
- **Rollback**: Re-deploy a previous stable version
- **Configuration Changes**: Deploy after updating environment variables or secrets
- **Infrastructure Updates**: Deploy after infrastructure changes complete
- **Testing**: Verify deployment pipeline changes

### Manual Deployment Behavior

- **Development**: Deploys immediately after build and test pass
- **Staging**: Deploys immediately after build and test pass
- **Production**: Requires manual approval in GitHub environment settings

## Deployment Strategy

### Infrastructure Deployment

All infrastructure deployments use **Incremental mode** for safety:

```yaml
az deployment group create \
  --mode Incremental \
  --template-file './infrastructure/main.bicep' \
  --parameters '@./infrastructure/params.dev.json'
```

**Incremental Mode Benefits**:
- Only creates/updates resources defined in template
- Never deletes existing resources not in template
- Safer for production environments

### Application Deployment

Applications deploy using Azure App Service deployment slots:

1. Deploy to staging slot
2. Run health checks
3. Swap slots (zero-downtime)
4. Keep previous version in staging slot (easy rollback)

## Secrets Management

### Required Secrets

| Secret | Description | Used In |
|--------|-------------|---------|
| `AZURE_CREDENTIALS` | Service principal credentials | All Azure deployments |
| `AZURE_SUBSCRIPTION_ID` | Target subscription | All Azure deployments |
| `JWT_SECRET_KEY` | JWT signing key | App configuration |

### Secret Configuration

Secrets are configured per environment in GitHub:

- **development**: Dev environment secrets
- **staging**: Staging environment secrets
- **production**: Production environment secrets

## Status Checks

### Required Checks for PRs

| Check | Description |
|-------|-------------|
| `build` | .NET build succeeds |
| `test` | All tests pass |
| `lint` | Code style validation |
| `infra-validate` | Bicep validation (if infra changes) |

## Monitoring & Notifications

### Deployment Notifications

- **Success**: Posted to deployment status
- **Failure**: Alert sent to team channel

### Monitoring Integration

All environments send telemetry to:
- Application Insights (per environment)
- Log Analytics Workspace (per environment)

## Rollback Procedures

### Application Rollback

```bash
# Swap back to previous deployment
az webapp deployment slot swap \
  --resource-group mys-prod-mystira-rg-san \
  --name mys-prod-mystira-api-san \
  --slot staging \
  --target-slot production
```

### Infrastructure Rollback

For infrastructure issues:

1. Identify the last known good deployment
2. Re-run that deployment with same parameters
3. Or restore from backup if data loss occurred

## Pipeline Variables

### Common Variables

```yaml
env:
  DOTNET_VERSION: '9.0.x'
  NODE_VERSION: '20.x'
  AZURE_LOCATION: 'southafricanorth'
```

### Environment-Specific Variables

| Variable | Dev | Staging | Prod |
|----------|-----|---------|------|
| `RESOURCE_GROUP` | mys-dev-mystira-rg-san | mys-staging-mystira-rg-san | mys-prod-mystira-rg-san |
| `APP_NAME` | mys-dev-mystira-api-san | mys-staging-mystira-api-san | mys-prod-mystira-api-san |

## Security Considerations

1. **Least Privilege**: Service principals have minimal required permissions
2. **Secret Rotation**: Secrets rotated regularly
3. **Audit Logging**: All deployments logged
4. **Approval Gates**: Production requires manual approval
5. **Branch Protection**: Main branches are protected
