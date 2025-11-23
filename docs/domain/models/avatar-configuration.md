# AvatarConfiguration Domain Model

## Overview

The `AvatarConfiguration` domain model manages avatar assignments by age group. Avatars are media assets that can be selected by user profiles based on their age group.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/AvatarConfiguration.cs`

## Properties

| Property         | Type           | Description                                     |
| ---------------- | -------------- | ----------------------------------------------- |
| `Id`             | `string`       | Unique identifier (GUID)                        |
| `AgeGroup`       | `string`       | Age group name                                  |
| `AvatarMediaIds` | `List<string>` | List of media IDs for avatars in this age group |
| `CreatedAt`      | `DateTime`     | Creation timestamp                              |
| `UpdatedAt`      | `DateTime`     | Last update timestamp                           |

## Related Domain Models

### AvatarConfigurationFile

Represents the avatar mapping file stored in blob storage. This is a single file that contains all age group avatar mappings.

**Properties**:

| Property          | Type                               | Description                          |
| ----------------- | ---------------------------------- | ------------------------------------ |
| `Id`              | `string`                           | Fixed ID: "avatar-configuration"     |
| `AgeGroupAvatars` | `Dictionary<string, List<string>>` | Map of age group to avatar media IDs |
| `CreatedAt`       | `DateTime`                         | Creation timestamp                   |
| `UpdatedAt`       | `DateTime`                         | Last update timestamp                |
| `Version`         | `string`                           | File version (default: "1.0")        |

## Relationships

- `AvatarConfiguration` → `AgeGroup` (via `AgeGroup`)
- `AvatarConfiguration` → `MediaAsset` (via `AvatarMediaIds`)
- `AvatarConfiguration` → `UserProfile` (via avatar selection)

## Use Cases

**Current Status**: ❌ No use cases (all operations in services)

**Use Cases** (Should be implemented):

- ❌ `GetAvatarConfigurationsUseCase` - Get all avatar configurations
- ❌ `GetAvatarsByAgeGroupUseCase` - Get avatars for age group
- ❌ `CreateAvatarConfigurationUseCase` - Create new configuration
- ❌ `UpdateAvatarConfigurationUseCase` - Update configuration
- ❌ `DeleteAvatarConfigurationUseCase` - Delete configuration
- ❌ `AssignAvatarToAgeGroupUseCase` - Assign avatar to age group

**Current Implementation**: `AvatarApiService` (should be refactored)

**Recommendation**: Create `Application.UseCases.Avatars` directory

## Avatar Selection Flow

1. User profile has an `AgeGroup` assigned
2. System queries `AvatarConfiguration` for that age group
3. Available avatars (`AvatarMediaIds`) are presented to user
4. User selects avatar, stored in `UserProfile.SelectedAvatarMediaId`

## Persistence

- Stored in Cosmos DB via `IAvatarConfigurationRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id` and `AgeGroup`
- Can also be stored as a single file (`AvatarConfigurationFile`) in blob storage

## Related Documentation

- [UserProfile Domain Model](./user-profile.md)
- [AgeGroup](./user-profile.md#agegroup)
- [Use Cases Documentation](../usecases/README.md)
