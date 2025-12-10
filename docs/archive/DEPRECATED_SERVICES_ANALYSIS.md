# Deprecated Services Analysis

## Summary
- **API Project**: All deprecated services are **NOT USED** by controllers. Controllers use IMediator (CQRS pattern). ✅ **SAFE TO DELETE**
- **Admin-API Project**: Deprecated services **ARE USED** by Admin controllers. ⚠️ **MIGRATION NEEDED**

## Services to DELETE from API Project (Not Used)

### Deprecated API Service Interfaces & Implementations:
1. `IAccountApiService` / `AccountApiService` - Controllers use CQRS commands/queries
2. `IScenarioApiService` / `ScenarioApiService` - Controllers use CQRS commands/queries
3. `IMediaApiService` / `MediaApiService` - Controllers use CQRS commands/queries
4. `IUserProfileApiService` / `UserProfileApiService` - Controllers use CQRS commands/queries
5. `IUserBadgeApiService` / `UserBadgeApiService` - Controllers use CQRS commands/queries
6. `ICharacterMapApiService` / `CharacterMapApiService` - Controllers use CQRS commands/queries
7. `IAvatarApiService` / `AvatarApiService` - Controllers use CQRS commands/queries
8. `ICharacterMediaMetadataService` / `CharacterMediaMetadataService` - Controllers use CQRS commands/queries
9. `ICharacterMapFileService` / `CharacterMapFileService` - Controllers use CQRS commands/queries
10. `IMediaMetadataService` (API version) / `MediaMetadataService` - Controllers use CQRS commands/queries
11. `IPasswordlessAuthService` / `PasswordlessAuthService` - Controllers use CQRS commands/queries

### Services That Depend on Deprecated Services (Also Not Used):
12. `IBundleService` / `BundleService` - Uses IScenarioApiService, IMediaApiService
13. `IClientApiService` / `ClientApiService` - Uses IScenarioApiService
14. `IClientStatusService` / `ClientStatusService` - Uses IScenarioApiService
15. `IMediaUploadService` / `MediaUploadService` - Uses IMediaMetadataService
16. `IMediaQueryService` / `MediaQueryService` - Uses IMediaMetadataService

## Files to Delete

### Interfaces:
- `src/Mystira.App.Api/Services/IAccountApiService.cs`
- `src/Mystira.App.Api/Services/IScenarioApiService.cs`
- `src/Mystira.App.Api/Services/IMediaApiService.cs`
- `src/Mystira.App.Api/Services/IUserProfileApiService.cs`
- `src/Mystira.App.Api/Services/IUserBadgeApiService.cs`
- `src/Mystira.App.Api/Services/ICharacterMapApiService.cs`
- `src/Mystira.App.Api/Services/IAvatarApiService.cs`
- `src/Mystira.App.Api/Services/ICharacterMediaMetadataService.cs`
- `src/Mystira.App.Api/Services/ICharacterMapFileService.cs`
- `src/Mystira.App.Api/Services/IMediaMetadataService.cs`
- `src/Mystira.App.Api/Services/IPasswordlessAuthService.cs`
- `src/Mystira.App.Api/Services/IBundleService.cs`
- `src/Mystira.App.Api/Services/IClientApiService.cs`
- `src/Mystira.App.Api/Services/IClientStatusService.cs`
- `src/Mystira.App.Api/Services/IMediaUploadService.cs`
- `src/Mystira.App.Api/Services/IMediaQueryService.cs`

### Implementations:
- `src/Mystira.App.Api/Services/AccountApiService.cs`
- `src/Mystira.App.Api/Services/ScenarioApiService.cs`
- `src/Mystira.App.Api/Services/MediaApiService.cs`
- `src/Mystira.App.Api/Services/UserProfileApiService.cs`
- `src/Mystira.App.Api/Services/UserBadgeApiService.cs`
- `src/Mystira.App.Api/Services/CharacterMapApiService.cs`
- `src/Mystira.App.Api/Services/AvatarApiService.cs`
- `src/Mystira.App.Api/Services/CharacterMediaMetadataService.cs`
- `src/Mystira.App.Api/Services/CharacterMapFileService.cs`
- `src/Mystira.App.Api/Services/MediaMetadataService.cs`
- `src/Mystira.App.Api/Services/PasswordlessAuthService.cs`
- `src/Mystira.App.Api/Services/BundleService.cs`
- `src/Mystira.App.Api/Services/ClientApiService.cs`
- `src/Mystira.App.Api/Services/ClientStatusService.cs`
- `src/Mystira.App.Api/Services/MediaUploadService.cs`
- `src/Mystira.App.Api/Services/MediaQueryService.cs`

### Adapters (if only used by deleted services):
- `src/Mystira.App.Api/Adapters/MediaMetadataServiceAdapter.cs` - Check if still needed

## Program.cs Changes

Remove these registrations from `src/Mystira.App.Api/Program.cs`:
- Lines 302-318: All deprecated service registrations

## Admin-API Project - Services in Use (Migration Recommendations)

### Services Currently Used by Admin Controllers:
1. **IScenarioApiService** - Used by:
   - `ScenariosController`
   - `ScenariosAdminController`
   - `AdminController`
   - **Recommendation**: Migrate to CQRS queries/commands (GetPaginatedScenariosQuery, CreateScenarioCommand, etc.)

2. **IAccountApiService** - Used by:
   - `UserProfilesAdminController`
   - `GameSessionsController`
   - **Recommendation**: Migrate to CQRS queries/commands (GetAccountQuery, GetAccountByEmailQuery, etc.)

3. **IMediaApiService** - Used by:
   - `MediaAdminController`
   - **Recommendation**: Migrate to CQRS queries/commands (GetMediaAssetQuery, etc.)

4. **IMediaMetadataService** - Used by:
   - `MediaMetadataAdminController`
   - `MediaAdminController`
   - `AdminController`
   - **Recommendation**: Migrate to CQRS queries/commands OR use Application.Ports.IMediaMetadataService (already has adapter)

5. **ICharacterMediaMetadataService** - Used by:
   - `CharacterMediaMetadataAdminController`
   - `AdminController`
   - **Recommendation**: Create CQRS commands/queries for character media metadata operations

6. **ICharacterMapApiService** - Used by:
   - `CharacterMapsAdminController`
   - `AdminController`
   - **Recommendation**: Migrate to CQRS queries/commands

7. **ICharacterMapFileService** - Used by:
   - `CharacterAdminController`
   - `AdminController`
   - **Recommendation**: Migrate to CQRS queries/commands

8. **IAvatarApiService** - Used by:
   - `AvatarAdminController`
   - **Recommendation**: Migrate to CQRS queries/commands

9. **IBundleService** - Used by:
   - `AdminController`
   - **Recommendation**: Migrate bundle upload logic to CQRS commands

10. **IUserProfileApiService** - Used by:
    - `UserProfilesAdminController`
    - **Recommendation**: Migrate to CQRS queries/commands

### Migration Strategy for Admin-API:
1. **Phase 1**: Create CQRS commands/queries for each deprecated service operation
2. **Phase 2**: Update Admin controllers one by one to use IMediator instead
3. **Phase 3**: Remove deprecated service registrations after all controllers migrated
4. **Phase 4**: Delete deprecated service files

## Verification
✅ API Controllers use IMediator (CQRS)
✅ API: No controllers inject deprecated services
✅ Admin-API: Controllers still use deprecated services - migration needed

