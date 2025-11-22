# MediaAsset Migration to Domain Layer - Status

## Overview

This document tracks the migration of `MediaAsset` from `Api.Models` to `Domain.Models` to align with hexagonal architecture principles.

## Completed ✅

1. **Domain Model Created**
   - ✅ Created `src/Mystira.App.Domain/Models/MediaAsset.cs`
   - ✅ Created `src/Mystira.App.Domain/Models/MediaMetadata.cs`

2. **Repository Interface Moved**
   - ✅ Created `src/Mystira.App.Infrastructure.Data/Repositories/IMediaAssetRepository.cs`
   - ✅ Created `src/Mystira.App.Infrastructure.Data/Repositories/MediaAssetRepository.cs`
   - ✅ Repository extends `IRepository<MediaAsset>` and implements domain-specific queries

3. **Application Project Updated**
   - ✅ Added reference to `Mystira.App.Infrastructure.Azure` in `Mystira.App.Application.csproj`

4. **DbContext Updated**
   - ✅ Updated `src/Mystira.App.Api/Data/MystiraAppDbContext.cs` to use `Domain.Models` instead of `Api.Models`

5. **README Fixed**
   - ✅ Fixed Cosmos Console path in README.md (added `src/` prefix)

## In Progress 🔄

### Reference Updates Required

The following files need to be updated to use `Mystira.App.Domain.Models.MediaAsset` instead of `Mystira.App.Api.Models.MediaAsset`:

#### API Project (`src/Mystira.App.Api/`)

- [ ] `Repositories/MediaAssetRepository.cs` - Update to use Domain model
- [ ] `Repositories/IMediaAssetRepository.cs` - Update to use Domain model (or remove if replaced)
- [ ] `Services/MediaApiService.cs` - Update references
- [ ] `Services/MediaUploadService.cs` - Update references
- [ ] `Services/MediaQueryService.cs` - Update references
- [ ] `Services/MediaMetadataService.cs` - May need updates
- [ ] `Controllers/MediaController.cs` - Update references
- [ ] `Models/MediaModels.cs` - Remove `MediaAsset` and `MediaMetadata` classes (keep DTOs)

#### Admin API Project (`src/Mystira.App.Admin.Api/`)

- [ ] `Repositories/MediaAssetRepository.cs` - Update to use Domain model
- [ ] `Repositories/IMediaAssetRepository.cs` - Update to use Domain model (or remove if replaced)
- [ ] `Services/MediaApiService.cs` - Update references
- [ ] `Controllers/MediaAdminController.cs` - Update references
- [ ] `Models/MediaModels.cs` - Remove `MediaAsset` and `MediaMetadata` classes (keep DTOs)
- [ ] `Data/MystiraAppDbContext.cs` - Update to use Domain model

#### Dependency Injection

- [ ] Update `Program.cs` in both API projects to register `IMediaAssetRepository` from `Infrastructure.Data` instead of `Api.Repositories`

## Pending ⏳

### Media Use Cases

Once references are updated, create the following use cases in `src/Mystira.App.Application/UseCases/Media/`:

- [ ] `GetMediaUseCase.cs` - Get media by ID
- [ ] `GetMediaByFilenameUseCase.cs` - Get media by filename (via metadata)
- [ ] `ListMediaUseCase.cs` - List media with filtering and pagination
- [ ] `UploadMediaUseCase.cs` - Upload media asset
- [ ] `UpdateMediaMetadataUseCase.cs` - Update media metadata
- [ ] `DeleteMediaUseCase.cs` - Delete media asset
- [ ] `DownloadMediaUseCase.cs` - Download media file

### Service Integration

- [ ] Update `MediaApiService` to use use cases instead of direct repository access
- [ ] Update `MediaUploadService` to use use cases
- [ ] Update `MediaQueryService` to use use cases

### Controller Integration

- [ ] Update `MediaController` to call use cases instead of services
- [ ] Update `MediaAdminController` to call use cases instead of services

## Notes

- `MediaMetadataFile`, `MediaMetadataEntry`, `CharacterMediaMetadataFile`, etc. remain in `Api.Models` as they are API-specific DTOs for file uploads/metadata management, not domain entities.
- `MediaQueryRequest`, `MediaQueryResponse`, `MediaUpdateRequest`, `BulkUploadResult`, etc. remain in `Api.Models` as they are API DTOs.
- The old `Api.Repositories.MediaAssetRepository` and `Api.Repositories.IMediaAssetRepository` should be removed once all references are updated.

## Migration Strategy

1. **Phase 1: Update References** (Current)
   - Update all `using` statements
   - Update repository registrations in DI
   - Remove duplicate `MediaAsset` classes from `Api.Models`

2. **Phase 2: Create Use Cases**
   - Implement all Media use cases
   - Register use cases in DI

3. **Phase 3: Integrate Use Cases**
   - Update services to use use cases
   - Update controllers to use use cases
   - Remove direct repository access from services/controllers

4. **Phase 4: Cleanup**
   - Remove old repository implementations from `Api.Repositories`
   - Remove unused service methods
   - Update tests
