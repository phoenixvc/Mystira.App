# Mystira Azure Infrastructure

This project contains all **Azure Cloud-specific** infrastructure components for the Mystira.App application, including cloud services, configuration, health checks, and deployment templates.

## Overview

The Mystira.App Azure Infrastructure project provides **CLOUD SERVICES ONLY**:

- **Azure Blob Storage Service (Cloud)** - Cloud media asset management with upload, download, and deletion capabilities
- **Azure Cosmos DB Health Checks (Cloud)** - Cloud database connectivity monitoring
- **Azure Blob Storage Health Checks (Cloud)** - Cloud storage service monitoring
- **Azure Configuration Management** - Centralized Azure cloud service configuration
- **Azure Deployment Templates** - Bicep Infrastructure as Code for cloud resources
- **Service Registration Extensions** - Easy integration with .NET dependency injection for cloud services

> **Important**: This project contains ONLY cloud Azure services. Local development uses in-memory databases and local file storage, which are configured separately in the API and MAUI projects.

## Project Structure

```
Mystira.App.Infrastructure.Azure/
├── Services/
│   ├── IAzureBlobService.cs          # Blob storage interface
│   └── AzureBlobService.cs           # Blob storage implementation
├── HealthChecks/
│   └── AzureHealthChecks.cs          # Azure service health checks
├── Configuration/
│   └── AzureOptions.cs               # Configuration models
├── Deployment/
│   ├── main.bicep                    # Main deployment template
│   ├── app-service.bicep             # App Service configuration
│   ├── cosmos-db.bicep               # Cosmos DB configuration
│   ├── storage.bicep                 # Storage account configuration
│   ├── ci-cd.yml                     # GitHub Actions workflow
│   └── deploy.sh                     # Deployment script
└── ServiceCollectionExtensions.cs    # DI container extensions
```

## Usage

### 1. Add to Your Project

Add a reference to this project in your API or application project:

```xml
<ProjectReference Include="..\Mystira.App.Infrastructure.Azure\Mystira.App.Infrastructure.Azure.csproj" />
```

### 2. Configure Services

In your `Program.cs` or `Startup.cs`:

```csharp
using Mystira.App.Infrastructure.Azure;

// Add Azure infrastructure services
builder.Services.AddAzureInfrastructure(builder.Configuration);

// Or add specific services
builder.Services.AddAzureBlobStorage(builder.Configuration);
builder.Services.AddCosmosDb<YourDbContext>(builder.Configuration);
```

### 3. Configuration

Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CosmosDb": "your-cosmos-db-connection-string",
    "AzureStorage": "your-azure-storage-connection-string"
  },
  "Azure": {
    "BlobStorage": {
      "ContainerName": "mystira-app-media",
      "MaxFileSizeMB": 10
    },
    "CosmosDb": {
      "DatabaseName": "MystiraAppDb"
    }
  }
}
```

## Services

### IAzureBlobService

Provides media asset management capabilities:

```csharp
public interface IAzureBlobService
{
    Task<string> UploadMediaAsync(Stream fileStream, string fileName, string contentType);
    Task<string> GetMediaUrlAsync(string blobName);
    Task<bool> DeleteMediaAsync(string blobName);
    Task<List<string>> ListMediaAsync(string prefix = "");
    Task<Stream?> DownloadMediaAsync(string blobName);
}
```

**Usage Example:**

```csharp
[ApiController]
public class MediaController : ControllerBase
{
    private readonly IAzureBlobService _blobService;

    public MediaController(IAzureBlobService blobService)
    {
        _blobService = blobService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var blobName = await _blobService.UploadMediaAsync(stream, file.FileName, file.ContentType);
        var url = await _blobService.GetMediaUrlAsync(blobName);
        return Ok(new { blobName, url });
    }
}
```

### Health Checks

The infrastructure automatically registers health checks for:

- **Cosmos DB connectivity** - Validates database connection
- **Blob Storage connectivity** - Validates storage service access

Access health checks at: `/health`

## Deployment

### Prerequisites

1. Azure CLI installed and logged in
2. Appropriate Azure subscription permissions
3. OpenSSL (for JWT secret generation)

### Quick Deploy

```bash
cd Deployment/
./deploy.sh -g "dev-euw-rg-mystira-app" -e "dev" -l "eastus"
```

### Deployment Options

```bash
./deploy.sh [OPTIONS]

Options:
  -e, --environment    Environment (dev, staging, prod) [default: dev]
  -g, --resource-group Resource group name [required]
  -l, --location       Azure location [default: eastus]
  -s, --subscription   Azure subscription ID [optional]
  -h, --help          Show help message
```

### Manual Deployment

```bash
# Create resource group
az group create --name "dev-euw-rg-mystira-app" --location "eastus"

# Deploy infrastructure
az deployment group create \
  --resource-group "dev-euw-rg-mystira-app" \
  --template-file "main.bicep" \
  --parameters environment="dev"
```

## Azure Resources Created

The deployment creates the following Azure resources:

### App Service
- **Plan**: Linux-based App Service Plan
- **Runtime**: .NET 9.0
- **Features**: Always On, HTTPS Only, Health Check monitoring
- **Configuration**: Environment variables for connection strings and JWT settings

### Cosmos DB
- **Type**: Serverless SQL API
- **Containers**: UserProfiles, Scenarios, GameSessions, CompassTrackings
- **Consistency**: Session-level consistency
- **Partition Keys**: Optimized for query patterns

### Storage Account
- **Type**: StorageV2 with Hot access tier
- **Features**: HTTPS only, TLS 1.2 minimum
- **Containers**: mystira-app-media (public blob access)
- **CORS**: Configured for frontend domains

## Security Features

- **HTTPS Enforcement** - All traffic requires HTTPS
- **TLS 1.2 Minimum** - Modern encryption standards
- **Managed Identity Support** - For passwordless authentication
- **CORS Configuration** - Restricted to known domains
- **Public Blob Access** - Only for media container with appropriate permissions

## Cost Optimization

- **Cosmos DB Serverless** - Pay-per-request pricing
- **App Service Basic Tier** - Cost-effective for development
- **Storage Hot Tier** - Optimized for frequent access
- **Auto-scaling** - Resources scale based on demand

## Monitoring

Health checks are available at:
- `/health` - Comprehensive health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

Monitor the following metrics:
- App Service response time and availability
- Cosmos DB request units and throttling
- Storage account blob operations and bandwidth

## Environment-Specific Configuration

### Development
- **App Service**: Basic B1 SKU
- **Storage**: Standard LRS replication
- **Cosmos DB**: Serverless with minimal throughput

### Production
- **App Service**: Premium P1v3 SKU
- **Storage**: Standard GRS replication
- **Cosmos DB**: Serverless with auto-scale

## Troubleshooting

### Common Issues

1. **Deployment Fails**
   - Check Azure CLI login: `az account show`
   - Verify subscription permissions
   - Ensure resource group exists

2. **Health Checks Fail**
   - Verify connection strings in app settings
   - Check network connectivity
   - Review App Service logs

3. **Storage Access Issues**
   - Confirm CORS settings
   - Verify container permissions
   - Check storage account keys

### Support

For issues related to Azure infrastructure:
1. Check the deployment logs in Azure Portal
2. Review health check endpoints
3. Examine Application Insights telemetry
4. Contact the development team with specific error messages