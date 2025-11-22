# Scenario Use Cases

This directory contains documentation for all scenario-related use cases.

## Overview

Scenarios represent the core game content - interactive stories with scenes, characters, choices, and compass tracking. These use cases handle the complete lifecycle of scenario management.

## Use Cases

### [Create Scenario](./create-scenario.md)

Creates a new scenario with full validation, including:

- JSON schema validation
- Business rule validation (scene references, branch references)
- Parsing from dictionary/YAML format

**Flow**: Controller → Service → Use Case → Parser → Validation → Repository → Database

### [Get Scenarios](./get-scenarios.md)

Retrieves scenarios with advanced filtering and pagination:

- Filter by difficulty, session length, age group, tags, archetypes, core axes
- Pagination support
- Sorting by creation date

**Flow**: Controller → Service → Use Case → Repository → Database

### [Update Scenario](./update-scenario.md)

Updates an existing scenario with validation:

- Schema validation
- Business rule validation
- Preserves existing relationships

**Flow**: Controller → Service → Use Case → Validation → Repository → Database

### [Delete Scenario](./delete-scenario.md)

Deletes a scenario by ID:

- Soft validation (checks existence)
- Transactional deletion

**Flow**: Controller → Service → Use Case → Repository → Database

### [Validate Scenario](./validate-scenario.md)

Validates scenario business rules:

- Scene ID uniqueness
- Branch NextSceneId references
- Echo reveal trigger scene references

**Flow**: Controller → Service → Use Case → Domain Validation

## Parser Integration

All scenario creation/update flows use parsers from `Application.Parsers`:

- **ScenarioParser** - Validates and parses top-level scenario data
- **SceneParser** - Parses scene dictionaries to Scene domain objects
- **CharacterParser** - Parses character dictionaries to ScenarioCharacter objects
- **CharacterMetadataParser** - Parses character metadata (species, age, backstory, etc.)
- **BranchParser** - Parses choice/branch dictionaries to Branch objects
- **EchoLogParser** - Parses echo log dictionaries to EchoLog objects
- **CompassChangeParser** - Parses compass change dictionaries to CompassChange objects
- **EchoRevealParser** - Parses echo reveal dictionaries to EchoReveal objects
- **MediaReferencesParser** - Parses media reference dictionaries to MediaReferences objects

## Related Components

- **Domain Models**: `Scenario`, `Scene`, `ScenarioCharacter`, `Branch`, `EchoLog`, `CompassChange`, `EchoReveal`
- **Repositories**: `IScenarioRepository`
- **Validation**: `ValidateScenarioUseCase`, `ScenarioSchemaDefinitions`
- **DTOs**: `CreateScenarioRequest`, `ScenarioQueryRequest`, `ScenarioListResponse`
