# Startup Configuration Guide

## Database Initialization on Startup

### Problem
The application was hanging during startup when deployed to Azure App Service because the database initialization code (`EnsureCreatedAsync`) would hang indefinitely if:
- The Cosmos DB connection string was invalid or empty
- The database/containers didn't exist
- Network connectivity issues occurred
- Cosmos DB service was unavailable

### Solution
We've implemented a robust startup configuration system with the following improvements:

#### 1. Configuration Flags

**`InitializeDatabaseOnStartup`** (default: `false`)
- Controls whether the app attempts to initialize the database on startup
- **Production**: Set to `false` to skip database initialization (recommended)
- **Development**: Automatically `true` for in-memory databases
- Add to `appsettings.json` or Azure App Service Configuration

```json
{
  "InitializeDatabaseOnStartup": false
}
```

**`SeedMasterDataOnStartup`** (default: `false`)
- Controls whether to seed master data (CompassAxes, Archetypes, etc.) on startup
- Only runs if `InitializeDatabaseOnStartup` is `true`
- Automatically enabled for in-memory databases (local development)
- Production: Keep `false` and pre-seed data via separate process

```json
{
  "SeedMasterDataOnStartup": false
}
```

#### 2. Timeout Protection

- **Database initialization timeout**: 30 seconds
- **Seeding timeout**: 60 seconds
- **HTTP client timeout**: 30 seconds (Cosmos DB operations)
- **Request timeout**: 30 seconds (Cosmos DB operations)

If any timeout is reached, the application logs a warning and continues to start in degraded mode.

#### 3. Error Handling

- **Production (Cosmos DB)**: Logs errors and continues startup
- **Development (In-Memory)**: Fails fast to catch configuration issues early
- All errors are logged with detailed context for troubleshooting

### Production Deployment Checklist

1. **Pre-create Cosmos DB resources**:
   - Create database: `MystiraAppDb`
   - Create containers with correct partition keys (see error logs for details)

2. **Configure App Service Settings**:
   ```
   ConnectionStrings__CosmosDb = "AccountEndpoint=https://...;AccountKey=..."
   InitializeDatabaseOnStartup = false
   SeedMasterDataOnStartup = false
   ```

3. **Verify deployment**:
   - Check application logs for startup messages
   - Verify `/health` endpoint responds
   - Monitor for any database-related warnings

### Local Development

In-memory database is used by default when no Cosmos DB connection string is configured:

```json
{
  "ConnectionStrings": {
    "CosmosDb": ""
  }
}
```

This automatically:
- Enables `InitializeDatabaseOnStartup` (override in appsettings)
- Enables `SeedMasterDataOnStartup` (override in appsettings)
- Uses in-memory database provider

### Troubleshooting

**Application hangs on startup**:
- Check if `InitializeDatabaseOnStartup=true` is set
- Verify Cosmos DB connection string is valid
- Check network connectivity to Cosmos DB
- Review application logs for timeout messages

**Database not initialized**:
- Ensure `InitializeDatabaseOnStartup=true` in configuration
- Check application logs for timeout or error messages
- Verify Cosmos DB permissions and container existence

**Seeding fails**:
- Check `SeedMasterDataOnStartup` setting
- Verify JSON seed files exist in the application deployment
- Review logs for specific seeding errors

### Configuration Examples

**Azure App Service (Production)**:
```
InitializeDatabaseOnStartup=false
SeedMasterDataOnStartup=false
ConnectionStrings__CosmosDb=<your-cosmos-connection-string>
```

**Local Development (In-Memory)**:
```json
{
  "ConnectionStrings": {
    "CosmosDb": ""
  }
}
```

**Local Development (Cosmos DB)**:
```json
{
  "ConnectionStrings": {
    "CosmosDb": "<your-cosmos-connection-string>"
  },
  "InitializeDatabaseOnStartup": true,
  "SeedMasterDataOnStartup": true
}
```

### Monitoring

Watch for these log messages:

- ✅ `Database initialization skipped` - Normal for production
- ✅ `Database initialization succeeded` - Successful init
- ⚠️ `Database initialization timed out` - Connection issues
- ⚠️ `Master data seeding timed out` - Seeding issues
- ❌ `Failed to initialize database` - Configuration or permission errors

### Performance Impact

- Skipping database initialization reduces startup time by 5-30 seconds
- Recommended for production deployments with pre-configured databases
- No impact on runtime performance after startup completes
