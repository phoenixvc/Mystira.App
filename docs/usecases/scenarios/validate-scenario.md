# Validate Scenario Use Case

## Overview

The `ValidateScenarioUseCase` validates scenario business rules, ensuring scene references are valid and scenes are reachable.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.Scenarios.ValidateScenarioUseCase`

**Input**: `Scenario` (domain model)

**Output**: `Task` (throws exception on validation failure)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant UseCase as ValidateScenarioUseCase
    participant Scenario as Scenario Domain Model

    Note over UseCase: Step 1: Extract Scene IDs
    UseCase->>Scenario: Extract all scene IDs<br/>from scenario.Scenes
    Scenario-->>UseCase: HashSet<string> sceneIds
    
    Note over UseCase: Step 2: Validate Scene References
    loop For each scene in scenario.Scenes
        alt Scene has NextSceneId
            UseCase->>UseCase: Check if NextSceneId<br/>exists in sceneIds
            alt Reference Invalid
                UseCase-->>UseCase: Throw ArgumentException<br/>("Scene references<br/>non-existent next scene")
            end
            UseCase->>UseCase: Add to referencedScenes
        end
        
        loop For each branch in scene.Branches
            UseCase->>UseCase: Check if branch.NextSceneId<br/>exists in sceneIds
            alt Reference Invalid
                UseCase-->>UseCase: Throw ArgumentException<br/>("Branch references<br/>non-existent scene")
            end
            UseCase->>UseCase: Add to referencedScenes
        end
        
        loop For each echo reveal in scene.EchoReveals
            UseCase->>UseCase: Check if reveal.TriggerSceneId<br/>exists in sceneIds
            alt Reference Invalid
                UseCase-->>UseCase: Throw ArgumentException<br/>("Echo reveal references<br/>non-existent scene")
            end
        end
    end
    
    Note over UseCase: Step 3: Validate Scene Reachability
    UseCase->>UseCase: Find first scene<br/>(starting point)
    UseCase->>UseCase: Calculate unreachable scenes:<br/>sceneIds - referencedScenes<br/>(excluding first scene)
    alt Unreachable Scenes Found
        UseCase->>UseCase: Log warning<br/>(does not throw exception)
    end
    
    UseCase-->>UseCase: Validation Complete
```

## Validation Rules

### 1. Scene ID Uniqueness

- All scene IDs must be unique within the scenario
- Enforced by domain model structure

### 2. Next Scene References

- `Scene.NextSceneId` must reference a valid scene ID
- Throws `ArgumentException` if reference is invalid

### 3. Branch References

- `Branch.NextSceneId` must reference a valid scene ID
- Throws `ArgumentException` if reference is invalid

### 4. Echo Reveal References

- `EchoReveal.TriggerSceneId` must reference a valid scene ID
- Throws `ArgumentException` if reference is invalid

### 5. Scene Reachability (Warning Only)

- All scenes (except the first) should be reachable
- Unreachable scenes are logged as warnings (not errors)
- Allows for future scenes or alternative paths

## Validation Flow

1. **Extract Scene IDs**: Creates set of all scene IDs
2. **Validate References**: Checks all scene, branch, and echo reveal references
3. **Check Reachability**: Identifies unreachable scenes (warning only)

## Error Messages

- `"Scene '{sceneId}' references non-existent next scene '{nextSceneId}'"`
- `"Scene '{sceneId}' branch references non-existent scene '{nextSceneId}'"`
- `"Scene '{sceneId}' echo reveal references non-existent scene '{triggerSceneId}'"`
- `"Scenario must have at least one scene"`

## Usage

This use case is called by:

- `CreateScenarioUseCase` - Before saving new scenario
- `UpdateScenarioUseCase` - Before saving updated scenario

## Related Documentation

- [Create Scenario Use Case](./create-scenario.md)
- [Update Scenario Use Case](./update-scenario.md)
- [Scenario Domain Model](../../domain/models/scenario.md)
