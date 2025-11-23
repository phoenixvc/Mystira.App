# UserProfile Domain Model

## Overview

The `UserProfile` domain model represents an individual player profile within an account. Profiles support COPPA compliance, age-based content filtering, and badge tracking. Each account can have multiple profiles (e.g., for different children).

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/UserProfile.cs`

## Properties

| Property                 | Type                 | Description                                 |
| ------------------------ | -------------------- | ------------------------------------------- |
| `Id`                     | `string`             | Unique identifier (GUID)                    |
| `Name`                   | `string`             | Profile name                                |
| `PreferredFantasyThemes` | `List<FantasyTheme>` | Preferred fantasy themes                    |
| `DateOfBirth`            | `DateTime?`          | Date of birth (COPPA compliance)            |
| `IsGuest`                | `bool`               | Whether this is a temporary guest profile   |
| `IsNpc`                  | `bool`               | Whether this profile represents an NPC      |
| `AgeGroupName`           | `string`             | Age group name (stored as string)           |
| `AgeGroup`               | `AgeGroup`           | Age group object (convenience property)     |
| `AvatarMediaId`          | `string?`            | Avatar media ID                             |
| `SelectedAvatarMediaId`  | `string?`            | Selected avatar media ID                    |
| `CurrentAge`             | `int?`               | Calculated current age from date of birth   |
| `EarnedBadges`           | `List<UserBadge>`    | Badges earned by this profile               |
| `HasCompletedOnboarding` | `bool`               | Whether onboarding is complete              |
| `CreatedAt`              | `DateTime`           | Profile creation timestamp                  |
| `UpdatedAt`              | `DateTime`           | Last update timestamp                       |
| `AccountId`              | `string?`            | Link to parent Account                      |
| `Pronouns`               | `string?`            | Pronouns (e.g., they/them, she/her, he/him) |
| `Bio`                    | `string?`            | Bio or description                          |

## Methods

### `UpdateAgeGroupFromBirthDate()`

Updates the age group based on the current age calculated from date of birth.

**Returns**: `void`

### `GetAgeGroupFromBirthDate()`

Gets the appropriate age group based on current age.

**Returns**: `AgeGroup?` - Age group if age can be determined, null otherwise

### `GetBadgesForAxis(string axis)`

Gets badges earned for a specific compass axis.

**Parameters**:
- `axis` - The compass axis name

**Returns**: `List<UserBadge>` - List of badges for the axis, ordered by most recent

### `HasEarnedBadge(string badgeConfigurationId)`

Checks if a badge has already been earned.

**Parameters**:
- `badgeConfigurationId` - The badge configuration ID

**Returns**: `bool` - True if badge has been earned

### `AddEarnedBadge(UserBadge badge)`

Adds a new earned badge to the profile.

**Parameters**:
- `badge` - The badge to add

**Returns**: `void`

**Note**: Prevents duplicate badges for the same configuration

## Related Domain Models

### AgeGroup

Represents an age group classification using the `StringEnum<T>` pattern.

**Static Values**:

- `Toddlers` - Ages 1-3
- `Preschoolers` - Ages 4-5
- `School` - Ages 6-9
- `Preteens` - Ages 10-12
- `Teens` - Ages 13-18
- `Adults` - Ages 19+

**Properties**:

| Property     | Type     | Description                            |
| ------------ | -------- | -------------------------------------- |
| `Value`      | `string` | Age group identifier (from StringEnum) |
| `Name`       | `string` | Age group name                         |
| `MinimumAge` | `int`    | Minimum age for this group             |
| `MaximumAge` | `int`    | Maximum age for this group             |
| `AgeRange`   | `string` | Formatted age range (e.g., "6-9")      |

**Methods**:

- `IsAppropriateFor(int requiredMinimumAge)` - Checks if age group meets minimum age requirement
- `IsAppropriateFor(AgeGroup targetAgeGroup)` - Checks if age group is appropriate for target
- `IsContentAppropriate(string contentMinimumAgeGroup, string targetAgeGroup)` - Static method to check content appropriateness

## Relationships

- `UserProfile` → `Account` (via `AccountId`)
- `UserProfile` → `List<UserBadge>` (via `EarnedBadges`)
- `UserProfile` → `List<GameSession>` (via `ProfileId`)
- `UserProfile` → `List<FantasyTheme>` (via `PreferredFantasyThemes`)

## Use Cases

**Current Status**: ✅ Fully implemented with use cases

**Use Cases**:

- ✅ `CreateUserProfileUseCase` - Create new profile
- ✅ `GetUserProfileUseCase` - Get profile by ID
- ✅ `UpdateUserProfileUseCase` - Update profile
- ✅ `DeleteUserProfileUseCase` - Delete profile (COPPA compliant)

**Implementation**: Located in `Application.UseCases.UserProfiles`

## COPPA Compliance

- Date of birth is stored securely and used for age calculation
- Profiles can be deleted to comply with data deletion requests
- Guest profiles are temporary and not persisted long-term
- Age-based content filtering ensures appropriate content access

## Persistence

- Stored in Cosmos DB via `IUserProfileRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id` and `AccountId`
- Badges are stored as embedded documents

## Related Documentation

- [Account Domain Model](./account.md)
- [UserBadge Domain Model](./user-badge.md)
- [GameSession Domain Model](./game-session.md)
- [FantasyTheme Domain Model](./fantasy-theme.md)
- [Use Cases Documentation](../usecases/README.md)
