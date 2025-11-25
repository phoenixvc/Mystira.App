# Architectural Refactoring Plan: Service Layer Migration

**Created:** November 25, 2025
**Priority:** HIGH (PERF-4)
**Effort:** Medium (3-5 days)
**Impact:** Architectural compliance, improved testability, better maintainability

---

## Executive Summary

The API layer currently contains 20+ service classes that violate hexagonal architecture principles. According to hexagonal architecture, the API layer should be a **thin adapter** that only handles HTTP concerns and delegates to the Application layer via **MediatR** (CQRS pattern).

**Current State:**
- ❌ 20+ services in API layer (`Mystira.App.Api/Services/`)
- ❌ Controllers inject both IMediator AND services
- ❌ Mixed patterns (some use MediatR, some use services)
- ❌ Duplication (services delegate to use cases which delegate to CQRS handlers)

**Target State:**
- ✅ ZERO services in API layer (except infrastructure adapters like JwtService, EmailService)
- ✅ Controllers ONLY inject IMediator
- ✅ All business operations go through CQRS (commands/queries)
- ✅ Consistent pattern across all controllers

---

## Problem Analysis

### Architectural Violation

**Hexagonal Architecture Principle:**
```
External Clients
    ↓ HTTP
API Controllers (thin adapter)
    ↓ IMediator.Send()
CQRS Handlers (Application layer)
    ↓ uses ports
Domain Logic & Infrastructure
```

**Current Reality:**
```
External Clients
    ↓ HTTP
API Controllers
    ↓ sometimes IMediator
    ↓ sometimes Services ❌ VIOLATION
        ↓ delegates to UseCases
            ↓ delegates to CQRS
                ↓ uses ports
Domain Logic & Infrastructure
```

### Issues with Current Approach

1. **Extra Layer of Indirection**: Controllers → Services → UseCases → CQRS Handlers
   - Should be: Controllers → CQRS Handlers

2. **Inconsistent Patterns**: Some controllers use MediatR, others use services
   - Example: MediaController uses both IMediator AND IMediaApiService

3. **Violates Hexagonal Architecture**: Services in the API layer contain coordination logic
   - API layer should be a thin HTTP adapter only

4. **Poor Testability**: Tests must mock both services and MediatR
   - Should only mock MediatR (or use in-memory handlers)

5. **Difficult Maintenance**: Three layers to update for one feature
   - Controller → Service → UseCase → CQRS Handler

---

## Service Categories

### Category 1: Thin Wrappers (DELETE)
These services only delegate to CQRS handlers with no additional logic.

**Services:**
- `MediaApiService` - Delegates to Media CQRS
- `ScenarioApiService` - Delegates to Scenario CQRS
- `UserProfileApiService` - Delegates to UserProfile CQRS
- `AccountApiService` - Delegates to Account CQRS
- `UserBadgeApiService` - Delegates to UserBadge CQRS
- `AvatarApiService` - Delegates to Avatar CQRS
- `CharacterMapApiService` - Delegates to CharacterMap CQRS
- `BundleService` - Delegates to Bundle CQRS

**Action:** DELETE these services entirely. Controllers call MediatR directly.

---

### Category 2: Infrastructure Adapters (MOVE to Infrastructure)
These services handle cross-cutting infrastructure concerns.

**Services:**
- `JwtService` - Token generation/validation
- `AzureEmailService` - Email sending via Azure
- `PasswordlessAuthService` - Passwordless authentication flow
- `AuthService` - Authentication coordination

**Action:** MOVE to `Mystira.App.Infrastructure.Authentication/` or similar.
- These are legitimate infrastructure services
- Should be injected via ports in Application layer
- API layer should not contain them

---

### Category 3: Query Coordination (CONVERT to Queries)
These services coordinate multiple queries or add query logic.

**Services:**
- `MediaQueryService` - Media validation and statistics
- `MediaMetadataService` - Media metadata lookups
- `CharacterMediaMetadataService` - Character media lookups
- `AppStatusService` - Application health status
- `ClientStatusService` - Client connection status

**Action:** CONVERT to CQRS Queries in Application layer.
- `ValidateMediaReferencesQuery`
- `GetMediaUsageStatsQuery`
- `GetCharacterMediaMetadataQuery`
- `GetAppStatusQuery`

---

### Category 4: File Processing (CONVERT to Commands)
These services handle file uploads and processing.

**Services:**
- `MediaUploadService` - Media file uploads
- `CharacterMapFileService` - Character map file processing

**Action:** CONVERT to CQRS Commands in Application layer.
- `UploadMediaCommand`
- `BulkUploadMediaCommand`
- `ProcessCharacterMapFileCommand`

---

## Refactoring Strategy

### Phase 1: Create Missing CQRS Handlers (This Phase)

