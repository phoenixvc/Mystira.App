# Migration Guide: Infrastructure Extraction to Mystira.Infra

This guide documents the process for extracting infrastructure from `Mystira.App` to a dedicated `Mystira.Infra` repository.

## Current State

### Existing Infrastructure Location

All infrastructure-as-code currently resides in `Mystira.App`:

```
Mystira.App/
├── infrastructure/
│   ├── main.bicep           # Main orchestration
│   ├── params.dev.json
│   ├── params.staging.json
│   ├── params.prod.json
│   └── modules/
│       ├── app-service.bicep
│       ├── application-insights.bicep
│       ├── azure-bot.bicep
│       ├── communication-services.bicep
│       ├── cosmos-db.bicep
│       ├── dns-zone.bicep
│       ├── key-vault.bicep
│       ├── log-analytics.bicep
│       ├── static-web-app.bicep
│       └── storage.bicep
└── .github/workflows/
    ├── infrastructure-deploy-dev.yml
    ├── infrastructure-deploy-staging.yml
    └── infrastructure-deploy-prod.yml
```

### CI/CD Requirements

To enable current CI/CD, configure these GitHub secrets in Mystira.App:

#### Required Secrets

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AZURE_CREDENTIALS` | Service principal JSON | See below |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | Azure Portal → Subscriptions |
| `JWT_RSA_PRIVATE_KEY` | RSA private key (PEM) | Generate with OpenSSL |
| `JWT_RSA_PUBLIC_KEY` | RSA public key (PEM) | Extract from private key |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` | App Service publish profile | Azure Portal → App Service → Download |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` | Staging publish profile | Same as above |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` | Production publish profile | Same as above |
| `AZURE_ACS_CONNECTION_STRING` | Azure Communication Services | Azure Portal → ACS → Keys |
| `AZURE_ACS_SENDER_EMAIL_DEV` | Sender email for dev | e.g., `DoNotReply@mystira.app` |

#### Creating Azure Credentials

```bash
# Create service principal for GitHub Actions
az ad sp create-for-rbac \
  --name "github-mystira-deployer" \
  --role Contributor \
  --scopes /subscriptions/<subscription-id> \
  --sdk-auth

# Output goes into AZURE_CREDENTIALS secret (JSON format)
```

#### Generating JWT Keys

```bash
# Generate RSA private key
openssl genrsa -out jwt-private.pem 2048

# Extract public key
openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem

# Copy contents (including BEGIN/END markers) to secrets
cat jwt-private.pem  # → JWT_RSA_PRIVATE_KEY
cat jwt-public.pem   # → JWT_RSA_PUBLIC_KEY
```

---

## Phase 1: Enable Current Deployments

Before migration, ensure current setup works:

### 1.1 Configure Secrets

Go to: GitHub → Mystira.App → Settings → Secrets and variables → Actions

Add each secret from the table above.

### 1.2 Test Infrastructure Deployment

```bash
# Trigger manually from Actions tab
# Or push change to infrastructure/
```

### 1.3 Test Application Deployment

Once infrastructure is deployed, the API/Admin API workflows should succeed.

---

## Phase 2: Create Mystira.Infra Repository

### 2.1 Create Repository

```bash
# Via GitHub CLI
gh repo create phoenixvc/Mystira.Infra --private \
  --description "Infrastructure as Code for Mystira platform"
```

### 2.2 Initialize Structure

```bash
mkdir -p Mystira.Infra/{modules/{compute,data,networking,security,communication,monitoring},apps/{mystira-app,mystira-chain,mystira-storygenerator},shared,scripts}
cd Mystira.Infra
git init
```

### 2.3 Copy Existing Modules

```bash
# From Mystira.App to Mystira.Infra
cp ../Mystira.App/infrastructure/modules/app-service.bicep modules/compute/
cp ../Mystira.App/infrastructure/modules/static-web-app.bicep modules/compute/
cp ../Mystira.App/infrastructure/modules/cosmos-db.bicep modules/data/
cp ../Mystira.App/infrastructure/modules/key-vault.bicep modules/data/
cp ../Mystira.App/infrastructure/modules/storage.bicep modules/data/
cp ../Mystira.App/infrastructure/modules/azure-bot.bicep modules/communication/
cp ../Mystira.App/infrastructure/modules/communication-services.bicep modules/communication/
cp ../Mystira.App/infrastructure/modules/log-analytics.bicep modules/monitoring/
cp ../Mystira.App/infrastructure/modules/application-insights.bicep modules/monitoring/
cp ../Mystira.App/infrastructure/modules/dns-zone.bicep modules/networking/

# Copy main orchestration
cp ../Mystira.App/infrastructure/main.bicep apps/mystira-app/
cp ../Mystira.App/infrastructure/params.*.json apps/mystira-app/
```

