# Azure Naming Conventions v2

A unified, opinionated naming standard for all Azure resources across:

- **nl** – NeuralLiquid (Jurie)
- **pvc** – Phoenix VC (Eben)
- **tws** – Twines & Straps (Martyn)
- **mys** – Mystira (Eben)

This document is designed for both humans and AI systems. All patterns herein MUST be followed unless explicitly superseded.

---

## 1. Core Naming Pattern

### **Resources**
```
[org]-[env]-[project]-[type]-[region]
```

### **Resource Groups**
```
[org]-[env]-[project]-rg-[region]
```

### Rules
- Lowercase only
- Allowed characters: `a–z`, `0–9`, `-`
- No spaces, underscores, or trailing hyphens
- Naming must be stable, predictable, and machine-derivable

---

## 2. Segment Vocabulary

### 2.1 Org Codes (Ownership)

| Code | Organisation / Brand | Owner | Notes |
|------|---------------------|-------|-------|
| `nl` | NeuralLiquid | Jurie | Core AI, defence, Autopr, Rooivalk |
| `pvc` | Phoenix VC | Eben | VC brand, investor tooling, market data |
| `tws` | Twines & Straps | Martyn | E‑commerce & operations, Tassa integrations |
| `mys` | Mystira | Eben | Mystira platform + Story generator |

### 2.2 Environment Codes

| Code | Environment | Purpose |
|------|-------------|---------|
| `dev` | Development | Development and testing environment |
| `staging` | Staging | Pre-production testing |
| `prod` | Production | Live production environment |

### 2.3 Project Codes (Mystira Specific)

| Code | Project | Description |
|------|---------|-------------|
| `mystira` | Mystira App | Main Mystira application platform |

### 2.4 Type Codes (Azure Resource Types)

| Code | Azure Resource Type | Example |
|------|---------------------|---------|
| `app` | App Service (Web App) | Web applications, APIs |
| `api` | API App Service | Backend API services |
| `admin-api` | Admin API App Service | Admin backend services |
| `func` | Function App | Azure Functions |
| `swa` | Static Web App | Blazor PWA, static sites |
| `db` | Cosmos DB / SQL Database | Databases |
| `storage` | Storage Account | Blob, queue, table storage |
| `kv` | Key Vault | Secrets management |
| `ai` | Application Insights | Monitoring and analytics |
| `dns` | DNS Zone | Domain name services |
| `log` | Log Analytics Workspace | Centralized logging |
| `rg` | Resource Group | Logical container for resources |

### 2.5 Region Codes

| Code | Azure Region | Full Name |
|------|-------------|-----------|
| `euw` | West Europe | westeurope |
| `eun` | North Europe | northeurope |
| `wus` | West US | westus |
| `eus` | East US | eastus |
| `san` | South Africa North | southafricanorth |
| `saf` | South Africa West | southafricawest |
| `swe` | Sweden Central | swedencentral |
| `uks` | UK South | uksouth |
| `usw` | US West | westus |
| `glob` | Global | global (for resources without region) |

---

## 3. Mystira Current Resource Mapping

### 3.1 Development Environment

| Resource Type | Current Name | New Standard Name | Notes |
|--------------|--------------|-------------------|-------|
| Resource Group | (varies) | `mys-dev-mystira-rg-san` | South Africa North |
| PWA (Static Web App) | `dev-san-swa-mystira-app` | `mys-dev-mystira-swa-san` | Blazor PWA |
| API (App Service) | `dev-san-app-mystira-api` | `mys-dev-mystira-api-san` | Main API |
| Admin API (App Service) | `dev-san-app-mystira-admin-api` | `mys-dev-mystira-admin-api-san` | Admin API |

### 3.2 Staging Environment

| Resource Type | Current Name | New Standard Name | Notes |
|--------------|--------------|-------------------|-------|
| Resource Group | (varies) | `mys-staging-mystira-rg-wus` | West US |
| PWA | `mystira-app-staging-pwa` | `mys-staging-mystira-swa-wus` | Staging PWA |
| API (App Service) | `mystira-app-staging-api` | `mys-staging-mystira-api-wus` | Staging API |
| Admin API | `mystira-app-staging-admin-api` | `mys-staging-mystira-admin-api-wus` | Staging Admin API |

