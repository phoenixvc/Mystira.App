# ADR-0012: Infrastructure as Code (Mystira.Infra)

**Status**: 💭 Proposed

**Date**: 2025-12-10

**Deciders**: Development Team

**Tags**: infrastructure, iac, azure, networking, front-door, devops

**Supersedes**: None (new capability)

---

## Approvals

| Role | Name | Date | Status |
|------|------|------|--------|
| Tech Lead | | | ⏳ Pending |
| DevOps | | | ⏳ Pending |
| Security | | | ⏳ Pending |

---

## Context

As the Mystira ecosystem grows with multiple services (Mystira.App, Mystira.Chain, Mystira.StoryGenerator), infrastructure management becomes increasingly complex:

### Current State

| Service | Current Infrastructure | Issues |
|---------|----------------------|--------|
| Mystira.App API | Azure App Service | Manual deployment, no IaC |
| Mystira.App Admin API | Azure App Service | Separate config, drift risk |
| Mystira.App PWA | Azure Static Web Apps | Manual setup |
| Cosmos DB | Azure Cosmos DB | Hand-configured |
| Mystira.Chain | Not deployed | Needs containerization |
| Mystira.StoryGenerator | Azure App Service | Inconsistent with App |

### Problems Identified

1. **No Infrastructure as Code**
   - Resources created manually via Azure Portal
   - Configuration drift between environments
   - No version control for infrastructure changes
   - Difficult to reproduce environments

2. **No Unified Networking**
   - Services communicate over public internet
   - No VNet isolation
   - No private endpoints for databases
   - Security concerns for service-to-service communication

3. **No Global Load Balancing**
   - No SSL termination at edge
   - No DDoS protection
   - No WAF (Web Application Firewall)
   - No traffic management/routing

4. **Environment Inconsistency**
   - Dev, staging, and prod configurations differ
   - Manual environment promotion
   - Hard to test infrastructure changes

5. **Multi-Service Complexity**
   - Different tech stacks (.NET, Python)
   - Different deployment targets (App Service, Container Apps)
   - Need unified approach

---

## Decision Drivers

1. **Azure Native**: Prefer Azure-native tooling for best integration
2. **Repeatability**: Infrastructure must be reproducible across environments
3. **Security**: Network isolation, private endpoints, WAF
4. **Cost Efficiency**: Right-size resources, avoid over-provisioning
5. **Team Familiarity**: Balance ideal tooling with learning curve
6. **Multi-Service Support**: Handle .NET, Python, and future stacks

---

## Considered Options

### Option 1: Azure Bicep ⭐ **RECOMMENDED**

**Description**: Use Azure Bicep (ARM template DSL) for all infrastructure definitions.

**Pros**:
- ✅ Azure-native, first-class support
- ✅ Simpler syntax than ARM JSON
- ✅ Excellent VS Code tooling (validation, IntelliSense)
- ✅ Built-in what-if deployments
- ✅ No state file to manage (unlike Terraform)
- ✅ Direct Azure CLI integration
- ✅ Modules for reusability

**Cons**:
- ⚠️ Azure-only (no multi-cloud)
- ⚠️ Smaller community than Terraform
- ⚠️ Less mature ecosystem

### Option 2: Terraform

**Description**: Use HashiCorp Terraform with Azure provider.

**Pros**:
- ✅ Multi-cloud support
- ✅ Large community and ecosystem
- ✅ Mature tooling
- ✅ HCL is well-documented

**Cons**:
- ❌ State file management complexity
- ❌ Extra abstraction layer for Azure
- ❌ Provider version compatibility issues
- ❌ Learning curve for team

### Option 3: Pulumi

**Description**: Use Pulumi with TypeScript or Python for infrastructure.

**Pros**:
- ✅ Real programming languages
- ✅ Testable infrastructure code
- ✅ Multi-cloud support

**Cons**:
- ❌ State management (cloud or self-hosted)
- ❌ Steeper learning curve
- ❌ Less Azure-specific documentation
- ❌ Overkill for current needs

### Option 4: ARM Templates (Raw JSON)

**Description**: Use raw Azure Resource Manager JSON templates.

**Pros**:
- ✅ Most direct Azure representation
- ✅ No additional tooling

**Cons**:
- ❌ Verbose JSON syntax
- ❌ Poor developer experience
- ❌ Hard to maintain
- ❌ No modularity

---

## Decision

We will adopt **Option 1: Azure Bicep** with a structured repository (`Mystira.Infra`) containing:

### Repository Structure

