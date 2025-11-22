# Progress Scene Use Case

## Overview

The `ProgressSceneUseCase` allows advancing to a specific scene in a game session, supporting pause/resume functionality.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.GameSessions.ProgressSceneUseCase`

**Input**: `ProgressSceneRequest`

**Output**: `GameSession` (updated domain model)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as GameSessionsController
    participant Service as GameSessionApiService
    participant UseCase as ProgressSceneUseCase
    participant SessionRepo as IGameSessionRepository
    participant ScenarioRepo as IScenarioRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: PUT /api/gamesessions/{id}/progress<br/>(ProgressSceneRequest)
    Controller->>Service: ProgressSessionSceneAsync(sessionId, request)
    
    Service->>UseCase: ExecuteAsync(request)
    
    Note over UseCase: Step 1: Validate Session
    UseCase->>SessionRepo: GetByIdAsync(sessionId)
    SessionRepo->>DB: Query session
    alt Session Not Found
        DB-->>SessionRepo: null
        SessionRepo-->>UseCase: null
        UseCase-->>Service: null
        Service-->>Controller: NotFound
        Controller-->>Client: 404 Not Found
    end
    DB-->>SessionRepo: GameSession
    SessionRepo-->>UseCase: session
    
    alt Session Not InProgress or Paused
        UseCase-->>Service: InvalidOperationException
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    
    Note over UseCase: Step 2: Validate Scenario & Scene
    UseCase->>ScenarioRepo: GetByIdAsync(session.ScenarioId)
    ScenarioRepo->>DB: Query scenario
    DB-->>ScenarioRepo: Scenario
    ScenarioRepo-->>UseCase: scenario
    
    UseCase->>UseCase: Find targetScene<br/>by request.SceneId
    alt Scene Not Found
        UseCase-->>Service: ArgumentException("Scene not found")
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    
    Note over UseCase: Step 3: Update Session State
    UseCase->>UseCase: session.CurrentSceneId =<br/>request.SceneId
    UseCase->>UseCase: session.ElapsedTime =<br/>Now - session.StartTime
    
    Note over UseCase: Step 4: Resume if Paused
    alt Session Status == Paused
        UseCase->>UseCase: session.Status = InProgress
        UseCase->>UseCase: session.IsPaused = false
        UseCase->>UseCase: session.PausedAt = null
    end
    
    Note over UseCase: Step 5: Persist Changes
    UseCase->>SessionRepo: UpdateAsync(session)
    SessionRepo->>DB: Update entity
    UseCase->>UoW: SaveChangesAsync()
    UoW->>DB: Commit transaction
    DB-->>UoW: Success
    UoW-->>UseCase: Success
    SessionRepo-->>UseCase: GameSession (updated)
    
    UseCase-->>Service: GameSession
    Service-->>Controller: GameSession
    Controller-->>Client: 200 OK<br/>(GameSession)
```

## Use Cases

### Direct Scene Navigation

Allows jumping to any scene in the scenario (useful for testing, debugging, or admin features).

### Pause/Resume

- If session is paused, progressing to a scene automatically resumes it
- Status changes from `Paused` to `InProgress`
- `IsPaused` flag is cleared
- `PausedAt` timestamp is cleared

## Validation

1. **Session Exists**: Session must be found in database
2. **Session Status**: Must be `InProgress` or `Paused`
3. **Scenario Exists**: Scenario must exist for the session
4. **Scene Exists**: Target scene must exist in the scenario

## State Updates

- `CurrentSceneId`: Updated to target scene ID
- `ElapsedTime`: Recalculated from start time
- `Status`: Changed to `InProgress` if paused
- `IsPaused`: Cleared if paused
- `PausedAt`: Cleared if paused

## Error Handling

- **Session Not Found**: Returns `null` (handled as 404)
- **Invalid Status**: Returns `InvalidOperationException`
- **Scene Not Found**: Returns `ArgumentException` with scene ID
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Create Game Session Use Case](./create-game-session.md)
- [Make Choice Use Case](./make-choice.md)
- [Game Session Domain Model](../../domain/models/game-session.md)
