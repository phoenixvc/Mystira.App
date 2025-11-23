# Media Use Cases

## Status: ⚠️ Pending Domain Model Migration

Media use cases cannot be implemented in the Application layer until `MediaAsset` is moved from `Mystira.App.Api.Models` to `Mystira.App.Domain.Models`.

## Required Refactoring

Before implementing Media use cases, the following refactoring must be completed:

1. **Move MediaAsset to Domain Layer**
   - Move `MediaAsset` class from `src/Mystira.App.Api/Models/MediaModels.cs` to `src/Mystira.App.Domain/Models/MediaAsset.cs`
   - Move `MediaMetadata` class to Domain layer as well
   - Update all references across the codebase

2. **Move Repository Interface to Infrastructure**
   - Move `IMediaAssetRepository` from `src/Mystira.App.Api/Repositories/` to `src/Mystira.App.Infrastructure.Data/Repositories/`
   - Update repository implementation to use Domain model

3. **Add Infrastructure.Azure Reference**
   - Add project reference to `Mystira.App.Infrastructure.Azure` in `Mystira.App.Application.csproj` for blob storage operations

## Planned Use Cases

Once the refactoring is complete, the following use cases should be implemented:

- ✅ `GetMediaUseCase` - Get media by ID
- ✅ `GetMediaByFilenameUseCase` - Get media by filename (via metadata)
- ✅ `ListMediaUseCase` - List media with filtering and pagination
- ✅ `UploadMediaUseCase` - Upload media asset
- ✅ `UpdateMediaMetadataUseCase` - Update media metadata
- ✅ `DeleteMediaUseCase` - Delete media asset
- ✅ `DownloadMediaUseCase` - Download media file

## Current Implementation

Media operations are currently handled directly in:
- `MediaApiService` (composes upload and query services)
- `MediaUploadService` (handles file uploads)
- `MediaQueryService` (handles queries)

These services should be refactored to use the use cases once MediaAsset is moved to Domain.

