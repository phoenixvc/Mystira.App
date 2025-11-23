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
./deploy.sh -g "dev-wus-rg-mystira-app" -e "dev" -l "westus"
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
az group create --name "dev-wus-rg-mystira-app" --location "westus"

# Deploy infrastructure
az deployment group create \
  --resource-group "dev-wus-rg-mystira-app" \
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

## 🔍 Architectural Analysis

### Current State Assessment

**File Count**: ~9 C# files (small, focused)
**Project References**: 1 (Domain only)
- Domain ✅ (correct - infrastructure can reference domain)
- Application ❌ (missing - should reference for port interfaces)

**Dependencies**:
- Azure.Storage.Blobs ✅ (cloud storage SDK)
- Azure.Identity ✅ (authentication)
- Microsoft.EntityFrameworkCore.Cosmos ✅ (database)
- FFMpegCore ✅ (audio transcoding)

**Folders**:
- Services/ ✅ (Azure service implementations)
- HealthChecks/ ✅ (Azure health monitoring)
- Configuration/ ✅ (Azure options)
- ServiceCollectionExtensions ✅ (DI registration)

### ⚠️ Architectural Issues Found

#### 1. **Port Interfaces in Infrastructure Layer** (MEDIUM)
**Location**: `Services/IAzureBlobService.cs`, `Services/IAudioTranscodingService.cs`

**Issue**: Port interfaces (abstractions) are defined in Infrastructure project:
```csharp
// Currently in Infrastructure.Azure/Services/
public interface IAzureBlobService  // This is a PORT!
{
    Task<string> UploadMediaAsync(...);
    Task<string> GetMediaUrlAsync(string blobName);
    // ...
}
```

**Impact**:
- ⚠️ Violates Dependency Inversion Principle
- ⚠️ Application layer would need to reference Infrastructure to use the interface
- ⚠️ Port (abstraction) and Adapter (implementation) in same project
- ⚠️ Can't easily swap implementations (e.g., local file storage for testing)

**Recommendation**:
- **MOVE** `IAzureBlobService` → `Application/Ports/Storage/IBlobService.cs`
- **MOVE** `IAudioTranscodingService` → `Application/Ports/Media/IAudioTranscodingService.cs`
- **KEEP** implementations (`AzureBlobService`, `FfmpegAudioTranscodingService`) in Infrastructure.Azure
- **ADD** Application project reference to Infrastructure.Azure
- Infrastructure implements ports defined in Application

**Correct Structure**:
```
Application/Ports/Storage/
├── IBlobService.cs                    # Port interface

Infrastructure.Azure/Services/
├── AzureBlobService.cs                # Adapter (implements IBlobService)
```

#### 2. **Missing Application Reference** (MEDIUM)
**Location**: `Mystira.App.Infrastructure.Azure.csproj`

**Issue**: Infrastructure.Azure does not reference Application layer:
```xml
<ItemGroup>
  <ProjectReference Include="..\Mystira.App.Domain\Mystira.App.Domain.csproj" />
  <!-- Missing: Application reference -->
</ItemGroup>
```

**Impact**:
- ⚠️ Can't implement ports defined in Application
- ⚠️ Forces port interfaces to live in Infrastructure (wrong layer)
- ⚠️ Breaks hexagonal architecture dependency flow

**Recommendation**:
- **ADD** reference to Application project
- After moving interfaces to Application/Ports
- Correct dependency flow: Infrastructure → Application → Domain

**Example**:
```diff
  <ItemGroup>
    <ProjectReference Include="..\Mystira.App.Domain\Mystira.App.Domain.csproj" />
+   <ProjectReference Include="..\Mystira.App.Application\Mystira.App.Application.csproj" />
  </ItemGroup>
```

#### 3. **Dual EF Core Providers** (INFO)
**Location**: Package references

