# CI/CD Architecture

This document describes our continuous integration and continuous deployment (CI/CD) architecture.

## Terminology Glossary

> **Important**: These terms are used interchangeably but mean the same thing.

| Term | Also Known As | What It Actually Is |
|------|---------------|---------------------|
| **PWA** | SWA, Frontend, Blazor WASM | The Blazor WebAssembly frontend (`Mystira.App.PWA`). Deployed to Azure Static Web App. |
| **SWA** | PWA, Frontend | Azure Static Web App - the hosting service for the PWA |
| **API** | Backend | The .NET API (`Mystira.App.Api`). Deployed to Azure App Service. |

## Quick Reference: How to Deploy to Dev

### API Deployment (App Service)

| Method | How |
|--------|-----|
| **Automatic** | Push to `dev` branch (triggers `[Deploy] Trigger Workspace` → full build/deploy) |
| **Manual** | Run `[Deploy] Trigger Workspace` → select `api` |

This triggers a full build and deploy to Azure App Service via the workspace repo.

### SWA/PWA Deployment (Static Web App)

| Method | How |
|--------|-----|
| **Automatic** | Push to `dev` branch → Azure SWA CI/CD deploys directly |
| **Manual** | Run `[Deploy] Trigger Workspace` → select `pwa` ⚠️ |

> ⚠️ **Important**: The `[Deploy] Trigger Workspace` workflow for PWA **does NOT deploy directly**.
> It only updates the submodule reference in `Mystira.workspace`.
> The actual SWA deployment is handled by **Azure Static Web Apps CI/CD** which monitors this repo directly.
>
> **Translation**: Just push to `dev` and Azure handles the SWA deployment automatically.

### Workspace Event Types Reference

| Event Type | What It Does |
|------------|--------------|
| `app-deploy` | Full API deployment (build + deploy to App Service) |
| `app-swa-deploy` | Just updates submodule ref (SWA deploys itself via Azure) |
| `devhub-deploy` | Just updates submodule ref |
| `story-generator-swa-deploy` | Just updates submodule ref |

## Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CI/CD Pipeline                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  This Repo (Mystira.App)              Mystira.workspace                     │
│  ┌──────────┐     ┌──────────┐        ┌──────────┐     ┌──────────┐        │
│  │  Build   │────▶│   Test   │───────▶│  Deploy  │────▶│   Live   │        │
│  └──────────┘     └──────────┘        └──────────┘     └──────────┘        │
│       CI workflows               repository_dispatch    Actual deploy       │
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

### CI Workflows (Build & Test Only)

These workflows run tests but **do NOT deploy**:

| Workflow | File | Triggers On |
|----------|------|-------------|
| `[API] Build & Test (Dev)` | `api-ci-dev.yml` | Push/PR to `dev` with API changes |
| `[PWA] Build & Test (Dev)` | `pwa-ci-dev.yml` | Push/PR to `dev` with PWA changes |
| `[CI] Tests & Coverage` | `ci-tests.yml` | All PRs |

### Deployment Workflows

| Workflow | File | What It Does |
|----------|------|--------------|
| `[Deploy] Trigger Workspace` | `deploy-trigger-workspace.yml` | Sends deploy event to `Mystira.workspace` |
| `[API] Rollback (Production)` | `api-rollback-prod.yml` | Rollback API to previous version |

### Supporting Workflows

| Workflow | File | Purpose |
|----------|------|---------|
| `[PWA] Cleanup Preview Environments` | `pwa-cleanup-preview.yml` | Cleans up PR preview deployments |
| `[PWA] Smoke Tests [Preview]` | `pwa-smoke-tests.yml` | Runs smoke tests on preview URLs |
| Security Scanning | `security-scanning.yml` | CodeQL and dependency scanning |

### Where Actual Deployment Happens

| Component | Deployed By |
|-----------|-------------|
| **API** | `Mystira.workspace` repo (via `repository_dispatch`) |
| **SWA/PWA** | Azure Static Web Apps CI/CD (monitors this repo directly) |

### Infrastructure Workflows

> ⚠️ **DEPRECATED**: Infrastructure workflows have been removed from this repository.
> Infrastructure is now managed via Terraform in [Mystira.workspace](https://github.com/phoenixvc/Mystira.workspace).

## Manual Deployment

### How to Trigger Manual API Deployment

1. Go to **Actions** tab in GitHub
2. Select **`[Deploy] Trigger Workspace`** from the sidebar
3. Click **"Run workflow"** dropdown
4. Select component:
   - `api` - Deploy API only
   - `pwa` - Update PWA submodule ref (SWA deploys itself)
   - `all` - Both
5. Click **"Run workflow"**

### How SWA/PWA Actually Deploys

The SWA deployment is **not triggered via GitHub Actions in this repo**.

Azure Static Web Apps has its own CI/CD that:
- Monitors this repo directly
- Triggers on push to `dev`/`staging`/`main` branches
- Deploys automatically without needing manual intervention

To manually redeploy the SWA, just push a commit to the target branch.

### Monitor Deployment Progress

- **API deployment**: Check [Mystira.workspace Actions](https://github.com/phoenixvc/Mystira.workspace/actions)
- **SWA deployment**: Check Azure Portal → Static Web App → Deployment history

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
