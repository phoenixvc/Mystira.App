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

> ⚠️ **Note**: Infrastructure is now managed via Terraform in [Mystira.workspace](https://github.com/phoenixvc/Mystira.workspace).
> See the centralized infrastructure repository for deployment workflows.

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
- Infrastructure: [Mystira.workspace Terraform](https://github.com/phoenixvc/Mystira.workspace)
- SKU: Free/Basic tiers

### Staging Environment

| Trigger | Action |
|---------|--------|
| Push to `staging` | Auto-deploy |
| PR to `staging` | Preview only |
| Manual workflow dispatch | Deploy on-demand |

**Configuration**:
- Resource Group: `mys-staging-mystira-rg-san`
- Infrastructure: [Mystira.workspace Terraform](https://github.com/phoenixvc/Mystira.workspace)
- SKU: Standard tiers

### Production Environment

| Trigger | Action |
|---------|--------|
| Tag `v*.*.*` | Auto-deploy (with approval gate) |
| Manual workflow dispatch | Deploy on-demand (with approval gate) |

**Configuration**:
- Resource Group: `mys-prod-mystira-rg-san`
- Infrastructure: [Mystira.workspace Terraform](https://github.com/phoenixvc/Mystira.workspace)
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

> ⚠️ **DEPRECATED**: Infrastructure workflows have been removed from this repository.
> Infrastructure is now managed via Terraform in [Mystira.workspace](https://github.com/phoenixvc/Mystira.workspace).

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

> ⚠️ **DEPRECATED**: Infrastructure is now managed via Terraform in [Mystira.workspace](https://github.com/phoenixvc/Mystira.workspace).
> See the centralized repository for Terraform deployment procedures.

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

> ⚠️ Infrastructure is now managed via Terraform in [Mystira.workspace](https://github.com/phoenixvc/Mystira.workspace).
> For rollback procedures, see the centralized Terraform documentation.

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
