# Docker Support for Mystira.App

This document explains how to build and run the Mystira.App APIs using Docker.

## Prerequisites

- Docker Desktop or Docker Engine installed
- Docker Compose (included with Docker Desktop)
- (Optional) Azure Cosmos DB and Azure Blob Storage accounts

## Quick Start

### Using Docker Compose (Recommended)

1. **Copy the environment template:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` file with your configuration:**
   - For local development, you can leave `COSMOS_DB_CONNECTION_STRING` empty to use in-memory database
   - Set Discord bot token if you want to test Discord integration
   - Generate a JWT secret key (minimum 32 characters)

3. **Build and run all services:**
   ```bash
   docker-compose up --build
   ```

4. **Access the APIs:**
   - Main API: http://localhost:5000
   - Admin API: http://localhost:5100
   - Swagger UI (Main API): http://localhost:5000
   - Swagger UI (Admin API): http://localhost:5100

### Building Individual Images

Build from the **repository root** (important for correct file paths):

```bash
# Build Main API
docker build -f src/Mystira.App.Api/Dockerfile -t mystira-api:latest .

# Build Admin API
docker build -f src/Mystira.App.Admin.Api/Dockerfile -t mystira-admin-api:latest .
```

### Running Individual Containers

```bash
# Run Main API (with in-memory database)
docker run -d \
  --name mystira-api \
  -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Discord__Enabled=false \
  mystira-api:latest

# Run Admin API (with in-memory database)
docker run -d \
  --name mystira-admin-api \
  -p 5100:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  mystira-admin-api:latest
```

## Configuration

### Environment Variables

The Docker containers can be configured using environment variables:

#### Database Configuration
- `ConnectionStrings__CosmosDb` - Azure Cosmos DB connection string (empty = use in-memory DB)
- `Azure__CosmosDb__DatabaseName` - Cosmos DB database name (default: MystiraAppDb)

#### Azure Storage
- `ConnectionStrings__AzureStorage` - Azure Blob Storage connection string
- `Azure__BlobStorage__ContainerName` - Blob container name (default: mystira-app-media)

#### JWT Authentication
- `JwtSettings__Issuer` - JWT token issuer
- `JwtSettings__Audience` - JWT token audience
- `JwtSettings__SecretKey` - JWT signing key (minimum 32 characters)

#### Discord Bot (Main API only)
- `Discord__Enabled` - Enable/disable Discord bot (true/false)
- `Discord__BotToken` - Discord bot token from Discord Developer Portal
- `Discord__GuildId` - Discord server ID for command registration

#### Database Initialization
- `InitializeDatabaseOnStartup` - Create database on startup (default: false)
- `SeedMasterDataOnStartup` - Seed master data on startup (default: false)

### Volume Mounts

You can mount configuration files or logs:

```bash
# Mount custom appsettings file
docker run -d \
  -v /path/to/appsettings.local.json:/app/appsettings.local.json:ro \
  mystira-api:latest

# Mount logs directory
docker run -d \
  -v /path/to/logs:/app/logs \
  mystira-api:latest
```

## Troubleshooting

### Discord API Deserialization Errors

If you see Discord API deserialization errors:
1. Ensure Discord.Net package version is 3.17.1 or higher
2. Verify `Discord__Enabled=false` if not using Discord bot
3. Check that `DISCORD_BOT_TOKEN` is valid if Discord is enabled

### Volume Mount Errors

If you see volume mount errors:
1. Ensure you're building from the **repository root**, not the project directory
2. Check that `.dockerignore` is in the repository root
3. Verify appsettings.json files exist in the source projects

### Configuration Not Found

If the container can't find configuration:
1. Check that appsettings files are copied in the final stage of the Dockerfile (look for explicit COPY of appsettings.json)
2. Verify environment variables are set correctly
3. Use `docker logs <container-name>` to see detailed error messages

### Permission Errors

The containers run as a non-root user (`appuser`) for security:
1. Ensure mounted volumes have correct permissions
2. For logs, create the directory first: `mkdir -p ./logs/api`

## Health Checks

Both APIs expose a `/health` endpoint for health monitoring:

```bash
# Check Main API health
curl http://localhost:5000/health

# Check Admin API health
curl http://localhost:5100/health
```

## Production Deployment

For production deployments:

1. **Use Azure App Service or Azure Container Apps** (recommended)
2. **Store secrets in Azure Key Vault**, not environment variables
3. **Use Azure Cosmos DB** instead of in-memory database
4. **Enable HTTPS** at the load balancer level
5. **Set `ASPNETCORE_ENVIRONMENT=Production`**
6. **Configure proper CORS origins** for your frontend

### Example Production Run

```bash
docker run -d \
  --name mystira-api-prod \
  -p 80:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__CosmosDb="<from-key-vault>" \
  -e JwtSettings__SecretKey="<from-key-vault>" \
  -e Discord__Enabled=true \
  -e Discord__BotToken="<from-key-vault>" \
  mystira-api:latest
```

## Building for Multiple Platforms

To build multi-platform images (e.g., for ARM64 and AMD64):

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f src/Mystira.App.Api/Dockerfile \
  -t mystira-api:latest \
  --push \
  .
```

## Development Tips

1. **Use docker-compose for local development** - it's easier than managing individual containers
2. **Leave database connection strings empty** to use in-memory database for faster startup
3. **Mount a logs volume** for easier debugging
4. **Use `docker-compose logs -f <service>` to tail logs**
5. **Rebuild after code changes:** `docker-compose up --build`

## Related Documentation

- [Setup Instructions](docs/setup/SETUP.md)
- [Architecture Documentation](docs/architecture/ARCHITECTURE.md)
- [Discord Bot Integration](src/Mystira.App.Infrastructure.Discord/README.md)
