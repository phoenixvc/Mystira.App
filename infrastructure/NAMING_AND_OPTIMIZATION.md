# Azure Resource Naming Convention & Dev Optimization

This document explains the naming convention and cost optimizations applied to the dev environment infrastructure.

## Naming Convention

All resources follow the pattern: **`[env]-[region]-[type]-[name]-[ext]`**

Where:
- **`env`**: Environment (dev, prod, staging)
- **`region`**: Azure region abbreviation (euw = West Europe, wus = West US)
- **`type`**: Resource type abbreviation
- **`name`**: Application/service name
- **`ext`**: Optional extension for multi-instance resources

### Resource Type Abbreviations

| Type | Abbreviation | Example |
|------|-------------|---------|
| Storage Account | `st` | `deveuwstmystira` (no hyphens, max 24 chars) |
| Cosmos DB | `cosmos` | `dev-euw-cosmos-mystira` |
| App Service | `app` | `dev-euw-app-mystira-api` |
| App Service Plan | `asp` | `dev-euw-asp-mystira-api` |
| Log Analytics | `log` | `dev-euw-log-mystira` |
| Application Insights | `ai` | `dev-euw-ai-mystira` |
| Communication Services | `acs` | `dev-euw-acs-mystira` |
| Email Service | `ecs` | `dev-euw-ecs-mystira` |
| Static Web App | `swa` | `dev-euw-swa-mystira-app` |

### Special Cases

**Storage Accounts**: Due to Azure restrictions (max 24 chars, lowercase only, no hyphens), we use:
- Pattern: `[env][region][type][name]`
- Example: `deveuwstmystira`

## Dev Environment Resource Names

| Resource Type | Current Name (in Azure) | Desired Name (convention) |
|---------------|-------------------------|---------------------------|
| Storage Account | `mystiraappdevstorage` | `deveuwstmystira` |
| Cosmos DB | `mystiraappdevcosmos` | `dev-euw-cosmos-mystira` |
| Main API | `mystira-app-dev-api` | `dev-euw-app-mystira-api` |
| Admin API | `dev-euw-app-mystora-admin-api` | `dev-euw-app-mystira-admin-api` |
| Log Analytics | `dev-euw-log-mystira` | ✅ Already correct |
| App Insights | `dev-euw-ai-mystira` | ✅ Already correct |
| Communication Service | `dev-euw-acs-mystira` | ✅ Already correct |
| Email Service | `dev-euw-ecs-mystira` | ✅ Already correct |
| Static Web App | `dev-euw-swa-mystira-app` | ✅ Already correct |

**Note**: The GitHub Actions workflows have been updated to use the current Azure resource names to ensure successful deployments. Future infrastructure changes should migrate to the desired naming convention.

## Cost Optimization for Dev Environment

### Changes Made

1. **App Services**: Changed from **B1 ($13.14/mo)** to **F1 (Free)**
   - Saves: ~$26/month for 2 App Services
   - Trade-offs:
     - ⚠️ 60 CPU minutes/day limit
     - ⚠️ No AlwaysOn (app sleeps after inactivity)
     - ⚠️ 1GB RAM limit
     - ⚠️ 1GB storage limit
   - Mitigation: Can upgrade to B1 if limits are hit

2. **Cosmos DB**: Already using **Serverless** (optimal for dev)
   - Pay-per-request pricing: $0.25/million RUs
   - Expected cost: $1-5/month for dev workload
   - Auto-scales to zero when not in use

3. **Storage**: Already using **Standard_LRS** (optimal for dev)
   - Locally Redundant Storage (3 copies in same datacenter)
   - Cheapest replication option
   - Expected cost: $1-3/month for dev workload

4. **Log Analytics**: Already has **1GB daily cap**
   - First 5GB/month free
   - Expected to stay within free tier

### Cost Comparison

