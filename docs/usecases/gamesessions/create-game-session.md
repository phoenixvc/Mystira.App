# Create Game Session Use Case

## Overview

The `CreateGameSessionUseCase` handles starting a new game session, including validation, existing session management, and compass initialization.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.GameSessions.CreateGameSessionUseCase`

**Input**: `StartGameSessionRequest`

**Output**: `GameSession` (domain model)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as GameSessionsController
    participant Service as GameSessionApiService
    participant UseCase as CreateGameSessionUseCase
    participant ScenarioRepo as IScenarioRepository
    participant SessionRepo as IGameSessionRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: POST /api/gamesessions<br/>(StartGameSessionRequest)
    Controller->>Service: StartGameSessionAsync(request)
    
    Service->>UseCase: ExecuteAsync(request)
    
    Note over UseCase: Step 1: Validate Scenario
    UseCase->>ScenarioRepo: GetByIdAsync(scenarioId)
    ScenarioRepo->>DB: Query scenario
    alt Scenario Not Found
        DB-->>ScenarioRepo: null
        ScenarioRepo-->>UseCase: null
        UseCase-->>Service: ArgumentException("Scenario not found")
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    DB-->>ScenarioRepo: Scenario
    ScenarioRepo-->>UseCase: Scenario
    
    Note over UseCase: Step 2: Validate Age Compatibility
    UseCase->>UseCase: IsAgeGroupCompatible(<br/>  scenario.MinimumAge,<br/>  request.TargetAgeGroup<br/>)
    alt Age Incompatible
        UseCase-->>Service: ArgumentException("Age incompatible")
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    
    Note over UseCase: Step 3: Handle Existing Sessions
    UseCase->>SessionRepo: GetActiveSessionsByScenarioAndAccountAsync(<br/>  scenarioId, accountId<br/>)
    SessionRepo->>DB: Query active sessions
    DB-->>SessionRepo: List<GameSession>
    SessionRepo-->>UseCase: existingActiveSessions
    
    alt Existing Active Sessions Found
        loop For each existing session
            UseCase->>UseCase: Mark as Completed<br/>(Status = Completed,<br/>EndTime = Now)
            UseCase->>SessionRepo: UpdateAsync(existingSession)
            SessionRepo->>DB: Update entity
        end
        UseCase->>UoW: SaveChangesAsync()
        UoW->>DB: Commit transaction
        DB-->>UoW: Success
        UoW-->>UseCase: Success
    end
    
    alt Existing InProgress Session Found
        UseCase->>UseCase: Pause existing session<br/>(Status = Paused,<br/>IsPaused = true,<br/>PausedAt = Now)
        UseCase->>SessionRepo: UpdateAsync(pausedSession)
        SessionRepo->>DB: Update entity
        UseCase->>UoW: SaveChangesAsync()
        UoW->>DB: Commit transaction
    end
    
    Note over UseCase: Step 4: Create New Session
    UseCase->>UseCase: new GameSession {<br/>  Id = Guid.NewGuid(),<br/>  ScenarioId, AccountId,<br/>  ProfileId, PlayerNames,<br/>  Status = InProgress,<br/>  CurrentSceneId = first scene,<br/>  StartTime = Now,<br/>  TargetAgeGroupName,<br/>  SceneCount<br/>}
    
    Note over UseCase: Step 5: Initialize Compass Tracking
    loop For each core axis in scenario
        UseCase->>UseCase: session.CompassValues[axis] =<br/>  new CompassTracking {<br/>    Axis = axis.Value,<br/>    CurrentValue = 0.0,<br/>    History = [],<br/>    LastUpdated = Now<br/>  }
    end
    
    Note over UseCase: Step 6: Persist Session
    UseCase->>SessionRepo: AddAsync(session)
    SessionRepo->>DB: Add entity to DbSet
    UseCase->>UoW: SaveChangesAsync()
    UoW->>DB: Commit transaction
    DB-->>UoW: Success
    UoW-->>UseCase: Success
    SessionRepo-->>UseCase: GameSession (with ID)
    
    UseCase-->>Service: GameSession
    Service-->>Controller: GameSession
    Controller-->>Client: 201 Created<br/>(GameSession)
```

## Age Compatibility Validation

The use case validates that the scenario's minimum age is compatible with the target age group:

```mermaid
sequenceDiagram
    participant UseCase
    participant AgeGroup as AgeGroup Domain

    UseCase->>AgeGroup: Parse(targetAgeGroup)
    AgeGroup-->>UseCase: AgeGroup object
    
    UseCase->>UseCase: Compare:<br/>scenario.MinimumAge <=<br/>targetAgeGroup.MinimumAge
    
    alt Compatible
        UseCase-->>UseCase: Continue
    else Incompatible
        UseCase-->>UseCase: Throw ArgumentException
    end
```

## Existing Session Handling

The use case handles multiple scenarios for existing sessions:

1. **Auto-Complete**: Any existing active sessions for the same scenario and account are marked as completed
2. **Pause**: If an InProgress session exists, it's paused before creating a new one
3. **New Session**: A fresh session is created with initial state

## Compass Initialization

For each core axis defined in the scenario:

- Creates a `CompassTracking` object
- Initializes `CurrentValue` to 0.0
- Creates empty history list
- Sets `LastUpdated` to current time

This ensures all compass axes are tracked from the start of the session.

## Error Handling

- **Scenario Not Found**: Returns `ArgumentException` with scenario ID
- **Age Incompatible**: Returns `ArgumentException` with age details
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Make Choice Use Case](./make-choice.md)
- [Game Session Domain Model](../../domain/models/game-session.md)
- [Compass Tracking](../../domain/models/compass-tracking.md)
