# User Profile Use Cases

This directory contains documentation for all user profile-related use cases.

## Overview

User profiles represent individual players within an account. These use cases handle profile lifecycle, validation, and COPPA-compliant deletion.

## Use Cases

### [Create User Profile](./create-user-profile.md)

Creates a new user profile:

- Validates profile doesn't already exist
- Validates fantasy themes (must be valid FantasyTheme values)
- Validates age group (must be valid AgeGroup)
- Auto-updates age group from date of birth if provided

**Flow**: Controller → Service → Use Case → Repository → Database

### [Get User Profile](./get-user-profile.md)

Retrieves a user profile by ID:

- Simple lookup by ID
- Returns null if not found (not an error)

**Flow**: Controller → Service → Use Case → Repository → Database

### [Update User Profile](./update-user-profile.md)

Updates an existing user profile:

- Validates fantasy themes if provided
- Validates age group if provided
- Auto-updates age group from date of birth if provided
- Updates timestamp

**Flow**: Controller → Service → Use Case → Repository → Database

### [Delete User Profile](./delete-user-profile.md)

Deletes a user profile and associated data (COPPA compliance):

- Deletes all associated game sessions
- Deletes the profile
- Transactional deletion

**Flow**: Controller → Service → Use Case → Repository → Database

## Key Concepts

### Profile Types

- **Regular Profile**: Standard user profile linked to an account
- **Guest Profile**: Temporary profile (`IsGuest = true`)
- **NPC Profile**: Non-player character (`IsNpc = true`)

### Age Group Management

- Age groups: `school` (6-9), `preteens` (10-12), `teens` (13-18)
- Can be set explicitly or calculated from date of birth
- Validated against `AgeGroupConstants.AllAgeGroups`

### Fantasy Themes

- Validated against `FantasyTheme` domain model
- Supports multiple themes per profile
- Themes parsed from strings to domain objects

### COPPA Compliance

When deleting a profile (especially for users under 13):

- All associated game sessions are deleted
- Profile data is permanently removed
- Ensures compliance with COPPA data deletion requirements

## Related Components

- **Domain Models**: `UserProfile`, `FantasyTheme`, `AgeGroup`
- **Repositories**: `IUserProfileRepository`, `IGameSessionRepository`
- **Services**: `UserProfileApiService`
- **DTOs**: `CreateUserProfileRequest`, `UpdateUserProfileRequest`, `UserProfileResponse`