| Resource | Previous (B1) | Optimized (F1) | Savings |
|----------|--------------|----------------|---------|
| Main API App Service | $13.14/mo | **$0/mo** | $13.14 |
| Admin API App Service | $13.14/mo | **$0/mo** | $13.14 |
| Cosmos DB Serverless | $5-20/mo | $1-5/mo | $4-15 |
| Storage Account | $1-5/mo | $1-3/mo | $0-2 |
| Log Analytics + AI | $5-10/mo | $0-5/mo | $5 |
| **Total** | **$40-70/mo** | **$2-15/mo** | **$38-55** |

### F1 (Free) Tier Limitations

The Free tier is suitable for:
- ✅ Development and testing
- ✅ Low-traffic applications
- ✅ Proof-of-concepts
- ✅ Learning and experimentation

**NOT suitable for:**
- ❌ Production workloads
- ❌ High-traffic applications
- ❌ Apps requiring 24/7 availability
- ❌ Apps with CPU-intensive operations

### When to Upgrade

Upgrade to B1 or higher if you experience:
- Apps sleeping frequently (AlwaysOn needed)
- CPU quota exceeded (60 minutes/day)
- Performance issues
- Need for deployment slots
- Production-ready requirements

### Upgrade Path

To upgrade App Services from F1 to B1:

1. **Via Bicep**: Change SKU parameter in `infrastructure/dev/main.bicep`:
   ```bicep
   sku: 'B1'  // Change from 'F1'
   ```

2. **Via Azure Portal**:
   - Navigate to App Service
   - Settings → Scale up (App Service plan)
   - Select B1 Basic
   - Click Apply

3. **Via Azure CLI**:
   ```bash
   az appservice plan update \
     --name dev-euw-asp-mystira-api \
     --resource-group dev-euw-rg-mystira \
     --sku B1
   ```

## Production Environment Recommendations

For production, use:

| Resource | Recommended SKU | Reasoning |
|----------|----------------|-----------|
| App Services | **P1v3 or higher** | Production performance, AlwaysOn, deployment slots |
| Cosmos DB | **Provisioned throughput** | Predictable performance and costs |
| Storage | **Standard_GRS or Premium** | Geo-redundancy, higher availability |
| Log Analytics | **No daily cap** | Full logging capability |
| App Service Plan | **Premium tier** | Auto-scaling, VNet integration, private endpoints |

## Migration Notes

### Updating Existing Resources

If resources already exist in Azure with old names, you have two options:

1. **Create New Resources** (Recommended for production)
   - Deploy with new names
   - Migrate data
   - Update DNS/connection strings
   - Delete old resources

2. **Keep Existing Resources** (Easier for dev)
   - Update Bicep to use existing names
   - No data migration needed
   - Less downtime

### Updating GitHub Secrets

After renaming App Services, update publish profile secrets:
- `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` - for `mystira-app-dev-api` (current name)
- `AZURE_WEBAPP_PUBLISH_PROFILE_DEV_ADMIN` - for `dev-euw-app-mystora-admin-api` (current name)

Download new publish profiles:
```bash
# Main API (using current Azure resource name)
az webapp deployment list-publishing-profiles \
  --name mystira-app-dev-api \
  --resource-group dev-euw-rg-mystira \
  --xml

# Admin API (using current Azure resource name)
az webapp deployment list-publishing-profiles \
  --name dev-euw-app-mystora-admin-api \
  --resource-group dev-euw-rg-mystira \
  --xml
```

**Note**: These commands use the current actual Azure resource names. If you need to use the future desired names (`dev-euw-app-mystira-api` and `dev-euw-app-mystira-admin-api`), first rename or recreate the Azure resources.

## Benefits of New Naming Convention

1. **Consistency**: All resources follow the same pattern
2. **Clarity**: Easy to identify environment and region
3. **Organization**: Resources are sorted logically in Azure Portal
4. **Automation**: Easier to script and manage resources
5. **Best Practices**: Follows Microsoft Azure naming recommendations

## References

- [Azure Naming Conventions](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming)
- [Azure Resource Abbreviations](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations)
- [App Service Pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)
- [Cosmos DB Pricing](https://azure.microsoft.com/en-us/pricing/details/cosmos-db/)
