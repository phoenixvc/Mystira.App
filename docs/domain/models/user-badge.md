# UserBadge Domain Model

## Overview

The `UserBadge` domain model represents a badge earned by a user profile. Badges are permanent achievements tied to compass axis values and are awarded when players reach specific thresholds during gameplay.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/UserBadge.cs`

## Properties

| Property               | Type       | Description                                   |
| ---------------------- | ---------- | --------------------------------------------- |
| `Id`                   | `string`   | Unique identifier (GUID)                      |
| `UserProfileId`        | `string`   | ID of the user profile that earned the badge  |
| `BadgeConfigurationId` | `string`   | ID of the badge configuration                 |
| `BadgeName`            | `string`   | Badge name at time of earning (snapshot)      |
| `BadgeMessage`         | `string`   | Badge message at time of earning (snapshot)   |
| `Axis`                 | `string`   | Compass axis this badge was earned for        |
| `TriggerValue`         | `float`    | Compass value that triggered the badge        |
| `Threshold`            | `float`    | Threshold that was met to earn the badge      |
| `EarnedAt`             | `DateTime` | Timestamp when badge was earned               |
| `GameSessionId`        | `string?`  | Optional: Game session where badge was earned |
| `ScenarioId`           | `string?`  | Optional: Scenario where badge was earned     |
| `ImageId`              | `string`   | Image path for the badge                      |

## Badge System Architecture

### Profile-Level Badges (Permanent)

Badges are stored at the **profile level** and remain **permanent**:

- Badges are earned when compass values reach thresholds
- Badges are stored in `UserProfile.EarnedBadges`
- Badges persist across game sessions
- Badges represent long-term character development

### Session Achievements (Temporary)

Session achievements are separate from badges:

- Stored in `GameSession.Achievements`
- Temporary, session-specific accomplishments
- Not persisted beyond the session
- Represent "well done" moments in stories

## Relationships

- `UserBadge` → `UserProfile` (via `UserProfileId`)
- `UserBadge` → `BadgeConfiguration` (via `BadgeConfigurationId`)
- `UserBadge` → `GameSession` (via `GameSessionId`, optional)
- `UserBadge` → `Scenario` (via `ScenarioId`, optional)

## Use Cases

**Current Status**: ❌ No use cases (all operations in services)

**Use Cases** (Should be implemented):

- ❌ `AwardBadgeUseCase` - Award badge to user profile
- ❌ `GetUserBadgesUseCase` - Get badges for user profile
- ❌ `GetBadgeUseCase` - Get badge by ID
- ❌ `GetBadgesByAxisUseCase` - Get badges for specific axis
- ❌ `RevokeBadgeUseCase` - Revoke badge (admin only)

**Current Implementation**: `UserBadgeApiService` (should be refactored)

**Recommendation**: Create `Application.UseCases.Badges` directory and migrate service logic

## Badge Awarding Logic

Badges are awarded when:

1. Compass value reaches a threshold defined in `BadgeConfiguration`
2. Badge has not already been earned (`UserProfile.HasEarnedBadge()`)
3. Badge is added to profile (`UserProfile.AddEarnedBadge()`)

## Snapshot Pattern

Badge properties (`BadgeName`, `BadgeMessage`) are stored as snapshots at the time of earning:

- Ensures badge display remains consistent even if configuration changes
- Preserves historical context of achievement
- Prevents retroactive changes to earned badges

## Persistence

- Stored in Cosmos DB via `IUserBadgeRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id`, `UserProfileId`, and `BadgeConfigurationId`
- Can be queried by axis, profile, or session

## Related Documentation

- [BadgeConfiguration Domain Model](./badge-configuration.md)
- [UserProfile Domain Model](./user-profile.md)
- [GameSession Domain Model](./game-session.md)
- [Compass Domain Models](./compass.md)
- [Use Cases Documentation](../usecases/README.md)
