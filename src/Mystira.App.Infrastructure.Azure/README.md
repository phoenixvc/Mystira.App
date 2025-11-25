# Mystira.App.Infrastructure.Azure

Azure Cloud infrastructure adapter implementing storage and media processing ports defined by the Application layer. This project serves as a **secondary adapter** in the hexagonal architecture.

## ✅ Hexagonal Architecture - FULLY COMPLIANT

**Layer**: **Infrastructure - Azure Adapter (Secondary/Driven)**

The Infrastructure.Azure layer is a **secondary adapter** (driven adapter) that:
- **Implements** storage and media port interfaces defined in `Application.Ports`
- **Provides** Azure Blob Storage implementation for media management
- **Offers** FFmpeg-based audio transcoding services
- **Manages** Azure-specific health checks and configuration
- **Abstracts** Azure SDK details from the Application layer
- **ZERO reverse dependencies** - Application never references Infrastructure

**Dependency Flow** (Correct ✅):
```
Domain Layer (Core)
    ↓ references
Application Layer
    ↓ defines
Application.Ports.Storage (IBlobService)
Application.Ports.Media (IAudioTranscodingService)
    ↑ implemented by
Infrastructure.Azure (THIS - Implementations)
    ↓ uses
Azure SDK, FFmpeg
```

**Key Principles**:
- ✅ **Port Implementation** - Implements `IBlobService` and `IAudioTranscodingService` from Application
- ✅ **Technology Adapter** - Adapts Azure Blob Storage SDK to Application needs
- ✅ **Dependency Inversion** - Application defines ports, Infrastructure implements them
- ✅ **Clean Architecture** - No circular dependencies, proper layering
- ✅ **Swappable** - Can be replaced with other cloud providers (AWS S3, Google Cloud Storage)

## Project Structure

```
Mystira.App.Infrastructure.Azure/
├── Services/
│   ├── AzureBlobService.cs                   # Implements IBlobService
│   └── FfmpegAudioTranscodingService.cs      # Implements IAudioTranscodingService
├── HealthChecks/
│   └── AzureHealthChecks.cs                  # Azure service health monitoring
├── Configuration/
│   └── AzureOptions.cs                       # Configuration models
├── Deployment/
│   ├── main.bicep                            # Main deployment template
│   ├── app-service.bicep                     # App Service configuration
│   ├── cosmos-db.bicep                       # Cosmos DB configuration
│   ├── storage.bicep                         # Storage account configuration
│   ├── ci-cd.yml                             # GitHub Actions workflow
│   └── deploy.sh                             # Deployment script
└── ServiceCollectionExtensions.cs            # DI container extensions
```

**Port Interfaces** (defined in Application layer):
- `IBlobService` lives in `Application/Ports/Storage/`
- `IAudioTranscodingService` lives in `Application/Ports/Media/`
- Infrastructure.Azure references Application to implement these ports

## Port Implementations

### IBlobService Implementation

Application defines the port interface:

```csharp
// Location: Application/Ports/Storage/IBlobService.cs
namespace Mystira.App.Application.Ports.Storage;

public interface IBlobService
{
    Task<string> UploadMediaAsync(Stream fileStream, string fileName, string contentType);
    Task<string> GetMediaUrlAsync(string blobName);
    Task<bool> DeleteMediaAsync(string blobName);
    Task<List<string>> ListMediaAsync(string prefix = "");
    Task<Stream?> DownloadMediaAsync(string blobName);
}
```

Infrastructure.Azure provides the Azure-specific implementation:

```csharp
// Location: Infrastructure.Azure/Services/AzureBlobService.cs
using Mystira.App.Application.Ports.Storage;  // Port interface ✅
using Azure.Storage.Blobs;

namespace Mystira.App.Infrastructure.Azure.Services;

public class AzureBlobService : IBlobService  // Implements port ✅
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobService> _logger;
    private readonly string _containerName;

    public AzureBlobService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<AzureBlobService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _containerName = configuration["Azure:BlobStorage:ContainerName"]
            ?? "mystira-app-media";
    }

    public async Task<string> UploadMediaAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
        {
            ContentType = contentType
        });

        _logger.LogInformation("Uploaded blob: {BlobName}", blobName);
        return blobName;
    }

    public async Task<string> GetMediaUrlAsync(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteMediaAsync(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DeleteIfExistsAsync();
        return response.Value;
    }

    public async Task<List<string>> ListMediaAsync(string prefix = "")
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobs = new List<string>();

        await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix))
        {
            blobs.Add(blob.Name);
        }

        return blobs;
    }

    public async Task<Stream?> DownloadMediaAsync(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
            return null;

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
}
```

