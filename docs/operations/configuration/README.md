# Configuration Documentation

This directory contains configuration examples and documentation for the Mystira application.

## Files

### appsettings.example.json

Complete example configuration file showing all available settings:

#### Cache Options (`Caching` section)
- **Enabled**: Enable/disable distributed caching (Redis)
- **ConnectionString**: Redis connection string (from Azure Key Vault)
- **InstanceName**: Redis instance name prefix
- **KeyPrefix**: Cache key prefix for all cached items
- **DefaultSlidingExpirationMinutes**: Default sliding expiration (30 minutes)
- **DefaultAbsoluteExpirationMinutes**: Default absolute expiration (60 minutes)
- **EnableWriteThrough**: Write to cache on entity updates
- **EnableInvalidationOnChange**: Invalidate cache on entity changes

**Example Usage:**
```json
{
  "Caching": {
    "Enabled": true,
    "ConnectionString": "[from Key Vault: kv-mystira-prod/Redis--ConnectionString]",
    "InstanceName": "Mystira:",
    "KeyPrefix": "mystira:",
    "DefaultSlidingExpirationMinutes": 30,
    "DefaultAbsoluteExpirationMinutes": 60,
    "EnableWriteThrough": true,
    "EnableInvalidationOnChange": true
  }
}
```

#### Migration Options (`Migration` or `PolyglotPersistence` section)

Used for gradual database migration from Cosmos DB to PostgreSQL (per ADR-0013/0014).

- **Phase**: Migration phase
  - `CosmosOnly`: Read/write only to Cosmos DB
  - `DualWriteCosmosRead`: Write to both, read from Cosmos
  - `DualWritePostgresRead`: Write to both, read from Postgres
  - `PostgresOnly`: Read/write only to PostgreSQL
- **DualWriteTimeoutMs**: Timeout for secondary writes (5000ms default)
- **EnableCompensation**: Enable compensation logic on dual-write failures
- **EnableConsistencyValidation**: Validate data consistency between backends

**Example Usage:**
```json
{
  "Migration": {
    "Phase": "CosmosOnly",
    "DualWriteTimeoutMs": 5000,
    "EnableCompensation": false,
    "EnableConsistencyValidation": false
  }
}
```

**Note**: During migration, the `Phase` should be updated through these stages:
1. `CosmosOnly` (current state)
2. `DualWriteCosmosRead` (write to both, read from Cosmos)
3. `DualWritePostgresRead` (write to both, read from Postgres)
4. `PostgresOnly` (final state)

## Azure Key Vault Integration

Most sensitive configuration values are stored in Azure Key Vault and loaded at application startup using Managed Identity.

### Key Vault Secret Names

| Setting | Key Vault Secret Name |
|---------|----------------------|
| Cosmos DB Connection | `CosmosDb--ConnectionString` |
| Redis Connection | `Redis--ConnectionString` |
| JWT RSA Public Key | `JwtSettings--RsaPublicKey` |
| JWT RSA Private Key | `JwtSettings--RsaPrivateKey` |
| Blob Storage Connection | `AzureBlobStorage--ConnectionString` |
| Azure Communication Services | `AzureCommunicationServices--ConnectionString` |
| Discord Bot Token | `Discord--BotToken` |
| Chain Service API Key | `ChainService--ApiKey` |

### GitHub Secrets for CI/CD

The following secrets must be configured in GitHub repository secrets for deployment pipelines:

| Secret Name | Purpose | Used By |
|-------------|---------|---------|
| `AZURE_CREDENTIALS` | Azure service principal credentials | All deployment workflows |
| `AZURE_WEBAPP_PUBLISH_PROFILE_DEV` | App Service publish profile for dev | Dev API deployment |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` | App Service publish profile for staging | Staging API deployment |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD` | App Service publish profile for production (blue slot) | Production API deployment |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_STAGING` | App Service publish profile for production staging slot (green) | Production API deployment with blue/green |
| `COSMOS_CONNECTION_STRING_DEV` | Cosmos DB connection for dev | Dev deployment |
| `COSMOS_CONNECTION_STRING_STAGING` | Cosmos DB connection for staging | Staging deployment |
| `COSMOS_CONNECTION_STRING_PROD` | Cosmos DB connection for production | Production deployment |

#### AZURE_WEBAPP_PUBLISH_PROFILE_PROD_STAGING

This secret contains the Azure App Service publish profile for the **production staging slot** (also known as the "green" slot in blue/green deployment).

**Purpose**: Used in blue/green deployments to deploy new versions to the staging slot first, allowing testing before swapping to production.

**How to Obtain**:
```bash
# Get publish profile for staging slot
az webapp deployment list-publishing-profiles \
  --name mys-prod-mystira-api-san \
  --resource-group rg-mystira-prod \
  --slot staging \
  --xml
```

**Deployment Flow**:
1. Deploy to staging slot using `AZURE_WEBAPP_PUBLISH_PROFILE_PROD_STAGING`
2. Run smoke tests against staging slot
3. Swap staging slot with production slot if tests pass
4. Rollback by swapping back if issues detected

**Security**: Publish profiles should be rotated regularly (every 90 days) and immediately after any security incident.

## Environment-Specific Configuration

### Development (Local)
- Use `appsettings.Development.json`
- In-memory database or local Cosmos DB emulator
- Local Redis or in-memory cache
- Application Insights optional

### Staging
- Use `appsettings.Staging.json`
- Azure Cosmos DB (staging account)
- Azure Redis Cache
- Application Insights enabled
- SendGrid sandbox mode

### Production
- Use `appsettings.Production.json`
- Azure Cosmos DB (production account with backup)
- Azure Redis Cache with persistence
- Application Insights with sampling
- SendGrid production mode
- All secrets from Key Vault

## Configuration Best Practices

1. **Never commit secrets**: Use Key Vault or GitHub Secrets
2. **Use User Secrets for local development**: `dotnet user-secrets set "Key" "Value"`
3. **Validate configuration on startup**: Use `IOptions<T>` with validation
4. **Document all settings**: Keep this README up to date
5. **Rotate secrets regularly**: Every 90 days minimum
6. **Use Managed Identity**: Avoid connection strings where possible
7. **Enable audit logging**: Track configuration changes in Key Vault

## Related Documentation

- [Secret Rotation](../secret-rotation.md)
- [Azure Key Vault Setup](../rbac-and-managed-identity.md)
- [Deployment Runbooks](../runbooks/README.md)
- [Monitoring Implementation Plan](../monitoring-implementation-plan.md)