**GitHub Repository Settings for `Mystira.Infra`:**
| Field | Value |
|-------|-------|
| **Name** | `Mystira.Infra` |
| **Description** | Infrastructure as Code for Mystira platform - Azure Bicep, networking, and deployment |
| **Topics/Labels** | `infrastructure`, `azure`, `bicep`, `iac`, `mystira`, `devops` |
| **Visibility** | Private |
| **License** | Proprietary |

```
Mystira.Infra/
├── modules/                      # Reusable Bicep modules
│   ├── networking/
│   │   ├── vnet.bicep           # Virtual Network
│   │   ├── private-endpoints.bicep
│   │   └── nsg.bicep            # Network Security Groups
│   ├── compute/
│   │   ├── app-service.bicep    # App Service (for .NET)
│   │   ├── container-app.bicep  # Container Apps (for Python)
│   │   └── static-web-app.bicep # Static Web Apps (PWA)
│   ├── data/
│   │   ├── cosmos-db.bicep      # Cosmos DB
│   │   └── key-vault.bicep      # Key Vault
│   ├── security/
│   │   ├── front-door.bicep     # Azure Front Door + WAF
│   │   └── managed-identity.bicep
│   └── monitoring/
│       ├── app-insights.bicep   # Application Insights
│       └── log-analytics.bicep  # Log Analytics Workspace
├── environments/
│   ├── dev/
│   │   ├── main.bicep           # Dev environment orchestration
│   │   └── parameters.json      # Dev-specific parameters
│   ├── staging/
│   │   ├── main.bicep
│   │   └── parameters.json
│   └── prod/
│       ├── main.bicep
│       └── parameters.json
├── scripts/
│   ├── deploy.sh                # Deployment script
│   ├── deploy.ps1               # Windows deployment
│   └── validate.sh              # Pre-deployment validation
├── .github/
│   └── workflows/
│       ├── validate.yml         # PR validation
│       └── deploy.yml           # Deployment workflow
├── bicepconfig.json             # Bicep configuration
└── README.md
```

### Network Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Azure Front Door                                │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  WAF Policy │ SSL Termination │ Global Load Balancing │ Caching     │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│           │                    │                    │                        │
│    mystira.app          admin.mystira.app    api.mystira.app                │
└───────────┼────────────────────┼────────────────────┼────────────────────────┘
            │                    │                    │
            ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Azure Virtual Network                                │
│                         10.0.0.0/16                                          │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │  App Subnet (10.0.1.0/24)                                               │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │ │
│  │  │ App Service │  │ App Service │  │ Container   │                     │ │
│  │  │ (API)       │  │ (Admin API) │  │ App (Chain) │                     │ │
│  │  │ .NET 9      │  │ .NET 9      │  │ Python      │                     │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                     │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │  Private Endpoints Subnet (10.0.2.0/24)                                 │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │ │
│  │  │ Cosmos DB   │  │ Key Vault   │  │ Storage     │                     │ │
│  │  │ (Private)   │  │ (Private)   │  │ (Private)   │                     │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                     │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Azure Front Door Configuration

```bicep
// modules/security/front-door.bicep
@description('Azure Front Door with WAF for Mystira')
param location string = 'global'
param environmentName string

resource frontDoor 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: 'fd-mystira-${environmentName}'
  location: location
  sku: {
    name: 'Premium_AzureFrontDoor'  // Required for Private Link + WAF
  }
  properties: {}
}

resource wafPolicy 'Microsoft.Network/FrontDoorWebApplicationFirewallPolicies@2022-05-01' = {
  name: 'waf-mystira-${environmentName}'
  location: location
  sku: {
    name: 'Premium_AzureFrontDoor'
  }
  properties: {
    policySettings: {
      enabledState: 'Enabled'
      mode: 'Prevention'
      requestBodyCheck: 'Enabled'
    }
    managedRules: {
      managedRuleSets: [
        {
          ruleSetType: 'Microsoft_DefaultRuleSet'
          ruleSetVersion: '2.1'
        }
        {
          ruleSetType: 'Microsoft_BotManagerRuleSet'
          ruleSetVersion: '1.0'
        }
      ]
    }
  }
}

// Endpoints for each service
resource apiEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: frontDoor
  name: 'api-mystira-${environmentName}'
  location: location
  properties: {
    enabledState: 'Enabled'
  }
}

resource adminEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: frontDoor
  name: 'admin-mystira-${environmentName}'
  location: location
  properties: {
    enabledState: 'Enabled'
  }
}

resource chainEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: frontDoor
  name: 'chain-mystira-${environmentName}'
  location: location
  properties: {
    enabledState: 'Enabled'
  }
}

output frontDoorId string = frontDoor.id
output apiHostname string = apiEndpoint.properties.hostName
output adminHostname string = adminEndpoint.properties.hostName
output chainHostname string = chainEndpoint.properties.hostName
```