### IAudioTranscodingService Implementation

Application defines the port interface:

```csharp
// Location: Application/Ports/Media/IAudioTranscodingService.cs
namespace Mystira.App.Application.Ports.Media;

public interface IAudioTranscodingService
{
    Task<Stream> TranscodeToMp3Async(Stream inputStream, string inputFormat);
    Task<Stream> TranscodeToOggAsync(Stream inputStream, string inputFormat);
    Task<AudioMetadata> GetMetadataAsync(Stream audioStream);
}
```

Infrastructure.Azure provides FFmpeg implementation:

```csharp
// Location: Infrastructure.Azure/Services/FfmpegAudioTranscodingService.cs
using Mystira.App.Application.Ports.Media;  // Port interface ✅
using FFMpegCore;

namespace Mystira.App.Infrastructure.Azure.Services;

public class FfmpegAudioTranscodingService : IAudioTranscodingService  // Implements port ✅
{
    private readonly ILogger<FfmpegAudioTranscodingService> _logger;

    public FfmpegAudioTranscodingService(ILogger<FfmpegAudioTranscodingService> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> TranscodeToMp3Async(Stream inputStream, string inputFormat)
    {
        var outputStream = new MemoryStream();

        await FFMpegArguments
            .FromPipeInput(new StreamPipeSource(inputStream))
            .OutputToPipe(new StreamPipeSink(outputStream), options => options
                .WithAudioCodec("libmp3lame")
                .WithAudioBitrate(128))
            .ProcessAsynchronously();

        outputStream.Position = 0;
        _logger.LogInformation("Transcoded audio to MP3");
        return outputStream;
    }

    public async Task<Stream> TranscodeToOggAsync(Stream inputStream, string inputFormat)
    {
        var outputStream = new MemoryStream();

        await FFMpegArguments
            .FromPipeInput(new StreamPipeSource(inputStream))
            .OutputToPipe(new StreamPipeSink(outputStream), options => options
                .WithAudioCodec("libvorbis")
                .WithAudioBitrate(128))
            .ProcessAsynchronously();

        outputStream.Position = 0;
        _logger.LogInformation("Transcoded audio to OGG");
        return outputStream;
    }
}
```

## Usage in Application Layer

Application use cases depend on port interfaces, not Azure implementations:

```csharp
// Location: Application/UseCases/Media/UploadMediaUseCase.cs
using Mystira.App.Application.Ports.Storage;  // Port ✅

namespace Mystira.App.Application.UseCases.Media;

public class UploadMediaUseCase
{
    private readonly IBlobService _blobService;  // Port interface ✅
    private readonly IMediaAssetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadMediaUseCase> _logger;

    public UploadMediaUseCase(
        IBlobService blobService,  // Port ✅
        IMediaAssetRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UploadMediaUseCase> logger)
    {
        _blobService = blobService;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MediaAsset> ExecuteAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        // Upload to storage (Azure, AWS, local file system, etc.)
        var blobName = await _blobService.UploadMediaAsync(
            fileStream,
            fileName,
            contentType);

        // Get public URL
        var url = await _blobService.GetMediaUrlAsync(blobName);

        // Create domain entity
        var mediaAsset = new MediaAsset
        {
            Id = Guid.NewGuid().ToString(),
            BlobName = blobName,
            FileName = fileName,
            ContentType = contentType,
            Url = url,
            UploadedAt = DateTime.UtcNow
        };

        // Persist metadata
        await _repository.AddAsync(mediaAsset);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Media uploaded: {FileName}", fileName);
        return mediaAsset;
    }
}
```

**Benefits**:
- ✅ Application never references Infrastructure.Azure
- ✅ Can swap Azure for AWS S3 without changing Application
- ✅ Easy to mock for testing (in-memory, local file system)
- ✅ Clear separation of concerns

