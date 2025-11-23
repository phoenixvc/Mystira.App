# Compass Domain Models

## Overview

Compass models represent moral/ethical value tracking in the Mystira application. Compass values track player choices and their impact on various ethical axes.

## Domain Models

### CompassChange

**Location**: `Domain.Models.Scenario.CompassChange`

**Type**: Value Object

**Purpose**: Represents a single change to a compass axis value.

**Properties**:

- `Axis` - Compass axis name (e.g., "honesty", "kindness", "bravery")
- `Delta` - Change value (-1.0 to 1.0)
- `DevelopmentalLink` - Optional description linking to developmental concepts

**Usage**: Created when a player makes a choice that affects compass values.

### CompassTracking

**Location**: `Domain.Models.Scenario.CompassTracking`

**Type**: Entity/Value Object (embedded in GameSession)

**Purpose**: Tracks the current state and history of a compass axis within a game session.

**Properties**:

- `Axis` - Compass axis name
- `CurrentValue` - Current compass value (-2.0 to 2.0, clamped)
- `StartingValue` - Starting value (typically 0.0)
- `History` - List of CompassChange objects
- `LastUpdated` - Last update timestamp

**Usage**: Embedded in `GameSession.CompassValues` dictionary, one per axis.

## Current Architecture

### Association: GameSession

**Current Implementation**: Compass values are tracked **per GameSession**, not per UserProfile or Account.

**Rationale**:

- Each game session represents a distinct playthrough
- Compass values reset for each new session
- Values are specific to the choices made in that session
- Badges are awarded based on session-specific compass thresholds

**Storage**:

- `GameSession.CompassValues` - `Dictionary<string, CompassTracking>`
- Key: Compass axis name (e.g., "honesty")
- Value: CompassTracking object for that axis
- Stored as embedded document in Cosmos DB (not a separate entity)

### Initialization

Compass tracking is initialized when a game session is created:

1. Scenario defines `CoreAxes` (e.g., ["honesty", "kindness", "bravery"])
2. For each axis, a `CompassTracking` object is created with:
   - `CurrentValue = 0.0`
   - `StartingValue = 0.0`
   - `History = []`
   - `LastUpdated = Now`

### Updates

Compass values are updated when players make choices:

1. Choice contains `CompassChange` (optional)
2. If present, update `CompassTracking`:
   - `CurrentValue += CompassChange.Delta`
   - Clamp to [-2.0, 2.0]
   - Add `CompassChange` to `History`
   - Update `LastUpdated`

## Should Compass Be Associated with UserProfile?

### Current Design: Session-Scoped

**Pros**:

- ✅ Each session is independent
- ✅ Players can explore different moral paths in different sessions
- ✅ Simpler data model
- ✅ Badges are session-specific achievements

**Cons**:

- ❌ No aggregate view of player's overall moral development
- ❌ Cannot track progress across multiple sessions
- ❌ Cannot show "lifetime" compass values

### Alternative Design: Profile-Scoped

**Pros**:

- ✅ Aggregate compass values across all sessions
- ✅ Show overall moral development
- ✅ Enable "lifetime" badges
- ✅ Better analytics and insights

**Cons**:

- ❌ More complex data model
- ❌ Need to aggregate/merge compass values from multiple sessions
- ❌ May conflict with session-specific gameplay
- ❌ Harder to reset or start fresh

### Recommendation

**Keep Compass Session-Scoped** for the following reasons:

1. **Game Design**: Each session is a self-contained story experience
2. **Simplicity**: Current design is clear and maintainable
3. **Badge System**: Badges are already session-specific
4. **Privacy**: Session-scoped data aligns with COPPA compliance

**Future Enhancement**: If aggregate compass values are needed:

- Create a separate `UserProfileCompassValues` aggregate
- Calculate on-demand from completed sessions
- Store as cached/denormalized data
- Update when sessions complete

## Use Cases

Compass-related operations are handled through game session use cases:

- `CreateGameSessionUseCase` - Initializes compass tracking
- `MakeChoiceUseCase` - Updates compass values
- `GetSessionStatsUseCase` - Retrieves compass values for display

## Related Domain Models

- `GameSession` - Contains compass values
- `Scenario` - Defines core axes for compass tracking
- `Branch` - Contains CompassChange (optional)
- `SessionAchievement` - Awards based on compass thresholds
- `UserBadge` - Badges earned when compass thresholds are reached

## Related Documentation

- [Game Session Domain Model](./game-session.md)
- [Scenario Domain Model](./scenario.md)
- [Make Choice Use Case](../../usecases/gamesessions/make-choice.md)
