# End Game Session Use Case

## Overview

The `EndSessionAsync` method in `GameSessionApiService` handles manually ending a game session, marking it as completed.

## Use Case Details

**Class**: `Mystira.App.Api.Services.GameSessionApiService` (Service Layer)

**Input**: `string sessionId`

**Output**: `GameSession?` (domain model, null if not found)

**Status**: Currently in production

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as GameSessionsController
    participant Service as GameSessionApiService
    participant Repo as IGameSessionRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: POST /api/gamesessions/{id}/end
    Controller->>Service: EndSessionAsync(sessionId)
    
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
    
    Note over Service: Step 2: End Session
    Service->>Service: session.Status = Completed
    Service->>Service: session.EndTime = Now
    Service->>Service: session.ElapsedTime =<br/>  EndTime - StartTime
    Service->>Service: session.IsPaused = false
    
    Note over Service: Step 3: Persist Changes
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

### 2. End Operation

- Changes status to `Completed`
- Sets `EndTime` to current UTC time
- Calculates `ElapsedTime` as difference between `EndTime` and `StartTime`
- Clears `IsPaused` flag (if paused)

### 3. Persistence

- Updates session in database
- Commits transaction

## Use Cases

### Manual Session Completion

User explicitly ends a session before reaching the natural end.

### Session Abandonment

User abandons a session (though this typically uses `Abandoned` status).

### Admin Session Management

Administrators can end sessions for management purposes.

## State Transitions

``` text
InProgress → Completed
Paused → Completed
```

## Elapsed Time Calculation

The elapsed time is calculated as:

``` text
ElapsedTime = EndTime - StartTime
```

This provides the total play time for the session, regardless of pauses.

## Error Handling

- **Session Not Found**: Returns `null` (handled as 404)
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Create Game Session Use Case](./create-game-session.md)
- [Resume Game Session Use Case](./resume-game-session.md)
- [Game Session Domain Model](../../domain/models/game-session.md)
