# Media Use Cases

Media asset management use cases for upload, download, transcoding, and metadata operations.

## ✅ Hexagonal Architecture - FULLY COMPLIANT

**Layer**: **Application - Media Use Cases**

**Purpose**: Business logic for media asset management

**Status**: ✅ **Fully compliant** - uses Application.Ports interfaces

## Port Interfaces Used

These use cases depend on port interfaces defined in `Application/Ports`:
- **`IBlobService`** (Ports/Storage) - Media file storage operations
- **`IAudioTranscodingService`** (Ports/Media) - Audio format conversion
- **`IMediaAssetRepository`** (Ports/Data) - Media metadata persistence
- **`IUnitOfWork`** (Ports/Data) - Transaction management

Infrastructure implementations:
- `AzureBlobService` implements `IBlobService`
- `FfmpegAudioTranscodingService` implements `IAudioTranscodingService`
- `MediaAssetRepository` implements `IMediaAssetRepository`

## Implemented Use Cases

- ✅ `UploadMediaUseCase` - Upload media file to blob storage and save metadata
- ✅ `DeleteMediaUseCase` - Delete media file and metadata
- ✅ `GetMediaMetadataUseCase` - Retrieve media metadata
- ✅ `TranscodeAudioUseCase` - Convert audio files between formats
- ✅ `DownloadMediaUseCase` - Download media file from storage

## Domain Models

Media entities are in `Domain/Models`:
- `MediaAsset` - Media file metadata and references
- `MediaMetadata` - Extended media information

## Example Use Case

```csharp
public class UploadMediaUseCase
{
    private readonly IBlobService _blobService;              // Port ✅
    private readonly IMediaAssetRepository _repository;       // Port ✅
    private readonly IUnitOfWork _unitOfWork;                 // Port ✅

    public async Task<MediaAsset> ExecuteAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        // Upload to storage
        var blobName = await _blobService.UploadMediaAsync(
            fileStream,
            fileName,
            contentType);

        // Create domain entity
        var mediaAsset = new MediaAsset
        {
            Id = Guid.NewGuid().ToString(),
            BlobName = blobName,
            FileName = fileName,
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        };

        // Persist metadata
        await _repository.AddAsync(mediaAsset);
        await _unitOfWork.SaveChangesAsync();

        return mediaAsset;
    }
}
```

## Testing

Use cases are easily testable by mocking port interfaces:

```csharp
[Fact]
public async Task UploadMedia_Success()
{
    var mockBlobService = new Mock<IBlobService>();
    var mockRepository = new Mock<IMediaAssetRepository>();
    var mockUnitOfWork = new Mock<IUnitOfWork>();

    // Test without Azure or database ✅
}
```
