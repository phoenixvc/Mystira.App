# Delete Scenario Use Case

## Overview

The `DeleteScenarioUseCase` handles deletion of scenarios by ID.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.Scenarios.DeleteScenarioUseCase`

**Input**: `string id`

**Output**: `bool` (true if deleted, false if not found)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as ScenariosController
    participant Service as ScenarioApiService
    participant UseCase as DeleteScenarioUseCase
    participant Repo as IScenarioRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: DELETE /api/scenarios/{id}
    Controller->>Service: DeleteScenarioAsync(id)
    
    Service->>UseCase: ExecuteAsync(id)
    
    Note over UseCase: Step 1: Find Scenario
    UseCase->>Repo: GetByIdAsync(id)
    Repo->>DB: Query scenario
    alt Scenario Not Found
        DB-->>Repo: null
        Repo-->>UseCase: null
        UseCase-->>Service: false
        Service-->>Controller: NotFound
        Controller-->>Client: 404 Not Found
    end
    DB-->>Repo: Scenario
    Repo-->>UseCase: scenario
    
    Note over UseCase: Step 2: Delete Scenario
    UseCase->>Repo: DeleteAsync(id)
    Repo->>DB: Remove entity from DbSet
    UseCase->>UoW: SaveChangesAsync()
    UoW->>DB: Commit transaction
    DB-->>UoW: Success
    UoW-->>UseCase: Success
    
    UseCase-->>Service: true
    Service-->>Controller: true
    Controller-->>Client: 200 OK<br/>(Success)
```

## Deletion Process

1. **Find Scenario**: Retrieves scenario to verify existence and log details
2. **Delete**: Removes scenario from database
3. **Commit**: Saves changes transactionally

## Behavior

- **Idempotent**: Returns `false` if scenario doesn't exist (no error)
- **Transactional**: Deletion is atomic
- **Logged**: Logs deletion with scenario ID and title

## Cascading Behavior

Currently, deletion does not cascade to:

- Game sessions referencing the scenario (sessions remain)
- User badges earned in scenarios (badges remain)

**Note**: Consider adding cascade deletion or soft deletion if referential integrity is required.

## Error Handling

- **Scenario Not Found**: Returns `false` (not an error)
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Create Scenario Use Case](./create-scenario.md)
- [Scenario Domain Model](../../domain/models/scenario.md)
