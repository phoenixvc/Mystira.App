# Azure Resource Naming Conventions

This document defines the standard naming conventions for all Azure resources across our organization.

## Naming Pattern

All Azure resources follow this naming pattern:

```
[org]-[env]-[project]-[type]-[region]
```

### Components

| Component | Description | Example |
|-----------|-------------|---------|
| `org` | Organisation code | `mys`, `nl`, `pvc`, `tws` |
| `env` | Environment | `dev`, `staging`, `prod` |
| `project` | Project name | `mystira`, `website`, `api` |
| `type` | Resource type abbreviation | `rg`, `st`, `cosmos`, `api` |
| `region` | Azure region code | `euw`, `eun`, `wus` |

## Organisation Codes

| Code | Organisation | Owner |
|------|--------------|-------|
| `mys` | Mystira | Eben |
| `nl` | NeuralLiquid | Jurie |
| `pvc` | Phoenix VC | Eben |
| `tws` | Twines & Straps | Martyn |

## Environment Codes

| Code | Environment | Description |
|------|-------------|-------------|
| `dev` | Development | Development and testing |
| `staging` | Staging | Pre-production validation |
| `prod` | Production | Live production environment |

## Region Codes

| Code | Azure Region | Location |
|------|--------------|----------|
| `euw` | westeurope | West Europe (Netherlands) |
| `eun` | northeurope | North Europe (Ireland) |
| `wus` | westus | West US |
| `eus` | eastus | East US |
| `san` | southafricanorth | South Africa North |
| `saf` | southafricanorth | South Africa (alias) |
| `swe` | swedencentral | Sweden Central |
| `uks` | uksouth | UK South |
| `usw` | westus2 | West US 2 |
| `glob` | global | Global/non-regional resources |

## Resource Type Abbreviations

| Abbreviation | Resource Type |
|--------------|---------------|
| `rg` | Resource Group |
| `st` | Storage Account |
| `cosmos` | Cosmos DB |
| `api` | App Service (API) |
| `web` | App Service (Web) |
| `func` | Function App |
| `plan` | App Service Plan |
| `kv` | Key Vault |
| `log` | Log Analytics Workspace |
| `appins` | Application Insights |
| `bot` | Azure Bot |
| `comm` | Communication Services |
| `sql` | SQL Database |
| `redis` | Redis Cache |
| `sb` | Service Bus |
| `eh` | Event Hub |
| `apim` | API Management |
| `acr` | Container Registry |
| `aks` | Kubernetes Service |

## Examples

### Resource Groups

```
mys-dev-mystira-rg-euw      # Mystira development in West Europe
mys-staging-mystira-rg-euw  # Mystira staging in West Europe
mys-prod-mystira-rg-euw     # Mystira production in West Europe
nl-prod-api-rg-euw          # NeuralLiquid API production
tws-prod-website-rg-san     # Twines & Straps website in South Africa
```

### Storage Accounts

Storage accounts have special rules (no hyphens, max 24 chars):

```
mysdevmystirasteruw         # Mystira dev storage
mysprodmystirasteruw        # Mystira prod storage
twsprodwebsitestsan         # Twines & Straps prod storage
```

### Other Resources

```
mys-dev-mystira-cosmos-euw      # Cosmos DB
mys-dev-mystira-api-euw         # App Service (API)
mys-dev-mystira-log-euw         # Log Analytics
mys-dev-mystira-appins-euw      # Application Insights
mys-dev-mystira-kv-euw          # Key Vault
```

## Special Cases

### Storage Account Names

Storage accounts cannot contain hyphens and have a 24-character limit. Use this pattern:

```
[org][env][project]st[region]
```

Remove all hyphens and ensure the total length is under 24 characters.

### Global Resources

For resources that are not region-specific, use `glob` as the region:

```
mys-prod-mystira-dns-glob       # DNS Zone (global)
```

### Shared Resources

For resources shared across projects within an organisation:

```
mys-prod-shared-log-euw         # Shared Log Analytics workspace
```

## Bicep Implementation

In our Bicep templates, the naming is implemented using these parameters:

```bicep
@allowed(['mys', 'nl', 'pvc', 'tws'])
param org string

@allowed(['dev', 'staging', 'prod'])
param environment string

param project string

@allowed(['euw', 'eun', 'wus', 'eus', 'san', 'saf', 'swe', 'uks', 'usw', 'glob'])
param region string

var namePrefix = '${org}-${environment}-${project}'

var names = {
  resourceGroup: '${namePrefix}-rg-${region}'
  logAnalytics: '${namePrefix}-log-${region}'
  appInsights: '${namePrefix}-appins-${region}'
  cosmosDb: '${namePrefix}-cosmos-${region}'
  apiApp: '${namePrefix}-api-${region}'
  storageAccount: replace(toLower('${org}${environment}${project}st${region}'), '-', '')
}
```

## Validation

Before deploying, ensure:

1. Names follow the pattern exactly
2. Organisation code is valid
3. Environment code matches the target environment
4. Region code matches the actual Azure region
5. Storage account names are under 24 characters and contain no hyphens