### Virtual Network Module

```bicep
// modules/networking/vnet.bicep
@description('Virtual Network for Mystira services')
param location string = resourceGroup().location
param environmentName string

var vnetAddressPrefix = '10.0.0.0/16'
var appSubnetPrefix = '10.0.1.0/24'
var privateEndpointSubnetPrefix = '10.0.2.0/24'

resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: 'vnet-mystira-${environmentName}'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [vnetAddressPrefix]
    }
    subnets: [
      {
        name: 'snet-app'
        properties: {
          addressPrefix: appSubnetPrefix
          delegations: [
            {
              name: 'appServiceDelegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'snet-private-endpoints'
        properties: {
          addressPrefix: privateEndpointSubnetPrefix
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

output vnetId string = vnet.id
output appSubnetId string = vnet.properties.subnets[0].id
output privateEndpointSubnetId string = vnet.properties.subnets[1].id
```

### Container App for Mystira.Chain

```bicep
// modules/compute/container-app.bicep
@description('Container App for Python services (Mystira.Chain)')
param location string = resourceGroup().location
param environmentName string
param containerImage string
param apiKey string

resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'cae-mystira-${environmentName}'
  location: location
  properties: {
    zoneRedundant: environmentName == 'prod'
  }
}

resource chainApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-mystira-chain-${environmentName}'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: false  // Internal only - accessed via Front Door
        targetPort: 8000
        transport: 'http'
      }
      secrets: [
        {
          name: 'api-key'
          value: apiKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'mystira-chain'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ENVIRONMENT'
              value: environmentName
            }
            {
              name: 'API_KEY'
              secretRef: 'api-key'
            }
          ]
        }
      ]
      scale: {
        minReplicas: environmentName == 'prod' ? 2 : 1
        maxReplicas: environmentName == 'prod' ? 10 : 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output chainAppUrl string = 'https://${chainApp.properties.configuration.ingress.fqdn}'
```

### Environment Orchestration

```bicep
// environments/prod/main.bicep
targetScope = 'subscription'

param location string = 'westeurope'
param environmentName string = 'prod'

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-mystira-${environmentName}'
  location: location
}

// Networking
module networking '../../modules/networking/vnet.bicep' = {
  scope: rg
  name: 'networking'
  params: {
    location: location
    environmentName: environmentName
  }
}

// Front Door + WAF
module frontDoor '../../modules/security/front-door.bicep' = {
  scope: rg
  name: 'frontDoor'
  params: {
    environmentName: environmentName
  }
}

// Cosmos DB with Private Endpoint
module cosmosDb '../../modules/data/cosmos-db.bicep' = {
  scope: rg
  name: 'cosmosDb'
  params: {
    location: location
    environmentName: environmentName
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
  }
}

// Key Vault
module keyVault '../../modules/data/key-vault.bicep' = {
  scope: rg
  name: 'keyVault'
  params: {
    location: location
    environmentName: environmentName
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
  }
}

// App Service for .NET APIs
module apiAppService '../../modules/compute/app-service.bicep' = {
  scope: rg
  name: 'apiAppService'
  params: {
    location: location
    environmentName: environmentName
    appName: 'api'
    subnetId: networking.outputs.appSubnetId
  }
}

module adminAppService '../../modules/compute/app-service.bicep' = {
  scope: rg
  name: 'adminAppService'
  params: {
    location: location
    environmentName: environmentName
    appName: 'admin-api'
    subnetId: networking.outputs.appSubnetId
  }
}

// Container App for Mystira.Chain
module chainContainerApp '../../modules/compute/container-app.bicep' = {
  scope: rg
  name: 'chainContainerApp'
  params: {
    location: location
    environmentName: environmentName
    containerImage: 'ghcr.io/phoenixvc/mystira-chain:latest'
    apiKey: keyVault.outputs.chainApiKey
  }
}

// Application Insights
module appInsights '../../modules/monitoring/app-insights.bicep' = {
  scope: rg
  name: 'appInsights'
  params: {
    location: location
    environmentName: environmentName
  }
}

// Outputs
output resourceGroupName string = rg.name
output frontDoorHostname string = frontDoor.outputs.apiHostname
output apiUrl string = apiAppService.outputs.appUrl
output adminApiUrl string = adminAppService.outputs.appUrl
output chainUrl string = chainContainerApp.outputs.chainAppUrl
```

