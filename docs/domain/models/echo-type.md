# EchoType Domain Model

## Overview

The `EchoType` domain model represents echo types using the `StringEnum<T>` pattern. Echo types define the categories of echoes (moral reflections) that can be generated during gameplay.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/EchoType.cs`

## Base Class

Extends `StringEnum<EchoType>` - See [StringEnum Documentation](./string-enum.md) for base functionality.

## Properties

| Property | Type     | Description                            |
| -------- | -------- | -------------------------------------- |
| `Value`  | `string` | Echo type identifier (from StringEnum) |

## JSON Configuration

Echo types are loaded from `Data/EchoTypes.json`:

```json
[
  { "Value": "honesty" },
  { "Value": "kindness" },
  { "Value": "bravery" },
  { "Value": "wisdom" },
  { "Value": "curiosity" }
]
```

## Usage

```csharp
// Parse echo type
var echoType = EchoType.Parse("honesty");

// Check if valid
if (EchoType.TryParse("honesty", out var result))
{
    // Use result
}

// Compare (case-insensitive)
if (echoType == EchoType.Parse("HONESTY"))
{
    // Equal
}

// Get all echo types
var allEchoTypes = EchoType.ValueMap.Values;
```

## Relationships

- `EchoType` → `EchoLog` (via `EchoType` property)
- `EchoType` → `EchoReveal` (via `EchoType` property)

## Echo System Integration

Echo types are used in the echo system:

- **EchoLog** - Generated echoes have a type (`EchoLog.EchoType`)
- **EchoReveal** - Echo reveals specify which type to reveal (`EchoReveal.EchoType`)
- Echo types typically align with compass axes (honesty, kindness, etc.)

## Default Value

When creating `EchoLog` objects, the default echo type is "honesty":

```csharp
public EchoType EchoType { get; set; } = EchoType.Parse("honesty")!;
```

## JSON Serialization

Uses `StringEnumJsonConverter<EchoType>` for JSON serialization:

- Serializes to string value
- Deserializes from string value
- Case-insensitive parsing

## Related Documentation

- [StringEnum Base Class](./string-enum.md)
- [Scenario Domain Model](./scenario.md#echolog)
- [Scenario Domain Model](./scenario.md#echoreveal)