Create CQRS commands/queries to replace service logic:

**New Queries Needed:**
```
Application/CQRS/Media/
├── Queries/
│   ├── ValidateMediaReferencesQuery.cs
│   ├── ValidateMediaReferencesQueryHandler.cs
│   ├── GetMediaUsageStatsQuery.cs
│   ├── GetMediaUsageStatsQueryHandler.cs
│   ├── GetMediaFileQuery.cs (download)
│   └── GetMediaFileQueryHandler.cs

Application/CQRS/Scenarios/
├── Queries/
│   ├── GetScenariosByAgeGroupQuery.cs
│   └── GetScenariosByAgeGroupQueryHandler.cs

Application/CQRS/System/
├── Queries/
│   ├── GetAppStatusQuery.cs
│   └── GetAppStatusQueryHandler.cs
```

**New Commands Needed:**
```
Application/CQRS/Media/
├── Commands/
│   ├── UploadMediaCommand.cs
│   ├── UploadMediaCommandHandler.cs
│   ├── BulkUploadMediaCommand.cs
│   └── BulkUploadMediaCommandHandler.cs

Application/CQRS/Auth/
├── Commands/
│   ├── InitiatePasswordlessAuthCommand.cs
│   ├── VerifyPasswordlessAuthCommand.cs
│   └── GenerateJwtTokenCommand.cs
```

---

### Phase 2: Update Controllers (Next Phase)

Update controllers to use IMediator instead of services:

**Before:**
```csharp
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMediaApiService _mediaService; // ❌ Remove

    [HttpGet("{mediaId}")]
    public async Task<IActionResult> GetMediaFile(string mediaId)
    {
        var result = await _mediaService.GetMediaFileAsync(mediaId); // ❌ Old
        // ...
    }
}
```

**After:**
```csharp
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator; // ✅ Only dependency

    [HttpGet("{mediaId}")]
    public async Task<IActionResult> GetMediaFile(string mediaId)
    {
        var query = new GetMediaFileQuery(mediaId); // ✅ New
        var result = await _mediator.Send(query);
        // ...
    }
}
```

---

### Phase 3: Deprecate Services (Final Phase)

1. Mark services as `[Obsolete]`
2. Remove service registrations from `Program.cs`
3. Delete service files after all controllers updated
4. Update tests to use CQRS handlers directly

---

## CQRS Pattern Reference

### Query Example
```csharp
// Query (Application/CQRS/Media/Queries/GetMediaFileQuery.cs)
public record GetMediaFileQuery(string MediaId)
    : IQuery<(Stream stream, string contentType, string fileName)?>;

// Handler (Application/CQRS/Media/Queries/GetMediaFileQueryHandler.cs)
public class GetMediaFileQueryHandler
    : IQueryHandler<GetMediaFileQuery, (Stream, string, string)?>
{
    private readonly IMediaAssetRepository _repository;
    private readonly IBlobService _blobService; // Infrastructure port

    public async Task<(Stream, string, string)?> Handle(
        GetMediaFileQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Get media metadata from repository
        var media = await _repository.GetByIdAsync(query.MediaId);
        if (media == null) return null;

        // 2. Download file from blob storage
        var stream = await _blobService.DownloadAsync(media.BlobName);

        return (stream, media.MimeType, media.FileName);
    }
}
```

### Command Example
```csharp
// Command (Application/CQRS/Media/Commands/UploadMediaCommand.cs)
public record UploadMediaCommand(
    IFormFile File,
    string MediaId,
    string MediaType,
    string? Description = null,
    List<string>? Tags = null
) : ICommand<MediaAsset>;

// Handler (Application/CQRS/Media/Commands/UploadMediaCommandHandler.cs)
public class UploadMediaCommandHandler
    : ICommandHandler<UploadMediaCommand, MediaAsset>
{
    private readonly IMediaAssetRepository _repository;
    private readonly IBlobService _blobService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<MediaAsset> Handle(
        UploadMediaCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate file
        if (command.File.Length == 0)
            throw new ArgumentException("File is empty");

        // 2. Upload to blob storage
        var blobName = await _blobService.UploadAsync(
            command.File.OpenReadStream(),
            command.File.FileName);

        // 3. Create media asset entity
        var media = new MediaAsset
        {
            Id = command.MediaId,
            MediaType = command.MediaType,
            BlobName = blobName,
            FileName = command.File.FileName,
            MimeType = command.File.ContentType,
            Description = command.Description,
            Tags = command.Tags ?? new()
        };

        // 4. Save to database
        await _repository.AddAsync(media);
        await _unitOfWork.CommitAsync();

        return media;
    }
}
```