### Deployment Script

```bash
#!/bin/bash
# scripts/deploy.sh

set -e

ENVIRONMENT=${1:-dev}
LOCATION=${2:-westeurope}

echo "Deploying Mystira infrastructure to $ENVIRONMENT..."

# Validate first
az deployment sub what-if \
  --location $LOCATION \
  --template-file environments/$ENVIRONMENT/main.bicep \
  --parameters environments/$ENVIRONMENT/parameters.json

read -p "Continue with deployment? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
  az deployment sub create \
    --location $LOCATION \
    --template-file environments/$ENVIRONMENT/main.bicep \
    --parameters environments/$ENVIRONMENT/parameters.json \
    --name "mystira-$ENVIRONMENT-$(date +%Y%m%d%H%M%S)"

  echo "Deployment complete!"
fi
```

### GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy Infrastructure

on:
  push:
    branches: [main]
    paths:
      - 'environments/**'
      - 'modules/**'
  workflow_dispatch:
    inputs:
      environment:
        description: 'Environment to deploy'
        required: true
        default: 'dev'
        type: choice
        options:
          - dev
          - staging
          - prod

permissions:
  id-token: write
  contents: read

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Azure Login
        uses: azure/login@v1
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Validate Bicep
        run: |
          az bicep build --file environments/${{ inputs.environment || 'dev' }}/main.bicep

  deploy:
    needs: validate
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment || 'dev' }}
    steps:
      - uses: actions/checkout@v4

      - name: Azure Login
        uses: azure/login@v1
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy Infrastructure
        run: |
          az deployment sub create \
            --location westeurope \
            --template-file environments/${{ inputs.environment || 'dev' }}/main.bicep \
            --parameters environments/${{ inputs.environment || 'dev' }}/parameters.json
```

---

## Consequences

### Positive Consequences ✅

1. **Reproducible Infrastructure**
   - All resources defined in code
   - Version controlled changes
   - Consistent environments

2. **Security Improvements**
   - VNet isolation for services
   - Private endpoints for data stores
   - WAF protection via Front Door
   - Managed identities (no secrets in code)

3. **Global Performance**
   - Front Door provides edge caching
   - SSL termination at edge
   - Intelligent routing

4. **Cost Visibility**
   - Infrastructure costs tracked per environment
   - Easy to compare configurations
   - Resource right-sizing

5. **Developer Experience**
   - Self-service environment creation
   - Consistent dev/staging/prod
   - Clear deployment process

### Negative Consequences ❌

1. **Initial Setup Time**
   - Learning Bicep syntax
   - Building module library
   - Migration of existing resources

2. **Increased Complexity**
   - More moving parts
   - Network troubleshooting
   - Front Door configuration

3. **Cost Increase**
   - Front Door Premium required for Private Link
   - VNet integration costs
   - Additional Azure resources

### Mitigation Strategies

| Risk | Mitigation |
|------|------------|
| Learning curve | Start with dev environment; iterate |
| Migration complexity | Import existing resources; gradual adoption |
| Cost concerns | Use consumption tiers where possible; monitor spend |
| Network issues | Detailed documentation; runbooks for common problems |

---

## Implementation Plan

### Phase 1: Foundation

1. Create `Mystira.Infra` repository
2. Build core modules (VNet, App Service, Container Apps)
3. Deploy dev environment
4. Migrate existing dev resources

### Phase 2: Security

1. Add Front Door + WAF
2. Configure private endpoints
3. Set up Key Vault integration
4. Implement managed identities

### Phase 3: Production

1. Deploy staging environment
2. Test failover and scaling
3. Deploy production
4. Set up monitoring and alerts

### Phase 4: Automation

1. GitHub Actions for deployment
2. PR validation workflows
3. Cost monitoring
4. Documentation

---

## Related Decisions

- **ADR-0010**: Story Protocol SDK Integration (Mystira.Chain deployment)
- **ADR-0011**: Unified Workspace Repository (Mystira.Infra in workspace)
- **ADR-0005**: Separate API and Admin API (both deployed via Infra)

---

## References

- [Azure Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure Front Door Documentation](https://learn.microsoft.com/azure/frontdoor/)
- [Azure Virtual Network Documentation](https://learn.microsoft.com/azure/virtual-network/)
- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)

---

## Notes

- Bicep chosen over Terraform for Azure-native experience
- Front Door Premium required for Private Link origins
- Start with dev environment to iterate on modules
- Consider Azure Landing Zones for enterprise patterns

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