### 2.4 Add New Modules

Create these new modules for missing capabilities:

#### modules/networking/vnet.bicep
```bicep
// Virtual Network with app and private endpoint subnets
// See ADR-0012 for design
```

#### modules/security/front-door.bicep
```bicep
// Azure Front Door with WAF
// See ADR-0012 for design
```

#### modules/compute/container-app.bicep
```bicep
// Container Apps for Python services (Mystira.Chain)
// See ADR-0010 for design
```

---

## Phase 3: Create Migration Script

### scripts/migrate-from-app.sh

```bash
#!/bin/bash
# Migrate infrastructure from Mystira.App to Mystira.Infra

set -e

SOURCE_REPO="../Mystira.App/infrastructure"
DEST_DIR="."

echo "Migrating infrastructure from Mystira.App..."

# Check source exists
if [ ! -d "$SOURCE_REPO" ]; then
    echo "Error: Source not found at $SOURCE_REPO"
    exit 1
fi

# Copy modules with reorganization
echo "Copying modules..."

# Compute
mkdir -p modules/compute
cp "$SOURCE_REPO/modules/app-service.bicep" modules/compute/
cp "$SOURCE_REPO/modules/static-web-app.bicep" modules/compute/

# Data
mkdir -p modules/data
cp "$SOURCE_REPO/modules/cosmos-db.bicep" modules/data/
cp "$SOURCE_REPO/modules/key-vault.bicep" modules/data/
cp "$SOURCE_REPO/modules/storage.bicep" modules/data/

# Communication
mkdir -p modules/communication
cp "$SOURCE_REPO/modules/azure-bot.bicep" modules/communication/
cp "$SOURCE_REPO/modules/communication-services.bicep" modules/communication/

# Monitoring
mkdir -p modules/monitoring
cp "$SOURCE_REPO/modules/log-analytics.bicep" modules/monitoring/
cp "$SOURCE_REPO/modules/application-insights.bicep" modules/monitoring/

# Networking (existing)
mkdir -p modules/networking
cp "$SOURCE_REPO/modules/dns-zone.bicep" modules/networking/

# App-specific orchestration
mkdir -p apps/mystira-app
cp "$SOURCE_REPO/main.bicep" apps/mystira-app/
cp "$SOURCE_REPO/params.dev.json" apps/mystira-app/
cp "$SOURCE_REPO/params.staging.json" apps/mystira-app/
cp "$SOURCE_REPO/params.prod.json" apps/mystira-app/

echo "Migration complete!"
echo ""
echo "Next steps:"
echo "1. Update module paths in apps/mystira-app/main.bicep"
echo "2. Create new modules (vnet.bicep, front-door.bicep, container-app.bicep)"
echo "3. Add Mystira.Chain and Mystira.StoryGenerator orchestration"
echo "4. Update GitHub workflows"
```

---

## Phase 4: Update Module Paths

After migration, update `apps/mystira-app/main.bicep` to use new paths:

```bicep
// Before (in Mystira.App)
module appInsights 'modules/application-insights.bicep' = { ... }

// After (in Mystira.Infra)
module appInsights '../../modules/monitoring/application-insights.bicep' = { ... }
```

---

## Phase 5: Set Up New CI/CD

### Mystira.Infra GitHub Workflows

Create `.github/workflows/deploy.yml` that:
1. Validates Bicep syntax
2. Runs what-if for PRs
3. Deploys on merge to main

### Update Mystira.App Workflows

Option A: Remove infrastructure workflows from Mystira.App (delegate to Mystira.Infra)
Option B: Trigger Mystira.Infra workflows from Mystira.App

---

## Rollback Plan

If migration causes issues:

1. Keep `Mystira.App/infrastructure/` unchanged during transition
2. Both repos can deploy to same Azure resources
3. Remove from Mystira.App only after Mystira.Infra is proven

---

## Verification Checklist

- [ ] All GitHub secrets configured in Mystira.Infra
- [ ] Bicep validates without errors
- [ ] What-if shows expected changes (no unexpected deletions)
- [ ] Dev environment deploys successfully
- [ ] Staging environment deploys successfully
- [ ] Production deployment tested (manual approval)
- [ ] Application CI/CD still works
- [ ] Monitoring and alerts functioning

---

## Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Enable current CI/CD | 1-2 days | Secrets configuration |
| Create Mystira.Infra | 2-3 days | Phase 1 complete |
| Add new modules | 3-5 days | Phase 2 complete |
| Update workflows | 1-2 days | Phase 3 complete |
| Testing & validation | 2-3 days | All phases |
| Deprecate old infra | 1 day | Full validation |

---

## Support

For issues during migration:
- Check Azure deployment logs
- Validate Bicep syntax: `az bicep build --file main.bicep`
- Review what-if output before deployment
- Contact DevOps team for Azure access issues
