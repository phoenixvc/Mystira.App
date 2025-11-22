# Update Scenario Use Case

## Overview

The `UpdateScenarioUseCase` updates an existing scenario with full validation, similar to creation but preserves the scenario ID.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.Scenarios.UpdateScenarioUseCase`

**Input**: `string id`, `CreateScenarioRequest`

**Output**: `Scenario?` (updated domain model, null if not found)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as ScenariosController
    participant Service as ScenarioApiService
    participant UseCase as UpdateScenarioUseCase
    participant Validator as ValidateScenarioUseCase
    participant Schema as ScenarioSchemaDefinitions
    participant Repo as IScenarioRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: PUT /api/scenarios/{id}<br/>(CreateScenarioRequest)
    Controller->>Service: UpdateScenarioAsync(id, request)
    
    Service->>UseCase: ExecuteAsync(id, request)
    
    Note over UseCase: Step 1: Find Existing Scenario
    UseCase->>Repo: GetByIdAsync(id)
    Repo->>DB: Query scenario
    alt Scenario Not Found
        DB-->>Repo: null
        Repo-->>UseCase: null
        UseCase-->>Service: null
        Service-->>Controller: NotFound
        Controller-->>Client: 404 Not Found
    end
    DB-->>Repo: Scenario
    Repo-->>UseCase: existingScenario
    
    Note over UseCase: Step 2: Schema Validation
    UseCase->>Schema: ValidateAgainstSchema(request)
    Schema->>Schema: Serialize to JSON<br/>(snake_case)
    Schema->>Schema: Validate against JSON Schema
    alt Validation Fails
        Schema-->>UseCase: ValidationError
        UseCase-->>Service: ArgumentException
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    
    Note over UseCase: Step 3: Apply Updates
    UseCase->>UseCase: Update scenario properties:<br/>  Title, Description, Tags,<br/>  Difficulty, SessionLength,<br/>  Archetypes (parsed),<br/>  AgeGroup, MinimumAge,<br/>  CoreAxes (parsed),<br/>  Characters, Scenes<br/>(ID preserved)
    
    Note over UseCase: Step 4: Business Rule Validation
    UseCase->>Validator: ExecuteAsync(updatedScenario)
    Validator->>Validator: Validate scene IDs unique
    Validator->>Validator: Validate branch references
    Validator->>Validator: Validate echo reveal references
    alt Validation Fails
        Validator-->>UseCase: ArgumentException
        UseCase-->>Service: ArgumentException
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    
    Note over UseCase: Step 5: Persist Changes
    UseCase->>Repo: UpdateAsync(scenario)
    Repo->>DB: Update entity
    UseCase->>UoW: SaveChangesAsync()
    UoW->>DB: Commit transaction
    DB-->>UoW: Success
    UoW-->>UseCase: Success
    Repo-->>UseCase: Scenario (updated)
    
    UseCase-->>Service: Scenario
    Service-->>Controller: Scenario
    Controller-->>Client: 200 OK<br/>(Scenario)
```

## Update Process

1. **Retrieve Existing**: Loads current scenario from database
2. **Schema Validation**: Validates request against JSON schema
3. **Apply Updates**: Maps DTO properties to domain model (preserves ID)
4. **Business Validation**: Validates updated scenario business rules
5. **Persist**: Saves changes to database

## Preserved Properties

- **ID**: Scenario ID is never changed
- **CreatedAt**: Original creation timestamp preserved
- **Relationships**: Existing relationships maintained

## Updated Properties

All properties from `CreateScenarioRequest` are updated:

- Title, Description, Tags
- Difficulty, SessionLength
- Archetypes (parsed from strings)
- AgeGroup, MinimumAge
- CoreAxes (parsed from strings)
- Characters (full replacement)
- Scenes (full replacement)

## Validation

Same validation as creation:

- JSON schema validation
- Business rule validation (scene references, branch references)
- Domain model validation (enum parsing, value constraints)

## Error Handling

- **Scenario Not Found**: Returns `null` (handled as 404)
- **Schema Validation Failure**: Returns `ArgumentException` with errors
- **Business Rule Violation**: Returns `ArgumentException` with specific violation
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Create Scenario Use Case](./create-scenario.md)
- [Validate Scenario Use Case](./validate-scenario.md)
- [Scenario Domain Model](../../domain/models/scenario.md)
