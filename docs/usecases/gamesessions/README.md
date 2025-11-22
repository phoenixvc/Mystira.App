# Game Session Use Cases

This directory contains documentation for all game session-related use cases.

## Overview

Game sessions represent active playthroughs of scenarios. These use cases handle session lifecycle, choice processing, compass tracking, echo generation, and achievement checking.

## Use Cases

### [Create Game Session](./create-game-session.md)

Starts a new game session:

- Validates scenario exists and age compatibility
- Handles existing active sessions (auto-complete or pause)
- Initializes compass tracking for scenario axes
- Sets initial scene

**Flow**: Controller → Service → Use Case → Repository → Database

### [Make Choice](./make-choice.md)

Processes a player's choice in a game session:

- Validates session and scene
- Records choice in history
- Processes echo logs
- Updates compass values
- Checks for session completion
- Triggers badge checking (via service layer)

**Flow**: Controller → Service → Use Case → Repository → Service (Badge Check) → Database

### [Progress Scene](./progress-scene.md)

Progresses to a specific scene in a session:

- Validates target scene exists
- Updates current scene
- Handles pause/resume logic
- Updates elapsed time

**Flow**: Controller → Service → Use Case → Repository → Database

### [Resume Game Session](./resume-game-session.md)

Resumes a paused game session:

- Validates session is paused
- Changes status to InProgress
- Clears pause flags

**Flow**: Controller → Service → Repository → Database

**Status**: Not currently in production

### [Assign Player to Character](./assign-character.md)

Assigns a player (profile) to a character in a scenario:

- Links character ID to session
- Enables text replacement with player names

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production

### [End Game Session](./end-game-session.md)

Manually ends a game session:

- Marks session as completed
- Sets end time and calculates elapsed time
- Clears pause flags

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production

### [Pause Game Session](./pause-game-session.md)

Pauses an active game session:

- Validates session is in progress
- Changes status to Paused
- Sets pause flags and timestamp

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production (should be migrated to use case)

### [Get Game Session](./get-game-session.md)

Retrieves a game session by ID:

- Simple lookup by ID
- Returns null if not found

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production (should be migrated to use case)

### [Get Sessions by Account](./get-sessions-by-account.md)

Retrieves all game sessions for an account:

- Queries sessions by account ID
- Maps to DTOs with statistics

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production (should be migrated to use case)

### [Get Sessions by Profile](./get-sessions-by-profile.md)

Retrieves all game sessions for a user profile:

- Queries sessions by profile ID
- Maps to DTOs with statistics

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production (should be migrated to use case)

### [Get In Progress Sessions](./get-in-progress-sessions.md)

Retrieves active (in-progress or paused) sessions for an account:

- Filters by account ID and status
- Used for "Active Adventures" display

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production (should be migrated to use case)

### [Get Session Stats](./get-session-stats.md)

Retrieves statistics for a game session:

- Compass values
- Recent echoes
- Achievements
- Choice counts and duration

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production (should be migrated to use case)

### [Check Achievements](./check-achievements.md)

Checks if session qualifies for achievements:

- Compass threshold achievements
- First choice achievement
- Session completion achievement

**Flow**: Controller → Service → Repository → BadgeConfig → Database

**Status**: Currently in production (should be migrated to use case)

### [Delete Game Session](./delete-game-session.md)

Deletes a game session:

- Validates session exists
- Deletes session
- Transactional deletion

**Flow**: Controller → Service → Repository → Database

**Status**: Currently in production (should be migrated to use case)

## Key Concepts

### Compass Tracking

Each session maintains compass values for all scenario core axes:

- Initialized at session creation
- Updated when choices have compass changes
- Values clamped to [-2.0, 2.0] range
- History tracked for each axis

### Echo Logs

Echo logs are generated when choices trigger them:

- Stored in session EchoHistory
- Include echo type, description, strength, timestamp
- Used for echo reveal mechanics

### Session Status

- **NotStarted**: Session created but not yet started
- **InProgress**: Active session
- **Paused**: Temporarily paused
- **Completed**: Finished scenario
- **Abandoned**: User abandoned session

### Badge Integration

Badge checking happens in the service layer after use case execution:

- Checks compass thresholds against badge configurations
- Awards badges via `UserBadgeApiService`
- Links badges to game sessions and scenarios

## Related Components

- **Domain Models**: `GameSession`, `SessionChoice`, `CompassTracking`, `EchoLog`, `SessionAchievement`
- **Repositories**: `IGameSessionRepository`, `IScenarioRepository`
- **Services**: `GameSessionApiService`, `UserBadgeApiService`, `BadgeConfigurationApiService`
- **DTOs**: `StartGameSessionRequest`, `MakeChoiceRequest`, `ProgressSceneRequest`, `GameSessionResponse`
