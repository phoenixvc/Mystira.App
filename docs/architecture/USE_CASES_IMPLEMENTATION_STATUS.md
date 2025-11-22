# Use Cases Implementation Status

## Summary

**Total Use Cases Implemented: 70** across 8 domain areas (excluding Media, which requires MediaAsset migration first)

## Completed Use Cases ✅

### GameSessions (13 use cases)

- ✅ CreateGameSessionUseCase
- ✅ GetGameSessionUseCase
- ✅ GetGameSessionsByAccountUseCase
- ✅ GetGameSessionsByProfileUseCase
- ✅ GetInProgressSessionsUseCase
- ✅ MakeChoiceUseCase
- ✅ ProgressSceneUseCase
- ✅ PauseGameSessionUseCase
- ✅ ResumeGameSessionUseCase
- ✅ EndGameSessionUseCase
- ✅ SelectCharacterUseCase
- ✅ GetSessionStatsUseCase
- ✅ CheckAchievementsUseCase
- ✅ DeleteGameSessionUseCase

### Accounts (10 use cases)

- ✅ GetAccountUseCase
- ✅ GetAccountByEmailUseCase
- ✅ CreateAccountUseCase
- ✅ UpdateAccountUseCase
- ✅ UpdateAccountSettingsUseCase
- ✅ UpdateSubscriptionUseCase
- ✅ AddUserProfileToAccountUseCase
- ✅ RemoveUserProfileFromAccountUseCase
- ✅ AddCompletedScenarioUseCase
- ✅ GetCompletedScenariosUseCase

### Authentication (5 use cases)

- ✅ CreatePendingSignupUseCase
- ✅ GetPendingSignupUseCase
- ✅ ValidatePendingSignupUseCase
- ✅ CompletePendingSignupUseCase
- ✅ ExpirePendingSignupUseCase

### CharacterMaps (7 use cases)

- ✅ GetCharacterMapsUseCase
- ✅ GetCharacterMapUseCase
- ✅ CreateCharacterMapUseCase
- ✅ UpdateCharacterMapUseCase
- ✅ DeleteCharacterMapUseCase
- ✅ ExportCharacterMapUseCase
- ✅ ImportCharacterMapUseCase

### Badges (5 use cases)

- ✅ AwardBadgeUseCase
- ✅ GetUserBadgesUseCase
- ✅ GetBadgeUseCase
- ✅ GetBadgesByAxisUseCase
- ✅ RevokeBadgeUseCase

### BadgeConfigurations (8 use cases)

- ✅ GetBadgeConfigurationsUseCase
- ✅ GetBadgeConfigurationUseCase
- ✅ GetBadgeConfigurationsByAxisUseCase
- ✅ CreateBadgeConfigurationUseCase
- ✅ UpdateBadgeConfigurationUseCase
- ✅ DeleteBadgeConfigurationUseCase
- ✅ ExportBadgeConfigurationUseCase
- ✅ ImportBadgeConfigurationUseCase

### Avatars (6 use cases)

- ✅ GetAvatarConfigurationsUseCase
- ✅ GetAvatarsByAgeGroupUseCase
- ✅ CreateAvatarConfigurationUseCase
- ✅ UpdateAvatarConfigurationUseCase
- ✅ DeleteAvatarConfigurationUseCase
- ✅ AssignAvatarToAgeGroupUseCase

### ContentBundles (9 use cases)

- ✅ GetContentBundlesUseCase
- ✅ GetContentBundleUseCase
- ✅ GetContentBundlesByAgeGroupUseCase
- ✅ CreateContentBundleUseCase
- ✅ UpdateContentBundleUseCase
- ✅ DeleteContentBundleUseCase
- ✅ AddScenarioToBundleUseCase
- ✅ RemoveScenarioFromBundleUseCase
- ✅ CheckBundleAccessUseCase

### Scenarios (5 use cases) - Pre-existing

- ✅ CreateScenarioUseCase
- ✅ GetScenariosUseCase
- ✅ UpdateScenarioUseCase
- ✅ DeleteScenarioUseCase
- ✅ ValidateScenarioUseCase

### UserProfiles (4 use cases) - Pre-existing

- ✅ CreateUserProfileUseCase
- ✅ GetUserProfileUseCase
- ✅ UpdateUserProfileUseCase
- ✅ DeleteUserProfileUseCase

## Pending ⏳

### Media (7 use cases) - Blocked by MediaAsset Migration

- ⏳ GetMediaUseCase
- ⏳ GetMediaByFilenameUseCase
- ⏳ ListMediaUseCase
- ⏳ UploadMediaUseCase
- ⏳ UpdateMediaMetadataUseCase
- ⏳ DeleteMediaUseCase
- ⏳ DownloadMediaUseCase

**Blocking Issue**: `MediaAsset` needs to be fully migrated from `Api.Models` to `Domain.Models` before use cases can be implemented.

See `docs/architecture/MEDIA_ASSET_MIGRATION_STATUS.md` for migration progress.

## Integration Status

### Dependency Injection Registration

- ⏳ Use cases need to be registered in `Program.cs` for both API projects
- ⏳ Current services still use repositories directly

### Service Integration

- ⏳ Services need to be updated to call use cases instead of repositories
- ⏳ Services can be kept as thin facades or removed entirely

### Controller Integration

- ⏳ Controllers need to be updated to call use cases instead of services
- ⏳ Controllers should inject use cases directly

## Next Steps

1. **Complete MediaAsset Migration** (See `MEDIA_ASSET_MIGRATION_STATUS.md`)
   - Update all references from `Api.Models.MediaAsset` to `Domain.Models.MediaAsset`
   - Update repository registrations in DI
   - Create Media use cases

2. **Register Use Cases in DI**
   - Add use case registrations to `Program.cs` in both API projects
   - Use appropriate lifetime (typically Scoped)

3. **Update Services**
   - Refactor services to call use cases
   - Keep services as thin facades or remove if controllers call use cases directly

4. **Update Controllers**
   - Inject use cases instead of services
   - Update controller methods to call use cases
   - Map use case results to DTOs/Responses

5. **Testing**
   - Update unit tests to test use cases
   - Update integration tests if needed
   - Verify all documented use cases work end-to-end

## Architecture Notes

- All use cases follow the pattern: `ExecuteAsync(request)` returning domain models or DTOs
- Use cases are in `Mystira.App.Application` layer
- Use cases depend on repositories, unit of work, and infrastructure services
- Controllers should map use case results to API responses
- Services can be kept for backward compatibility or removed if not needed
