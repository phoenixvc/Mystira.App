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

**Configuration**:
- Resource Group: `mys-dev-mystira-rg-euw`
- Parameters: `infrastructure/params.dev.json`
- SKU: Free/Basic tiers

### Staging Environment

| Trigger | Action |
|---------|--------|
| Push to `main` | Auto-deploy |
| PR to `main` | Preview only |

**Configuration**:
- Resource Group: `mys-staging-mystira-rg-euw`
- Parameters: `infrastructure/params.staging.json`
- SKU: Standard tiers

### Production Environment

| Trigger | Action |
|---------|--------|
| Tag `v*.*.*` | Auto-deploy (with approval gate) |

**Configuration**:
- Resource Group: `mys-prod-mystira-rg-euw`
- Parameters: `infrastructure/params.prod.json`
- SKU: Premium tiers
- Manual approval required

## Workflow Files

### Application Workflows

| Workflow | File | Purpose |
|----------|------|---------|
| CI Build | `.github/workflows/ci.yml` | Build and test on all branches |
| Deploy Dev | `.github/workflows/deploy-dev.yml` | Deploy to development |
| Deploy Staging | `.github/workflows/deploy-staging.yml` | Deploy to staging |
| Deploy Prod | `.github/workflows/deploy-prod.yml` | Deploy to production |

### Infrastructure Workflows

| Workflow | File | Purpose |
|----------|------|---------|
| Infra Dev | `.github/workflows/infrastructure-deploy-dev.yml` | Dev infrastructure |
| Infra Staging | `.github/workflows/infrastructure-deploy-staging.yml` | Staging infrastructure |
| Infra Prod | `.github/workflows/infrastructure-deploy-prod.yml` | Production infrastructure |

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
  --resource-group mys-prod-mystira-rg-euw \
  --name mys-prod-mystira-api-euw \
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
  DOTNET_VERSION: '8.0.x'
  NODE_VERSION: '20.x'
  AZURE_LOCATION: 'westeurope'
```

### Environment-Specific Variables

| Variable | Dev | Staging | Prod |
|----------|-----|---------|------|
| `RESOURCE_GROUP` | mys-dev-mystira-rg-euw | mys-staging-mystira-rg-euw | mys-prod-mystira-rg-euw |
| `APP_NAME` | mys-dev-mystira-api-euw | mys-staging-mystira-api-euw | mys-prod-mystira-api-euw |

## Security Considerations

1. **Least Privilege**: Service principals have minimal required permissions
2. **Secret Rotation**: Secrets rotated regularly
3. **Audit Logging**: All deployments logged
4. **Approval Gates**: Production requires manual approval
5. **Branch Protection**: Main branches are protected
