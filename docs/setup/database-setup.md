# Database Setup and Seeding

This guide explains how database initialization and master data seeding work in Mystira.App APIs.

## Overview

Both the main API (`Mystira.App.Api`) and Admin API (`Mystira.App.Admin.Api`) automatically initialize the database and seed master data during startup. The behavior differs based on the database provider being used.

## Database Providers

### InMemory Database (Local Development)
- **When Used**: Automatically when no Cosmos DB connection string is configured
- **Behavior**: 
  - Database is created in memory
  - Master data is **automatically seeded** on every startup
  - Data is lost when the application stops
- **Use Case**: Local development and testing

### Azure Cosmos DB (Cloud)
- **When Used**: When `ConnectionStrings:CosmosDb` is configured
- **Behavior**:
  - Database and containers are created if they don't exist
  - Master data seeding is **opt-in** via configuration
  - Data persists across application restarts
- **Use Case**: Development, Staging, and Production environments

## Master Data Seeding

Master data includes reference data needed for the application to function:
- **Compass Axes** (from `CoreAxes.json`)
- **Archetypes** (from `Archetypes.json`)
- **Echo Types** (from `EchoTypes.json`)
- **Fantasy Themes** (from `FantasyThemes.json`)
- **Age Groups** (from `AgeGroups.json`)

### Configuration

#### Enable Seeding for Cosmos DB

To enable master data seeding in Cosmos DB environments, add this to your `appsettings.json` or Azure App Service configuration:

```json
{
  "SeedMasterDataOnStartup": true
}
```

**Or in Azure App Service:**

```bash
az webapp config appsettings set --name <app-name> --resource-group <rg-name> \
  --settings SeedMasterDataOnStartup=true
```

#### Default Behavior

| Environment | Database Provider | Seeding Enabled |
|------------|------------------|----------------|
| Local Dev (no connection string) | InMemory | ✅ Yes (automatic) |
| Local Dev (with Cosmos connection) | Cosmos DB | ❌ No (unless configured) |
| Cloud (Dev/Staging/Prod) | Cosmos DB | ❌ No (unless configured) |

### Seeding Logic

The seeding service (`MasterDataSeederService`) is **idempotent**:
- Checks if data already exists before inserting
- Uses deterministic IDs to prevent duplicates
- Safe to run multiple times
- Skips seeding if master data already exists

## Database Initialization Behavior

### Main API (`Mystira.App.Api`)

```csharp
// Database initialization happens during startup
await context.Database.EnsureCreatedAsync();

// Seeding runs if:
// 1. Using InMemory database (local dev), OR
// 2. SeedMasterDataOnStartup configuration is true
if (seedOnStartup || isInMemory)
{
    await seeder.SeedAllAsync();
}
```

**Error Handling:**
- Database connection failures → **FAIL FAST** (application exits)
- Seeding failures → **LOG ERROR** (application continues)

### Admin API (`Mystira.App.Admin.Api`)

Same behavior as Main API - consistent seeding logic across both APIs.

## Troubleshooting

### Database Connection Failures

If you see this error:
```
Failed to initialize database during startup. Ensure Azure Cosmos DB database 'MystiraAppDb' exists...
```

**Possible causes:**
1. Cosmos DB connection string is incorrect
2. Database 'MystiraAppDb' doesn't exist
3. Application doesn't have permissions to create containers
4. Network connectivity issues to Cosmos DB

**Solutions:**
1. Verify `ConnectionStrings:CosmosDb` is correct
2. Create the database manually in Azure Portal or use infrastructure scripts
3. Ensure Managed Identity or service principal has appropriate permissions
4. Check firewall rules in Cosmos DB allow your IP/service

### Seeding Failures

If seeding fails, you'll see:
```
Master data seeding failed. The application will continue to start...
```

**Common causes:**
1. JSON files not found (check `src/Mystira.App.Domain/Data/` directory)
2. Cosmos DB query issues in some configurations
3. Permission issues writing to containers