## Dependency Injection

Register Azure implementations in API layer `Program.cs`:

```csharp
using Mystira.App.Application.Ports.Storage;
using Mystira.App.Application.Ports.Media;
using Mystira.App.Infrastructure.Azure.Services;

// Register Azure Blob Storage
builder.Services.AddSingleton(x =>
{
    var connectionString = builder.Configuration
        .GetConnectionString("AzureStorage");
    return new BlobServiceClient(connectionString);
});

// Register port implementations
builder.Services.AddScoped<IBlobService, AzureBlobService>();  // Azure adapter ✅
builder.Services.AddScoped<IAudioTranscodingService, FfmpegAudioTranscodingService>();  // FFmpeg adapter ✅

// Or use extension method
builder.Services.AddAzureInfrastructure(builder.Configuration);
```

For local development, swap with local implementation:

```csharp
#if DEBUG
// Use local file storage for development
builder.Services.AddScoped<IBlobService, LocalFileStorageService>();
#else
// Use Azure for production
builder.Services.AddScoped<IBlobService, AzureBlobService>();
#endif
```

## Configuration

Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;"
  },
  "Azure": {
    "BlobStorage": {
      "ContainerName": "mystira-app-media",
      "MaxFileSizeMB": 10
    }
  }
}
```

For local development with Azurite:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "UseDevelopmentStorage=true"
  }
}
```

## Health Checks

Azure-specific health checks are automatically registered:

```csharp
public static class AzureHealthChecks
{
    public static IHealthChecksBuilder AddAzureHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        // Blob Storage connectivity
        builder.AddAzureBlobStorage(
            configuration.GetConnectionString("AzureStorage"),
            name: "azure-blob-storage",
            tags: new[] { "storage", "azure" });

        // Cosmos DB connectivity (if using)
        builder.AddCosmosDb(
            configuration.GetConnectionString("CosmosDb"),
            name: "cosmos-db",
            tags: new[] { "database", "azure" });

        return builder;
    }
}
```

Access health checks at:
- `/health` - Comprehensive health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

## Testing

### Unit Testing with Mocked Ports

Application use cases can be tested without Azure:

```csharp
[Fact]
public async Task UploadMedia_WithValidFile_CreatesMediaAsset()
{
    // Arrange
    var mockBlobService = new Mock<IBlobService>();  // Mock port ✅
    mockBlobService
        .Setup(s => s.UploadMediaAsync(It.IsAny<Stream>(), "test.mp3", "audio/mpeg"))
        .ReturnsAsync("blob-123");
    mockBlobService
        .Setup(s => s.GetMediaUrlAsync("blob-123"))
        .ReturnsAsync("https://storage.example.com/blob-123");

    var useCase = new UploadMediaUseCase(
        mockBlobService.Object,
        mockRepository.Object,
        mockUnitOfWork.Object,
        mockLogger.Object);

    // Act
    using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("test audio"));
    var result = await useCase.ExecuteAsync(fileStream, "test.mp3", "audio/mpeg");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("blob-123", result.BlobName);
    mockBlobService.Verify(s => s.UploadMediaAsync(
        It.IsAny<Stream>(),
        "test.mp3",
        "audio/mpeg"), Times.Once);
}
```

### Integration Testing with Azurite

Test Azure implementations using Azurite emulator:

```bash
# Start Azurite emulator
docker run -p 10000:10000 -p 10001:10001 mcr.microsoft.com/azure-storage/azurite
```

```csharp
[Fact]
public async Task AzureBlobService_UploadAndDownload_Success()
{
    // Arrange
    var connectionString = "UseDevelopmentStorage=true";  // Azurite
    var blobServiceClient = new BlobServiceClient(connectionString);
    var service = new AzureBlobService(
        blobServiceClient,
        mockConfiguration.Object,
        mockLogger.Object);

    // Act
    using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
    var blobName = await service.UploadMediaAsync(uploadStream, "test.txt", "text/plain");

    var downloadStream = await service.DownloadMediaAsync(blobName);

    // Assert
    Assert.NotNull(downloadStream);
    using var reader = new StreamReader(downloadStream);
    var content = await reader.ReadToEndAsync();
    Assert.Equal("test content", content);
}
```