### 3.3 Production Environment

| Resource Type | Current Name | New Standard Name | Notes |
|--------------|--------------|-------------------|-------|
| Resource Group | (varies) | `mys-prod-mystira-rg-wus` | West US |
| PWA (Static Web App) | `blue-water-0eab7991e` | `mys-prod-mystira-swa-wus` | Production PWA |
| API (App Service) | `prod-wus-app-mystira-api` | `mys-prod-mystira-api-wus` | Production API |
| Admin API | `prod-wus-app-mystira-admin-api` | `mys-prod-mystira-admin-api-wus` | Production Admin API |

---

## 4. PWA Configuration Mapping

The Blazor PWA uses environment-specific configuration files that map to Azure environments:

| Environment | Config File | API Endpoint | Azure Resource | Domain |
|-------------|-------------|--------------|----------------|--------|
| **Local Dev** | `appsettings.Local.json` | `http://localhost:5260/` | (Local) | localhost |
| **Dev** | `appsettings.Development.json` | `https://dev-san-app-mystira-api.azurewebsites.net/` | `dev-san-app-mystira-api` → `mys-dev-mystira-api-san` | dev.mystira.app |
| **Staging** | `appsettings.Staging.json` | `https://mystira-app-staging-api.azurewebsites.net/` | `mystira-app-staging-api` → `mys-staging-mystira-api-wus` | staging.mystira.app |
| **Production** | `appsettings.Production.json` | `https://prod-wus-app-mystira-api.azurewebsites.net/` | `prod-wus-app-mystira-api` → `mys-prod-mystira-api-wus` | mystira.app |

### Configuration File Selection

The Blazor environment is determined by:
1. **Azure Static Web Apps**: Via `Blazor-Environment` header in `staticwebapp.config.json`
2. **Azure App Service**: Via `ASPNETCORE_ENVIRONMENT` app setting
3. **Local Development**: Via environment variable or launch profile

---

## 5. Migration Strategy

### Phase 1: Documentation (Current)
- ✅ Document new naming conventions
- ✅ Map existing resources to new standard
- Document in code and infrastructure

### Phase 2: New Resources
- All NEW resources MUST use new naming convention
- Update IaC templates (Bicep, Terraform) to use new patterns
- Update CI/CD pipelines to reference new names

### Phase 3: Gradual Migration (Future)
- Migrate resources during natural refresh cycles
- Use Azure resource move capabilities where possible
- Update DNS, connection strings, and references
- Decommission old resources after validation

---

## 6. AI Usage Summary (Copy/Paste for AI Agents)

```
Use [org]-[env]-[project]-[type]-[region] for resources.
Use [org]-[env]-[project]-rg-[region] for RGs.
Valid org codes: nl, pvc, tws, mys.
Valid env codes: dev, staging, prod.
Valid type/region codes: MUST match tables in spec.
Never invent new org/env/project/type/region codes.
Assume names immutable → use migration, not rename.
All IaC must derive names from org/env/project/type/region.
```

---

## 7. Bicep Naming Module (`naming.bicep`)

```bicep
@allowed(['nl','pvc','tws','mys'])
param org string
@allowed(['dev','staging','prod'])
param env string
param project string
@allowed(['euw','eun','wus','eus','san','saf','swe','uks','usw','glob'])
param region string

var base = '${org}-${env}-${project}'

output rgName string = '${base}-rg-${region}'
output name_app string = '${base}-app-${region}'
output name_api string = '${base}-api-${region}'
output name_admin_api string = '${base}-admin-api-${region}'
output name_func string = '${base}-func-${region}'
output name_swa string = '${base}-swa-${region}'
output name_db string = '${base}-db-${region}'
output name_storage string = '${base}-storage-${region}'
output name_kv string = '${base}-kv-${region}'
output name_ai string = '${base}-ai-${region}'
output name_dns string = '${base}-dns-${region}'
output name_log string = '${base}-log-${region}'
```