**Solutions:**
1. Ensure all JSON files exist in the Domain project
2. Set `SeedMasterDataOnStartup=false` to skip seeding temporarily
3. Use InMemory database for local development
4. Manually seed data using the Admin API or DevHub CLI

### No Master Data After Startup

If the application starts but master data is missing:

**For InMemory:**
- Check logs for seeding errors
- Verify JSON files exist

**For Cosmos DB:**
1. Enable seeding: `"SeedMasterDataOnStartup": true`
2. Restart the application
3. Check logs for seeding status

## Required Cosmos DB Containers

When using Cosmos DB, these containers will be created automatically:

| Container | Partition Key |
|-----------|--------------|
| CompassAxes | `/Id` |
| ArchetypeDefinitions | `/id` |
| EchoTypeDefinitions | `/Id` |
| FantasyThemeDefinitions | `/Id` |
| AgeGroupDefinitions | `/Id` |
| BadgeConfigurations | `/Id` |
| CharacterMaps | `/Id` |
| ContentBundles | `/Id` |
| Scenarios | `/Id` |
| MediaMetadataFiles | `/Id` |
| CharacterMediaMetadataFiles | `/Id` |
| CharacterMapFiles | `/Id` |
| AvatarConfigurationFiles | `/Id` |
| UserProfiles | `/Id` |
| Accounts | `/id` |
| PendingSignups | `/email` |
| GameSessions | `/AccountId` |
| MediaAssets | `/MediaType` |
| CompassTrackings | `/Axis` |

## Local Development Setup

### Option 1: InMemory Database (Recommended for quick start)

No configuration needed! Just run:

```bash
dotnet run --project src/Mystira.App.Api
```

Master data will be automatically seeded.

### Option 2: appsettings.local.json (Recommended for persistent local config)

For a persistent local configuration that won't be committed to git:

1. Copy the template file:
```bash
cp src/Mystira.App.Api/appsettings.local.json.template src/Mystira.App.Api/appsettings.local.json
```

2. Edit `appsettings.local.json` with your settings:
```json
{
  "ConnectionStrings": {
    "CosmosDb": "",  // Leave empty for InMemory, or add Cosmos connection
    "AzureStorage": "UseDevelopmentStorage=true"
  },
  "SeedMasterDataOnStartup": false  // true for Cosmos DB
}
```

3. Run the application:
```bash
dotnet run --project src/Mystira.App.Api
```

**Note:** `appsettings.local.json` is gitignored to prevent accidentally committing secrets.

### Option 3: Cosmos DB Emulator

1. Install [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/local-emulator)
2. Start the emulator
3. Add to `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
  "SeedMasterDataOnStartup": true
}
```

4. Run the application - database will be created and seeded

## Production Deployment

### Azure App Service Configuration

Set these app settings in Azure Portal or via Azure CLI:

```bash
# Database connection (from Key Vault)
az webapp config appsettings set --name mystira-api --resource-group mystira-rg \
  --settings ConnectionStrings__CosmosDb="@Microsoft.KeyVault(SecretUri=https://...)"

# Optional: Enable seeding (only needed if master data isn't seeded yet)
az webapp config appsettings set --name mystira-api --resource-group mystira-rg \
  --settings SeedMasterDataOnStartup=true
```

**Best Practice:**
- Seed master data once during initial deployment
- Disable seeding for subsequent deployments to avoid unnecessary queries
- Keep seeding enabled only if you need to add new master data

## CI/CD Pipeline Considerations

Database initialization happens automatically during application startup, so no separate database setup step is needed in CI/CD pipelines.

However, you can add a health check after deployment to verify database connectivity:

```yaml
- name: Health Check
  run: |
    curl -f https://mystira-api.azurewebsites.net/health || exit 1
```

## See Also

- [Secrets Setup Guide](./secrets-management.md) - How to configure connection strings securely
- [Troubleshooting Guide](../troubleshooting.md) - Common issues and solutions
- [Architecture Documentation](../architecture/README.md) - System architecture overview