## Azure Deployment

### Prerequisites

1. Azure CLI installed and logged in
2. Appropriate Azure subscription permissions
3. OpenSSL (for JWT secret generation)

### Quick Deploy

```bash
cd Deployment/
./deploy.sh -g "dev-euw-rg-mystira-app" -e "dev" -l "westeurope"
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
az group create --name "dev-euw-rg-mystira-app" --location "westeurope"

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
- **Containers**: UserProfiles, Scenarios, GameSessions, MediaAssets
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
- **Alternative**: Use Azurite for local development

### Production
- **App Service**: Premium P1v3 SKU
- **Storage**: Standard GRS replication
- **Cosmos DB**: Serverless with auto-scale

## Architectural Compliance Verification

Verify that Infrastructure.Azure correctly implements Application ports:

```bash
# Check that Infrastructure.Azure references Application
grep "Mystira.App.Application" Mystira.App.Infrastructure.Azure.csproj
# Expected: <ProjectReference Include="..\Mystira.App.Application\...">

# Check that services use Application.Ports namespace
grep -r "using Mystira.App.Application.Ports" Services/
# Expected: All service files import from Application.Ports

# Check NO Infrastructure references in Application
cd ../Mystira.App.Application
grep -r "using Mystira.App.Infrastructure" .
# Expected: (no output - Application never references Infrastructure)
```

**Results**:
- ✅ Infrastructure.Azure references Application (correct direction)
- ✅ Services implement Application.Ports interfaces
- ✅ Application has ZERO Infrastructure references
- ✅ Full dependency inversion achieved

## Alternative Implementations

The port-based architecture allows easy swapping of implementations:

### Local File Storage (Development)
```csharp
public class LocalFileStorageService : IBlobService
{
    private readonly string _basePath;

    public async Task<string> UploadMediaAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_basePath, blobName);

        using var fileStream = File.Create(filePath);
        await fileStream.CopyToAsync(fileStream);

        return blobName;
    }
    // ... other methods
}
```

### AWS S3 (Alternative Cloud)
```csharp
public class AwsS3BlobService : IBlobService
{
    private readonly IAmazonS3 _s3Client;

    public async Task<string> UploadMediaAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        var key = $"{Guid.NewGuid()}_{fileName}";

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType
        });

        return key;
    }
    // ... other methods
}
```

## Troubleshooting

### Common Issues

1. **Connection String Invalid**
   - Verify Azure Storage connection string in configuration
   - Check Azurite is running for local development
   - Ensure storage account exists in Azure

2. **Health Checks Fail**
   - Verify connection strings in app settings
   - Check network connectivity to Azure
   - Review App Service logs

3. **Storage Access Issues**
   - Confirm CORS settings
   - Verify container permissions
   - Check storage account keys

## Related Documentation

- **[Application](../Mystira.App.Application/README.md)** - Defines port interfaces this layer implements
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Data infrastructure (similar pattern)
- **[API](../Mystira.App.Api/README.md)** - Registers Azure implementations via DI
- **[Admin.Api](../Mystira.App.Admin.Api/README.md)** - Also registers implementations

## Summary

**What This Layer Does**:
- ✅ Implements storage and media port interfaces from Application.Ports
- ✅ Provides Azure Blob Storage-based media management
- ✅ Offers FFmpeg-based audio transcoding
- ✅ Manages Azure-specific health checks
- ✅ Maintains clean hexagonal architecture

**What This Layer Does NOT Do**:
- ❌ Define port interfaces (Application does that)
- ❌ Contain business logic (Application/Domain does that)
- ❌ Make decisions about when to upload/transcode (Application decides)

**Key Success Metrics**:
- ✅ **Zero reverse dependencies** - Application never references Infrastructure.Azure
- ✅ **Clean interfaces** - All ports defined in Application layer
- ✅ **Testability** - Use cases can mock storage/transcoding services
- ✅ **Swappability** - Can replace Azure with AWS, Google Cloud, or local file system

## License

Copyright (c) 2025 Mystira. All rights reserved.
