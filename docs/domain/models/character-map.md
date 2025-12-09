# CharacterMap Domain Model

## Overview

The `CharacterMap` domain model represents reusable character definitions that can be used across multiple scenarios. Character maps provide a centralized way to manage character assets, metadata, and configurations.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/CharacterMap.cs`

## Properties

| Property      | Type                | Description                                    |
| ------------- | ------------------- | ---------------------------------------------- |
| `Id`          | `string`            | Unique identifier                              |
| `Name`        | `string`            | Character name                                 |
| `Image`        | `string`            | Media path (e.g., "media/images/elarion.jpg") |
| `Audio`        | `string?`           | Optional audio file path                       |
| `Metadata`     | `CharacterMetadata` | Character metadata                             |
| `CreatedAt`   | `DateTime`          | Creation timestamp                             |
| `UpdatedAt`   | `DateTime`          | Last update timestamp                          |

## Related Domain Models

### CharacterMetadata

Contains detailed character information.

**Properties**:

| Property      | Type                | Description                                    |
| ------------- | ------------------- | ---------------------------------------------- |
| `Roles`       | `List<string>`      | Character roles (e.g., ["mentor", "trickster"]) |
| `Archetypes`  | `List<string>`      | Character archetypes (e.g., ["guardian", "the listener"]) |
| `Species`     | `string`            | Species (e.g., "elf", "goblin")               |
| `Age`         | `int`               | Character age                                  |
| `Traits`      | `List<string>`      | Character traits (e.g., ["wise", "calm", "mysterious"]) |
| `Backstory`   | `string`            | Character backstory                            |

### CharacterMapYaml

YAML structure for character map export/import.

**Properties**:

- `Characters` - `List<CharacterMapYamlEntry>`

### CharacterMapYamlEntry

YAML entry structure for a single character.

**Properties**:

| Property      | Type                | Description                                    |
| ------------- | ------------------- | ---------------------------------------------- |
| `Id`          | `string`            | Character ID                                   |
| `Name`        | `string`            | Character name                                 |
| `Image`        | `string`            | Image path                                     |
| `Audio`        | `string?`           | Optional audio path                            |
| `Metadata`     | `CharacterMetadata` | Character metadata                             |

## Relationships

- `CharacterMap` → `CharacterMetadata` (embedded)
- `CharacterMap` → `ScenarioCharacter` (via character selection in scenarios)
- `CharacterMap` → `GameSession` (via `SelectedCharacterId`)

## Use Cases

**Current Status**: ❌ No use cases (all operations in services)

**Use Cases** (Should be implemented):

- ❌ `GetCharacterMapsUseCase` - Get all character maps
- ❌ `GetCharacterMapUseCase` - Get character map by ID
- ❌ `CreateCharacterMapUseCase` - Create new character map
- ❌ `UpdateCharacterMapUseCase` - Update character map
- ❌ `DeleteCharacterMapUseCase` - Delete character map
- ❌ `ImportCharacterMapUseCase` - Import from YAML
- ❌ `ExportCharacterMapUseCase` - Export to YAML

**Current Implementation**: `CharacterMapApiService` (should be refactored)

**Recommendation**: Create `Application.UseCases.CharacterMaps` directory and migrate service logic

## YAML Import/Export

Character maps support YAML-based import and export for easy content management:

- **Export**: Converts character maps to YAML format
- **Import**: Creates character maps from YAML files
- **Structure**: Uses `CharacterMapYaml` and `CharacterMapYamlEntry` classes

## Persistence

- Stored in Cosmos DB via `ICharacterMapRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id`
- Metadata is stored as embedded document

## Related Documentation

- [Scenario Domain Model](./scenario.md)
- [GameSession Domain Model](./game-session.md)
- [Archetype Domain Model](./archetype.md)
- [Use Cases Documentation](../usecases/README.md)