### Controller Usage
```csharp
[HttpPost("upload")]
public async Task<ActionResult<MediaAsset>> UploadMedia(
    IFormFile file,
    [FromForm] string mediaId,
    [FromForm] string mediaType)
{
    try
    {
        var command = new UploadMediaCommand(file, mediaId, mediaType);
        var media = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetMediaById),
            new { mediaId = media.Id },
            media);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new ErrorResponse { Message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error uploading media");
        return StatusCode(500, new ErrorResponse
        {
            Message = "Failed to upload media"
        });
    }
}
```

---

## Migration Checklist

### ✅ Phase 1: CQRS Handlers (Current)
- [ ] Create `GetMediaFileQuery` + Handler
- [ ] Create `ValidateMediaReferencesQuery` + Handler
- [ ] Create `GetMediaUsageStatsQuery` + Handler
- [ ] Create `UploadMediaCommand` + Handler
- [ ] Create `BulkUploadMediaCommand` + Handler
- [ ] Create `GetScenariosByAgeGroupQuery` + Handler
- [ ] Create `GetAppStatusQuery` + Handler
- [ ] Create auth-related commands (JWT, passwordless)

### ⏳ Phase 2: Controller Updates (Next)
- [ ] Update MediaController to use only IMediator
- [ ] Update ScenariosController to use only IMediator
- [ ] Update AccountsController to use only IMediator
- [ ] Update UserProfilesController to use only IMediator
- [ ] Update all other controllers
- [ ] Remove service constructor parameters

### ⏳ Phase 3: Service Cleanup (Final)
- [ ] Mark all API services as `[Obsolete]`
- [ ] Remove service registrations from Program.cs
- [ ] Delete service files
- [ ] Update controller tests to mock IMediator only
- [ ] Move infrastructure services to Infrastructure layer

---

## Benefits

### Before (Current)
```
Controller → Service → UseCase → CQRS Handler → Repository
```
- 4 layers of indirection
- Inconsistent patterns
- Difficult to test
- Violates hexagonal architecture

### After (Target)
```
Controller → CQRS Handler → Repository
```
- 2 layers (clean)
- Consistent MediatR pattern
- Easy to test (mock IMediator)
- Compliant hexagonal architecture

---

## Testing Impact

### Before
```csharp
// Must mock both service and MediatR
var mediatorMock = new Mock<IMediator>();
var serviceMock = new Mock<IMediaApiService>();
var controller = new MediaController(mediatorMock.Object, serviceMock.Object, logger);
```

### After
```csharp
// Only mock MediatR
var mediatorMock = new Mock<IMediator>();
var controller = new MediaController(mediatorMock.Object, logger);
```

---

## Risks & Mitigation

### Risk 1: Breaking Changes
**Risk:** Removing services might break existing clients
**Mitigation:**
- Keep API contracts identical (same endpoints, same responses)
- Only internal implementation changes
- Run full integration test suite

### Risk 2: Missing Functionality
**Risk:** Services might have hidden logic not captured in CQRS
**Mitigation:**
- Carefully review each service before deleting
- Create comprehensive CQRS handlers
- Add tests for all migrated logic

### Risk 3: Performance Degradation
**Risk:** MediatR might add overhead
**Mitigation:**
- MediatR is already used in existing controllers
- Benchmark critical endpoints
- Use caching in query handlers (already implemented)

---

## Success Criteria

✅ **Architecture Compliance**
- Zero services in API layer (except infrastructure adapters)
- All controllers use IMediator only
- Clear separation of concerns

✅ **Test Improvements**
- Controller tests only mock IMediator
- Integration tests use in-memory handlers
- Test coverage maintained or improved

✅ **Code Quality**
- Reduced complexity (fewer layers)
- Consistent patterns (all CQRS)
- Better maintainability

✅ **Functionality Preserved**
- All API endpoints work identically
- No breaking changes
- Integration tests pass

---

## Timeline

| Phase | Duration | Description |
|-------|----------|-------------|
| **Phase 1** | 2 days | Create missing CQRS handlers |
| **Phase 2** | 1 day | Update controllers to use MediatR |
| **Phase 3** | 1 day | Delete services and update tests |
| **Testing** | 1 day | Full integration testing |
| **Total** | 5 days | Complete refactoring |

---

## Next Actions

**Immediate (Today):**
1. ✅ Create this refactoring plan
2. ⏳ Create GetMediaFileQuery + Handler
3. ⏳ Update MediaController.GetMediaFile to use query
4. ⏳ Test the pattern works

**Week 1:**
- Complete all missing CQRS handlers
- Update 50% of controllers

**Week 2:**
- Update remaining controllers
- Delete deprecated services
- Full test suite verification

---

**Related:**
- Production Review: PERF-4
- Testing Strategy: `docs/TESTING_STRATEGY.md`
- Hexagonal Architecture: `src/Mystira.App.Api/README.md`
