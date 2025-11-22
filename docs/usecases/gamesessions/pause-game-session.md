# Pause Game Session Use Case

## Overview

The `PauseSessionAsync` method in `GameSessionApiService` handles pausing an active game session.

## Use Case Details

**Class**: `Mystira.App.Api.Services.GameSessionApiService` (Service Layer)

**Input**: `string sessionId`

**Output**: `GameSession?` (domain model, null if not found)

**Status**: Currently in production (should be migrated to use case)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as GameSessionsController
    participant Service as GameSessionApiService
    participant Repo as IGameSessionRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: POST /api/gamesessions/{id}/pause
    Controller->>Service: PauseSessionAsync(sessionId)
    
    Note over Service: Step 1: Get Session
    Service->>Repo: GetByIdAsync(sessionId)
    Repo->>DB: Query session
    alt Session Not Found
        DB-->>Repo: null
        Repo-->>Service: null
        Service-->>Controller: NotFound
        Controller-->>Client: 404 Not Found
    end
    DB-->>Repo: GameSession
    Repo-->>Service: session
    
    Note over Service: Step 2: Validate Status
    alt Session Not InProgress
        Service-->>Service: Throw InvalidOperationException<br/>("Can only pause sessions in progress")
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    
    Note over Service: Step 3: Pause Session
    Service->>Service: session.Status = Paused
    Service->>Service: session.IsPaused = true
    Service->>Service: session.PausedAt = Now
    
    Note over Service: Step 4: Persist Changes
    Service->>Repo: UpdateAsync(session)
    Repo->>DB: Update entity
    Service->>UoW: SaveChangesAsync()
    UoW->>DB: Commit transaction
    DB-->>UoW: Success
    UoW-->>Service: Success
    Repo-->>Service: GameSession (updated)
    
    Service-->>Controller: GameSession
    Controller-->>Client: 200 OK<br/>(GameSession)
```

## Use Case Flow

### 1. Session Retrieval

- Loads session from database by ID
- Returns null if session doesn't exist

### 2. Status Validation

- Validates session is in `InProgress` status
- Throws `InvalidOperationException` if session is not in progress

### 3. Pause Operation

- Changes status from `InProgress` to `Paused`
- Sets `IsPaused` flag to true
- Sets `PausedAt` timestamp to current UTC time

### 4. Persistence

- Updates session in database
- Commits transaction

## State Transitions

``` text
InProgress → Paused
```

## Elapsed Time Handling

When paused, elapsed time calculation accounts for pause duration:

- `GetTotalElapsedTime()` includes paused time
- `ElapsedTime` property stores time before pause
- Paused duration calculated as `Now - PausedAt` when resuming

## Error Handling

- **Session Not Found**: Returns `null` (handled as 404)
- **Invalid Status**: Returns `InvalidOperationException` (handled as 400)
- **Database Error**: Logs error and rethrows exception

## Migration to Use Case

**Recommended**: Create `PauseGameSessionUseCase` in `Application.UseCases.GameSessions`

**Benefits**:

- Consistent with other game session use cases
- Better testability
- Clear separation of concerns
- Follows hexagonal architecture pattern

## Related Documentation

- [Resume Game Session Use Case](./resume-game-session.md)
- [End Game Session Use Case](./end-game-session.md)
- [Game Session Domain Model](../../domain/models/game-session.md)