### Usage Example

```bicep
module naming 'naming.bicep' = {
  name: 'naming'
  params: {
    org: 'mys'
    env: 'dev'
    project: 'mystira'
    region: 'san'
  }
}

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: naming.outputs.rgName  // mys-dev-mystira-rg-san
  location: 'southafricanorth'
}

resource api 'Microsoft.Web/sites@2022-03-01' = {
  name: naming.outputs.name_api  // mys-dev-mystira-api-san
  location: 'southafricanorth'
  kind: 'app'
  // ... rest of configuration
}
```

---

## 8. CLI Naming Validator (`nl-az-name`)

A Python CLI to validate names and lint Bicep.

### Installation
```bash
pip install nl-az-name
```

### Usage
```bash
# Validate a resource name
nl-az-name validate mys-prod-mystira-api-wus

# Lint Bicep files for naming compliance
nl-az-name lint-bicep infrastructure/

# Generate names from parameters
nl-az-name generate --org mys --env dev --project mystira --type api --region san
```

---

## 9. Azure CLI Resource Discovery

Standardised commands for auditing your Azure estate and enforcing naming.

### List all resource groups
```bash
az group list --query "[].{name:name, location:location}" -o table
```

### List all resources
```bash
az resource list --query "[].{name:name, type:type, rg:resourceGroup, location:location}" -o table
```

### List resources inside a resource group
```bash
az resource list -g <rg> --query "[].{name:name, type:type, location:location}" -o table
```

### Filter by resource type

**App Services:**
```bash
az resource list --resource-type "Microsoft.Web/sites" -o table
```

**Function Apps:**
```bash
az resource list --resource-type "Microsoft.Web/sites" --query "[?kind=='functionapp']" -o table
```

**Storage Accounts:**
```bash
az storage account list --query "[].{name:name, rg:resourceGroup, location:location}" -o table
```

### Group resources by type
```bash
az resource list --query "sort_by([].{name:name, type:type, rg:resourceGroup}, &type)" -o table
```

### Find non-compliant resource names
```bash
# List all resources that don't match the naming pattern
az resource list --query "[?!starts_with(name, 'mys-') && !starts_with(name, 'nl-') && !starts_with(name, 'pvc-') && !starts_with(name, 'tws-')]" -o table
```

---

## 10. GitHub Actions & CI/CD Naming

### Environment Variable Pattern
```yaml
env:
  ORG: mys
  ENV: dev
  PROJECT: mystira
  REGION: san
  AZURE_WEBAPP_NAME: "mys-dev-mystira-api-san"  # Derived from above
```

### Workflow Naming Example
```yaml
name: Deploy Mystira API - Dev

on:
  push:
    branches: [dev]

env:
  AZURE_WEBAPP_NAME: "mys-dev-mystira-api-san"
  
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
```

---

## 11. Future Considerations

### Domain Naming
Consider aligning domain names with resource names:
- `dev.mystira.app` → matches `mys-dev-mystira-*` resources
- `staging.mystira.app` → matches `mys-staging-mystira-*` resources
- `mystira.app` → matches `mys-prod-mystira-*` resources

### Tagging Strategy
Complement naming with Azure tags:
```json
{
  "org": "mys",
  "env": "dev",
  "project": "mystira",
  "cost-center": "engineering",
  "owner": "eben@mystira.app"
}
```

---

## 12. Questions & Clarifications

### Why not use Storage Account numeric suffixes?
Storage accounts have global uniqueness requirements and character limitations. Consider:
```
mysdevmystirastorage01san  // Compliant but allows counter suffix
```

### What about shared resources?
For resources shared across environments, use `shared` as the environment:
```
mys-shared-mystira-kv-glob
```

### How to handle long names that exceed Azure limits?
Use abbreviations defined in the type codes table. If still too long, remove the region code for global resources:
```
mys-prod-mystira-ai  // Instead of mys-prod-mystira-ai-wus
```

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v2.0 | 2025-12-07 | Copilot | Initial standardized naming convention document |

---

**This document is authoritative for all Mystira Azure resource naming. Any deviations must be documented and justified.**