**Issue**: Both Cosmos DB and InMemory EF Core providers:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Cosmos" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
```

**Impact**:
- ⚠️ Unclear why both providers are needed in Azure infrastructure
- ⚠️ InMemory provider typically for testing, shouldn't be in production infra

**Recommendation**:
- **CLARIFY** why InMemory provider is needed
- Consider moving InMemory to test projects only
- Or document if needed for specific dev/test scenarios

### ✅ What's Working Well

1. **Focused Scope** - Only Azure-specific infrastructure
2. **Clean Services** - Blob storage and transcoding well-separated
3. **Health Checks** - Proper monitoring infrastructure
4. **Deployment Automation** - Bicep templates and scripts
5. **Security** - HTTPS enforcement, TLS 1.2, managed identity support
6. **Cost Optimization** - Serverless Cosmos DB, appropriate tiers
7. **Clear Documentation** - Deployment guides and troubleshooting

## 📋 Refactoring TODO

### 🟡 High Priority

- [ ] **Move port interfaces to Application layer**
  - Move `Services/IAzureBlobService.cs` → `Application/Ports/Storage/IBlobService.cs`
  - Move `Services/IAudioTranscodingService.cs` → `Application/Ports/Media/IAudioTranscodingService.cs`
  - Rename to remove "Azure" prefix from interface names (implementation-agnostic)
  - Location: `Infrastructure.Azure/Services/I*.cs`

- [ ] **Add Application project reference**
  - Add `<ProjectReference Include="..\Mystira.App.Application\..." />`
  - Update implementations to reference Application ports
  - Location: `Mystira.App.Infrastructure.Azure.csproj`

- [ ] **Update implementations to use Application ports**
  - `AzureBlobService : IBlobService` (from Application)
  - `FfmpegAudioTranscodingService : IAudioTranscodingService` (from Application)
  - Remove local interface definitions

### 🟢 Medium Priority

- [ ] **Clarify EF Core InMemory usage**
  - Document why InMemory provider is needed
  - Consider removing if only for testing
  - Move to test projects if appropriate

- [ ] **Add local file storage adapter**
  - Create `LocalFileStorageService : IBlobService`
  - For local development without Azure
  - Location: New `Infrastructure.Local` project or similar

### 🔵 Low Priority

- [ ] **Add integration tests**
  - Test Azure Blob Service with Azurite emulator
  - Test health checks
  - Verify Cosmos DB connectivity

## 💡 Recommendations

### Immediate Actions
1. **Move port interfaces to Application/Ports** - Correct dependency inversion
2. **Add Application reference** - Enable proper layering
3. **Document InMemory provider usage** - Clarify purpose

### Short-term
1. **Create local storage adapter** - Development without Azure
2. **Add port documentation** - Explain storage abstraction
3. **Integration tests** - Use Azurite for blob storage tests

### Long-term
1. **Consider multiple storage backends** - AWS S3, Google Cloud Storage adapters
2. **Enhanced monitoring** - Application Insights integration
3. **Performance optimization** - CDN for media delivery

## 📊 SWOT Analysis

### Strengths 💪
- ✅ **Focused Scope** - Only Azure infrastructure, well-bounded
- ✅ **Modern Azure SDKs** - Latest Azure packages
- ✅ **IaC with Bicep** - Deployment automation
- ✅ **Health Checks** - Proper monitoring
- ✅ **Security Best Practices** - HTTPS, TLS, managed identity
- ✅ **Cost-Optimized** - Serverless options
- ✅ **Small and Maintainable** - Only 9 files

### Weaknesses ⚠️
- ⚠️ **Port Interfaces Misplaced** - Should be in Application layer
- ⚠️ **Missing Application Reference** - Can't implement Application ports
- ⚠️ **Dual EF Core Providers** - Unclear purpose of InMemory
- ⚠️ **No Local Development Alternative** - Requires Azure for dev

### Opportunities 🚀
- 📈 **Multi-Cloud Support** - Add AWS, GCP adapters
- 📈 **Local Development** - File system adapter for dev
- 📈 **Enhanced Media Pipeline** - Image optimization, CDN
- 📈 **Managed Identity** - Passwordless authentication
- 📈 **Azure Functions** - Serverless media processing
- 📈 **Event Grid Integration** - Event-driven architecture

### Threats 🔒
- ⚡ **Azure Lock-in** - Hard to switch cloud providers
- ⚡ **Cost Surprises** - Azure costs can escalate
- ⚡ **Service Changes** - Azure SDKs and services evolve
- ⚡ **Regional Availability** - Cosmos DB not in all regions

### Risk Mitigation
1. **Abstract with ports** - Make cloud provider swappable
2. **Cost monitoring** - Azure Cost Management alerts
3. **Pin package versions** - Control SDK updates
4. **Multi-region deployment** - Geographic redundancy

## Related Documentation

- **[Application Layer](../Mystira.App.Application/README.md)** - Where port interfaces belong
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Data infrastructure (similar pattern)
- **[Main README](../../README.md)** - Project overview

## License

Copyright (c) 2025 Mystira. All rights reserved.