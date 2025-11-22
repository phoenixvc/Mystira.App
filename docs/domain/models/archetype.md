# Archetype Domain Model

## Overview

The `Archetype` domain model represents character archetypes using the `StringEnum<T>` pattern. Archetypes define character types and roles that appear in scenarios.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/Archetype.cs`

## Base Class

Extends `StringEnum<Archetype>` - See [StringEnum Documentation](./string-enum.md) for base functionality.

## Properties

| Property | Type     | Description                            |
| -------- | -------- | -------------------------------------- |
| `Value`  | `string` | Archetype identifier (from StringEnum) |

## JSON Configuration

Archetypes are loaded from `Data/Archetypes.json`:

```json
[
  { "Value": "guardian" },
  { "Value": "the listener" },
  { "Value": "trickster" },
  { "Value": "mentor" },
  { "Value": "hero" }
]
```

## Usage

```csharp
// Parse archetype
var archetype = Archetype.Parse("guardian");

// Check if valid
if (Archetype.TryParse("guardian", out var result))
{
    // Use result
}

// Compare (case-insensitive)
if (archetype == Archetype.Parse("GUARDIAN"))
{
    // Equal
}

// Get all archetypes
var allArchetypes = Archetype.ValueMap.Values;
```

## Relationships

- `Archetype` → `Scenario` (via `Archetypes` list)
- `Archetype` → `ScenarioCharacter` (via `Metadata.Archetype`)
- `Archetype` → `CharacterMap` (via `Metadata.Archetypes`)

## JSON Serialization

Uses `StringEnumJsonConverter<Archetype>` for JSON serialization:

- Serializes to string value
- Deserializes from string value
- Case-insensitive parsing

## Related Documentation

- [StringEnum Base Class](./string-enum.md)
- [Scenario Domain Model](./scenario.md)
- [CharacterMap Domain Model](./character-map.md)
